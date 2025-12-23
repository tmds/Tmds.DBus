using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Tmds.DBus.Protocol.Tests;

public class PathNodeDictionaryTests
{
    private class MethodHandler : IPathMethodHandler
    {
        public MethodHandler(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public bool HandlesChildPaths => false;

        public ValueTask HandleMethodAsync(MethodContext context)
        {
            throw new NotImplementedException();
        }
    }

    private class TreeMethodHandler : IPathMethodHandler
    {
        public TreeMethodHandler(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public bool HandlesChildPaths => true;

        public ValueTask HandleMethodAsync(MethodContext context)
        {
            throw new NotImplementedException();
        }
    }

    [Theory]
    [InlineData("noleadingslash")]
    [InlineData("/double//slash")]
    public void ThrowsFormatExceptionForInvalidPaths(string path)
    {
        var dictionary = new PathNodeDictionary();
        Assert.Throws<FormatException>(() => dictionary.AddMethodHandlers([new MethodHandler(path)]));
    }

    [Fact]
    public void ThrowsArgumentNullExceptionForNullMethodHandlers()
    {
        var dictionary = new PathNodeDictionary();
        var exception = Assert.Throws<ArgumentNullException>(() => dictionary.AddMethodHandlers(null!));
        Assert.Equal("methodHandlers", exception.ParamName);
    }

    [Fact]
    public void ThrowsArgumentNullExceptionForNullMethodHandler()
    {
        var dictionary = new PathNodeDictionary();
        var exception = Assert.Throws<ArgumentNullException>(() => dictionary.AddMethodHandlers([null!]));
        Assert.Equal("methodHandler", exception.ParamName);
    }

    [Fact]
    public void ThrowsArgumentNullExceptionForNullPath()
    {
        var dictionary = new PathNodeDictionary();
        var exception = Assert.Throws<ArgumentNullException>(() => dictionary.AddMethodHandlers([new MethodHandler(null!)]));
        Assert.Equal("Path", exception.ParamName);
    }

    [Fact]
    public void ThrowsInvalidOperationWhenOverridingMethodHandler()
    {
        var dictionary = new PathNodeDictionary();
        Assert.Throws<InvalidOperationException>(() =>
            dictionary.AddMethodHandlers(
            [
                new MethodHandler("/path"),
                new MethodHandler("/path"),
            ]));
    }

    [Fact]
    public void CreatesParentNodes()
    {
        var dictionary = new PathNodeDictionary();
        dictionary.AddMethodHandlers(
        [
            new MethodHandler("/root1/node1"),
        ]);

        PathNode root = dictionary["/"];
        Assert.Null(root.Parent);
        Assert.Null(root.MethodHandler);
        AssertChildNames([ "root1" ], root);

        PathNode root1 = dictionary["/root1"];
        Assert.Equal(root, root1.Parent);
        Assert.Null(root1.MethodHandler);
        AssertChildNames([ "node1" ], root1);

        PathNode node1 = dictionary["/root1/node1"];
        Assert.Equal(root1, node1.Parent);
        Assert.NotNull(node1.MethodHandler);
        AssertChildNames([ ], node1);

        Assert.Equal(3, dictionary.Count);
    }

    [Fact]
    public void CanAddToExistingParent()
    {
        var dictionary = new PathNodeDictionary();
        dictionary.AddMethodHandlers(
        [
            new MethodHandler("/root1/node1"),
        ]);

        dictionary.AddMethodHandlers(
        [
            new MethodHandler("/root1/node2"),
        ]);

        PathNode root = dictionary["/"];
        Assert.Null(root.Parent);
        Assert.Null(root.MethodHandler);
        AssertChildNames([ "root1" ], root);

        PathNode root1 = dictionary["/root1"];
        Assert.Equal(root, root1.Parent);
        Assert.Null(root1.MethodHandler);
        AssertChildNames([ "node1", "node2" ], root1);

        PathNode node1 = dictionary["/root1/node1"];
        Assert.Equal(root1, node1.Parent);
        Assert.NotNull(node1.MethodHandler);
        AssertChildNames([ ], node1);

        PathNode node2 = dictionary["/root1/node2"];
        Assert.Equal(root1, node1.Parent);
        Assert.NotNull(node1.MethodHandler);
        AssertChildNames([ ], node1);

        Assert.Equal(4, dictionary.Count);
    }

