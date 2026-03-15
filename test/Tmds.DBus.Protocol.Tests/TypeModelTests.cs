using System;
using Xunit;

namespace Tmds.DBus.Protocol.Tests;

public class TypeModelTests
{
    [Fact]
    public void EnsureSupportedVariantType_RejectsUnsupportedNestedType()
    {
        // Array with unsupported inner type should throw.
        Assert.Throws<NotSupportedException>(() => new Array<Array<DateTime>>());

        // Dict with unsupported value type should throw.
        Assert.Throws<NotSupportedException>(() => new Dict<string, Array<DateTime>>());

        // Dict with unsupported key type should throw.
        Assert.Throws<NotSupportedException>(() => new Dict<DateTime, string>());
    }

    [Fact]
    public void EnsureSupportedVariantType_AcceptsSupportedNestedType()
    {
        // These should not throw.
        _ = new Array<Array<int>>();
        _ = new Array<Dict<string, int>>();
        _ = new Dict<string, Array<string>>();
    }
}
