using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Tmds.DBus.Protocol.Tests;

public class PathNodeDictionaryTests
{
    private class MethodHandler : IMethodHandler
    {
        public MethodHandler(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public ValueTask HandleMethodAsync(MethodContext context)
        {
            throw new NotImplementedException();
        }

        public bool RunMethodHandlerSynchronously(Message message)
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

    private void AssertChildNames(string[] expectedChildNames, PathNode node)
    {
        var methodContext = new MethodContext(null!, null!, default);
        node.CopyChildNamesTo(methodContext);
        if (methodContext.IntrospectChildNameList == null)
        {
            Assert.Empty(expectedChildNames);
        }
        else
        {
            Assert.Equal(expectedChildNames.ToHashSet(), methodContext.IntrospectChildNameList.ToHashSet());
        }
    }
}