namespace Tmds.DBus.Protocol;

sealed class PathNode
{
    // _childNames is null when there are no child names
    // a string if there is a single child name
    // a List<string> List<string>.Count child names
    private object? _childNames;
    public IPathMethodHandler? MethodHandler;
    public PathNode? Parent { get; set; }

    public int ChildNameCount =>
        _childNames switch
        {
            null => 0,
            string => 1,
            var list => ((List<string>)list).Count
        };

    public void ClearChildNames()
    {
        Debug.Assert(ChildNameCount == 1, "Method isn't expected to be called unless there is 1 child name.");
        if (_childNames is List<string> list)
        {
            list.Clear();
        }
        else
        {
            _childNames = null;
        }
    }

    public void RemoveChildName(string name)
    {
        Debug.Assert(ChildNameCount > 1, "Caller is expected to call ClearChildNames instead.");
        var list = (List<string>)_childNames!;
        list.Remove(name);
    }

    public void AddChildName(string value)
    {
        if (_childNames is null)
        {
            _childNames = value;
        }
        else if (_childNames is string first)
        {
            _childNames = new List<string>() { first, value };
        }
        else
        {
            ((List<string>)_childNames).Add(value);
        }
    }

    public void SetIntrospectChildNames(MethodContext methodContext)
    {
        Debug.Assert(methodContext.IntrospectChildNames is null);

        if (_childNames is null)
        {
            methodContext.IntrospectChildNames = null;
        }
        else if (_childNames is string s)
        {
            methodContext.IntrospectChildNames = new[] { s };
        }
        else
        {
            methodContext.IntrospectChildNames = ((List<string>)_childNames).ToArray();
        }
    }
}

sealed class PathNodeDictionary : IMethodHandlerDictionary
{
    private readonly Dictionary<string, PathNode> _dictionary = new();

    public void TryGetValue(string? path, out IPathMethodHandler? handler, out PathNode? pathNode)
    {
        if (path is null)
        {
            handler = null;
            pathNode = null;
            return;
        }

        if (_dictionary.TryGetValue(path, out pathNode))
        {
            handler = pathNode.MethodHandler;
            if (handler is not null)
            {
                return;
            }
        }

        handler = FindTreeHandler(path);
    }

    private IPathMethodHandler? FindTreeHandler(string path)
    {
        if (!IsValidPath(path))
        {
            return null;
        }

        if (TryFindTreeHandler("/".AsSpan(), out IPathMethodHandler? handler))
        {
            return handler;
        }

        ReadOnlySpan<char> pathSpan = path.AsSpan();
        int pos = 1;
        while (true)
        {
            int nextSlash = pathSpan.Slice(pos).IndexOf('/');
            if (nextSlash == -1)
            {
                break;
            }
            pos += nextSlash;
            ReadOnlySpan<char> ancestorPath = pathSpan.Slice(0, pos);
            if (TryFindTreeHandler(ancestorPath, out handler))
            {
                return handler;
            }

            pos++;
        }

        return null;

        bool TryFindTreeHandler(ReadOnlySpan<char> path, out IPathMethodHandler? handler)
        {
            handler = null;
#if NET9_0_OR_GREATER
            var lookup = _dictionary.GetAlternateLookup<ReadOnlySpan<char>>();
            if (!lookup.TryGetValue(path, out PathNode? node))
#else
            if (!_dictionary.TryGetValue(path.ToString(), out PathNode? node))
#endif
            {
                return true; // Dead end, there's no handler.
            }
            if (node.MethodHandler is null || node.MethodHandler.HandlesChildPaths == false)
            {
                return false;
            }
            handler = node.MethodHandler;
            return true;
        }
    }

    // For tests:
    public PathNode this[string path]
        => _dictionary[path];
    public int Count
        => _dictionary.Count;

    public void AddMethodHandlers(IReadOnlyList<IPathMethodHandler> methodHandlers)
    {
        if (methodHandlers is null)
        {
            throw new ArgumentNullException(nameof(methodHandlers));
        }

        int registeredCount = 0;
        try
        {
            for (int i = 0; i < methodHandlers.Count; i++)
            {
                IPathMethodHandler methodHandler = methodHandlers[i] ?? throw new ArgumentNullException("methodHandler");

                AddMethodHandler(methodHandler);

                registeredCount++;
            }
        }
        catch
        {
            RemoveMethodHandlers(methodHandlers, registeredCount);

            throw;
        }
    }


    private (PathNode, bool exists) GetOrCreateNode(string path)
    {
#if NET6_0_OR_GREATER
        ref PathNode? node = ref CollectionsMarshal.GetValueRefOrAddDefault(_dictionary, path, out bool exists);
        if (exists)
        {
            return (node!, true);
        }
        PathNode newNode = new PathNode();
        node = newNode;
#else
        if (_dictionary.TryGetValue(path, out PathNode? node))
        {
            return (node, true);
        }
        PathNode newNode = new PathNode();
        _dictionary.Add(path, newNode);
#endif
        string? parentPath = GetParentPath(path);
        if (parentPath is not null)
        {
            (PathNode parent, _) = GetOrCreateNode(parentPath);
            newNode.Parent = parent;
            parent.AddChildName(GetChildName(path));
        }

        return (newNode, false);
    }