    [Fact]
    public void CanAddMultipleHandlers()
    {
        var dictionary = new PathNodeDictionary();
        dictionary.AddMethodHandlers(
        [
            new MethodHandler("/root1/node1"),
            new MethodHandler("/root1/node2"),
        ]);

        PathNode root = dictionary["/"];
        Assert.Null(root.Parent);
        Assert.Null(root.MethodHandler);
        AssertChildNames([ "root1" ], root);

        PathNode root1 = dictionary["/root1"];
        Assert.Equal(root, root1.Parent);
        Assert.Null(root1.MethodHandler);
        AssertChildNames([ "node1", "node2" ], root1);

        PathNode node1 = dictionary["/root1/node1"];
        Assert.Equal(root1, node1.Parent);
        Assert.NotNull(node1.MethodHandler);
        AssertChildNames([ ], node1);

        PathNode node2 = dictionary["/root1/node2"];
        Assert.Equal(root1, node1.Parent);
        Assert.NotNull(node1.MethodHandler);
        AssertChildNames([ ], node1);

        Assert.Equal(4, dictionary.Count);
    }

    [Fact]
    public void CanSetExistingNodeHandler()
    {
        var dictionary = new PathNodeDictionary();
        dictionary.AddMethodHandlers(
        [
            new MethodHandler("/root1/node1"),
        ]);

        dictionary.AddMethodHandlers(
        [
            new MethodHandler("/root1"),
        ]);

        PathNode root = dictionary["/"];
        Assert.Null(root.Parent);
        Assert.Null(root.MethodHandler);
        AssertChildNames([ "root1" ], root);

        PathNode root1 = dictionary["/root1"];
        Assert.Equal(root, root1.Parent);
        Assert.NotNull(root1.MethodHandler);
        AssertChildNames([ "node1" ], root1);

        PathNode node1 = dictionary["/root1/node1"];
        Assert.Equal(root1, node1.Parent);
        Assert.NotNull(node1.MethodHandler);
        AssertChildNames([ ], node1);

        Assert.Equal(3, dictionary.Count);
    }

    [Fact]
    public void BadPathRemovesAddedMethodHandlers()
    {
        var dictionary = new PathNodeDictionary();
        Assert.Throws<FormatException>(() =>
        dictionary.AddMethodHandlers(
        [
            new MethodHandler("/root1/node1"),
            new MethodHandler("invalid_path"),
        ]));

        Assert.Equal(0, dictionary.Count);
    }

    [Fact]
    public void RemoveHandlersDoesntRemovePreExistingHandlers()
    {
        var dictionary = new PathNodeDictionary();

        dictionary.AddMethodHandlers(
        [
            new MethodHandler("/root1"),
        ]);

        Assert.Throws<FormatException>(() =>
        dictionary.AddMethodHandlers(
        [
            new MethodHandler("/root1/node1"),
            new MethodHandler("invalid_path"),
        ]));

        PathNode root = dictionary["/"];
        Assert.Null(root.Parent);
        Assert.Null(root.MethodHandler);
        AssertChildNames([ "root1" ], root);

        PathNode root1 = dictionary["/root1"];
        Assert.Equal(root, root1.Parent);
        Assert.NotNull(root1.MethodHandler);
        AssertChildNames([ ], root1);
    }

    [Fact]
    public void RemoveHandlersDoesntRemovePreExistingParentNodes()
    {
        var dictionary = new PathNodeDictionary();

        dictionary.AddMethodHandlers(
        [
            new MethodHandler("/root1/node2"),
        ]);

        Assert.Throws<FormatException>(() =>
        dictionary.AddMethodHandlers(
        [
            new MethodHandler("/root1/node1"),
            new MethodHandler("invalid_path"),
        ]));

        PathNode root = dictionary["/"];
        Assert.Null(root.Parent);
        Assert.Null(root.MethodHandler);
        AssertChildNames([ "root1" ], root);

        PathNode root1 = dictionary["/root1"];
        Assert.Equal(root, root1.Parent);
        Assert.Null(root1.MethodHandler);
        AssertChildNames([ "node2" ], root1);
    }

