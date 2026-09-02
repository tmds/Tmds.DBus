using System.Collections.Generic;
using Xunit;

namespace Tmds.DBus.Protocol.Tests;

public class MatchRuleTests
{
    [Theory, MemberData(nameof(MatchRuleTestData))]
    public void MatchRule_ToString(MatchRule rule, string expected)
    {
        Assert.Equal(rule.ToString(), expected);
    }

    public static IEnumerable<object[]> MatchRuleTestData
    {
        get
        {
            yield return new object[]
            {
                new MatchRule
                {
                    Type = MessageType.Signal,
                    Sender = "org.freedesktop.DBus",
                    Interface = "org.freedesktop.DBus",
                    Member = "NameOwnerChanged",
                    Path = "/org/freedesktop/DBus",
                    PathNamespace = "/org/freedesktop/DBus",
                    Destination = ":1.0",
                    Arg0 = "org.example.FakeName",
                    Arg0Path = "/org/example/",
                    Arg0Namespace = "org.example",
                },
                "type=signal,sender=org.freedesktop.DBus,interface=org.freedesktop.DBus,member=NameOwnerChanged,path=/org/freedesktop/DBus,path_namespace=/org/freedesktop/DBus,destination=:1.0,arg0=org.example.FakeName,arg0path=/org/example/,arg0namespace=org.example"
            };
        }
    }
}