    private static string? GetParentPath(string path)
    {
        if (path.Length == 1)
        {
            return null;
        }

        int index = path.LastIndexOf('/');
        Debug.Assert(index != -1);

        // When index == 0, return '/'.
        index = Math.Max(index, 1);

        return path.Substring(0, index);
    }

    private static string GetChildName(string path)
    {
        int index = path.LastIndexOf('/');
        return path.Substring(index + 1);
    }

    private void RemoveMethodHandlers(IReadOnlyList<IPathMethodHandler> methodHandlers, int count)
    {
        // We start by (optimistically) removing all nodes (assuming they form a tree that is pruned).
        // If there are nodes that are still needed to serve as parent nodes, we'll add them back at the end.
        (string Path, PathNode Node)[] nodes = new (string, PathNode)[count];
        int j = 0;
        for (int i = 0; i < count; i++)
        {
            string path = methodHandlers[i].Path;
            if (_dictionary.Remove(path, out PathNode? node))
            {
                nodes[j++] = (path, node);
                node.MethodHandler = null;
            }
        }
        count = j; j = 0;
        
        // Reverse sort by path length to remove leaves before parents.
        Array.Sort(nodes, 0, count, RemoveKeyComparerInstance);
        for (int i = 0; i < count; i++)
        {
            var node = nodes[i];
            if (node.Node.ChildNameCount == 0)
            {
                RemoveFromParent(node.Path, node.Node);
            }
            else
            {
                nodes[j++] = node;
            }
        }
        count = j; j = 0;

        // Add back the nodes that serve as parent nodes.
        for (int i = 0; i < count; i++)
        {
            var node = nodes[i];
            _dictionary[node.Path] = node.Node;
        }
    }

    private void RemoveFromParent(string path, PathNode node)
    {
        PathNode? parent = node.Parent;
        if (parent is null)
        {
            return;
        }
        Debug.Assert(parent.ChildNameCount >= 1, "node is expected to be a known child");
        if (parent.ChildNameCount == 1) // We're the only child.
        {
            if (parent.MethodHandler is not null)
            {
                // Parent is still needed for the MethodHandler.
                parent.ClearChildNames();
            }
            else
            {
// Suppress netstandard2.0 nullability warnings around NetstandardExtensions.Remove.
#if NETSTANDARD2_0
#pragma warning disable CS8620
#pragma warning disable CS8604
#endif
                // Parent is no longer needed.
                string parentPath = GetParentPath(path)!;
                Debug.Assert(parentPath is not null);
                _dictionary.Remove(parentPath, out PathNode? parentNode);
                Debug.Assert(parentNode is not null);
                RemoveFromParent(parentPath, parentNode);
#if NETSTANDARD2_0
#pragma warning restore CS8620
#pragma warning restore CS8604
#endif
            }
        }
        else
        {
            string childName = GetChildName(path);
            parent.RemoveChildName(childName);
        }
    }

    private bool IsValidPath(string path)
    {
        return path.Length > 0 && path[0] == '/' && path.IndexOf("//", StringComparison.Ordinal) == -1;
    }

    public void AddMethodHandler(IPathMethodHandler methodHandler)
    {
        string path = methodHandler.Path ?? throw new ArgumentNullException(nameof(methodHandler.Path));

        // Validate the path starts with '/' and has no empty sections.
        // GetParentPath relies on this.
        if (!IsValidPath(path))
        {
            throw new FormatException($"The path '{path}' is not valid.");
        }

        IPathMethodHandler? ancestorTreeHandler = FindTreeHandler(path);
        if (ancestorTreeHandler is not null)
        {
            throw new InvalidOperationException($"A method handler is already registered which handles '{path}' as a child path.");
        }

        (PathNode node, bool exists) = GetOrCreateNode(path);

        if (node.MethodHandler is not null)
        {
            throw new InvalidOperationException($"A method handler is already registered for the path '{path}'.");
        }

        if (methodHandler.HandlesChildPaths && exists)
        {
            throw new InvalidOperationException($"A method handler is already registered for a child path of '{path}'.");
        }

        node.MethodHandler = methodHandler;
    }

    public void RemoveMethodHandler(string path)
    {
        if (path is null)
        {
            throw new ArgumentNullException(nameof(path));
        }
        if (_dictionary.Remove(path, out PathNode? node))
        {
            if (node.ChildNameCount > 0)
            {
                // Node is still needed for its children.
                node.MethodHandler = null;
                _dictionary.Add(path, node);
            }
            else
            {
                RemoveFromParent(path, node);
            }
        }
    }

    public void RemoveMethodHandlers(IEnumerable<string> paths)
    {
        if (paths is null)
        {
            throw new ArgumentNullException(nameof(paths));
        }
        foreach (var path in paths)
        {
            RemoveMethodHandler(path);
        }
    }

    private static readonly RemoveKeyComparer RemoveKeyComparerInstance = new();

    sealed class RemoveKeyComparer : IComparer<(string Path, PathNode Node)>
    {
        public int Compare((string Path, PathNode Node) x, (string Path, PathNode Node) y)
            => x.Path.Length - y.Path.Length;
    }
}