    [Fact]
    public void Remove()
    {
        var dictionary = new PathNodeDictionary();
        dictionary.AddMethodHandlers(
        [
            new MethodHandler("/root1"),
        ]);
        Assert.Equal(2, dictionary.Count);

        dictionary.RemoveMethodHandler("/root1");

        Assert.Equal(0, dictionary.Count);
    }

    [Fact]
    public void RemoveRemovesUnneededParentNodes()
    {
        var dictionary = new PathNodeDictionary();
        dictionary.AddMethodHandlers(
        [
            new MethodHandler("/root1/node1"),
        ]);
        Assert.Equal(3, dictionary.Count);

        dictionary.RemoveMethodHandler("/root1/node1");

        Assert.Equal(0, dictionary.Count);
    }

    [Fact]
    public void RemoveRetainsNeededParentNodes()
    {
        var dictionary = new PathNodeDictionary();
        dictionary.AddMethodHandlers(
        [
            new MethodHandler("/root1"),
            new MethodHandler("/root1/node1"),
        ]);
        Assert.Equal(3, dictionary.Count);

        dictionary.RemoveMethodHandler("/root1/node1");

        Assert.Equal(2, dictionary.Count);

        PathNode root = dictionary["/"];
        Assert.Null(root.Parent);
        Assert.Null(root.MethodHandler);
        AssertChildNames([ "root1" ], root);

        PathNode root1 = dictionary["/root1"];
        Assert.Equal(root, root1.Parent);
        Assert.NotNull(root1.MethodHandler);
        AssertChildNames([ ], root1);
    }

    [Fact]
    public void RemoveRetainsForChildNodes()
    {
        var dictionary = new PathNodeDictionary();
        dictionary.AddMethodHandlers(
        [
            new MethodHandler("/root1"),
            new MethodHandler("/root1/node1"),
        ]);
        Assert.Equal(3, dictionary.Count);

        dictionary.RemoveMethodHandler("/root1");

        PathNode root = dictionary["/"];
        Assert.Null(root.Parent);
        Assert.Null(root.MethodHandler);
        AssertChildNames([ "root1" ], root);

        PathNode root1 = dictionary["/root1"];
        Assert.Equal(root, root1.Parent);
        Assert.Null(root1.MethodHandler);
        AssertChildNames([ "node1" ], root1);

        PathNode node1 = dictionary["/root1/node1"];
        Assert.Equal(root1, node1.Parent);
        Assert.NotNull(node1.MethodHandler);
        AssertChildNames([ ], node1);

        Assert.Equal(3, dictionary.Count);
    }

    [Fact]
    public void RemoveMultiple()
    {
        var dictionary = new PathNodeDictionary();
        dictionary.AddMethodHandlers(
        [
            new MethodHandler("/root1"),
            new MethodHandler("/root2"),
        ]);
        Assert.Equal(3, dictionary.Count);

        dictionary.RemoveMethodHandlers([ "/root1", "/root2" ]);

        Assert.Equal(0, dictionary.Count);
    }

    [Fact]
    public void TreeHandlerAtRootHandlesChildPaths()
    {
        var dictionary = new PathNodeDictionary();
        var treeHandler = new TreeMethodHandler("/");
        dictionary.AddMethodHandlers([treeHandler]);

        // Root should be found directly
        dictionary.TryGetValue("/", out var handler, out var pathNode);
        Assert.Equal(treeHandler, handler);

        // Child paths should find the root tree handler
        dictionary.TryGetValue("/child", out handler, out pathNode);
        Assert.Equal(treeHandler, handler);

        dictionary.TryGetValue("/child/grandchild", out handler, out pathNode);
        Assert.Equal(treeHandler, handler);
    }

    [Fact]
    public void TreeHandlerAtIntermediatePathHandlesChildren()
    {
        var dictionary = new PathNodeDictionary();
        var treeHandler = new TreeMethodHandler("/org/example");
        dictionary.AddMethodHandlers([treeHandler]);

        // Tree handler path should be found directly
        dictionary.TryGetValue("/org/example", out var handler, out var pathNode);
        Assert.Equal(treeHandler, handler);

        // Child paths should find the tree handler
        dictionary.TryGetValue("/org/example/child", out handler, out pathNode);
        Assert.Equal(treeHandler, handler);

        dictionary.TryGetValue("/org/example/child/grandchild", out handler, out pathNode);
        Assert.Equal(treeHandler, handler);

        // Paths outside the tree handler's scope should not find it
        dictionary.TryGetValue("/org", out handler, out pathNode);
        Assert.Null(handler);

        dictionary.TryGetValue("/org/other", out handler, out pathNode);
        Assert.Null(handler);
    }

    [Fact]
    public void CannotRegisterHandlerUnderTreeHandler()
    {
        var dictionary = new PathNodeDictionary();
        dictionary.AddMethodHandlers([new TreeMethodHandler("/org/example")]);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            dictionary.AddMethodHandlers([new MethodHandler("/org/example/child")]));
    }

    [Fact]
    public void CannotRegisterTreeHandlerWhenChildrenExist()
    {
        var dictionary = new PathNodeDictionary();
        dictionary.AddMethodHandlers([new MethodHandler("/org/example/child")]);

        // Attempting to register a tree handler when children exist should fail
        var exception = Assert.Throws<InvalidOperationException>(() =>
            dictionary.AddMethodHandlers([new TreeMethodHandler("/org/example")]));
    }

    [Fact]
    public void TryGetValueReturnsNullForInvalidPaths()
    {
        var dictionary = new PathNodeDictionary();
        dictionary.AddMethodHandlers([new TreeMethodHandler("/org/example")]);

        dictionary.TryGetValue("noleadingslash", out var handler, out var pathNode);
        Assert.Null(handler);

        dictionary.TryGetValue("/double//slash", out handler, out pathNode);
        Assert.Null(handler);

        dictionary.TryGetValue("", out handler, out pathNode);
        Assert.Null(handler);

        dictionary.TryGetValue(null, out handler, out pathNode);
        Assert.Null(handler);
        Assert.Null(pathNode);
    }

    [Fact]
    public void TryGetValueReturnsNullWhenNoHandlersRegistered()
    {
        var dictionary = new PathNodeDictionary();

        dictionary.TryGetValue("/org/example", out var handler, out var pathNode);
        Assert.Null(handler);
        Assert.Null(pathNode);
    }

    [Fact]
    public void NonTreeHandlerDoesNotHandleChildPaths()
    {
        var dictionary = new PathNodeDictionary();
        var regularHandler = new MethodHandler("/org/example");
        dictionary.AddMethodHandlers([regularHandler]);

        // Regular handler should be found at its exact path
        dictionary.TryGetValue("/org/example", out var handler, out var pathNode);
        Assert.Equal(regularHandler, handler);

        // Child paths should not find the regular handler
        dictionary.TryGetValue("/org/example/child", out handler, out pathNode);
        Assert.Null(handler);
    }

    [Fact]
    public void TreeHandlerSearchContinuesPastNonTreeHandler()
    {
        var dictionary = new PathNodeDictionary();
        var regularHandler = new MethodHandler("/org");
        var treeHandler = new TreeMethodHandler("/org/example");
        dictionary.AddMethodHandlers([regularHandler, treeHandler]);

        dictionary.TryGetValue("/org/example/child", out var handler, out var pathNode);
        Assert.Equal(treeHandler, handler);

        dictionary.TryGetValue("/org", out handler, out pathNode);
        Assert.Equal(regularHandler, handler);
    }

    private void AssertChildNames(string[] expectedChildNames, PathNode node)
    {
        var methodContext = new MethodContext(null!, null!, default);
        node.SetIntrospectChildNames(methodContext);
        Assert.NotNull(methodContext.IntrospectChildNames);
        Assert.Equal(expectedChildNames.ToHashSet(), methodContext.IntrospectChildNames.ToHashSet());
    }
}