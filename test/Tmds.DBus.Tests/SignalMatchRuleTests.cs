using Xunit;

namespace Tmds.DBus.Tests
{
    public class SignalMatchRuleTests
    {
        [Fact]
        public void StringBuilder()
        {
            var rule = new SignalMatchRule {
                Interface = "org.example.Test",
                Member = "TestSignal",
                Path = "/org/example/test",
            };

            Assert.Equal(rule.ToString(),
                "type='signal',interface='org.example.Test',member='TestSignal',path='/org/example/test'");

            rule.Args = new[] { (2, "arg 2") };
            rule.ArgPaths = new[] { (1, "/org/example/test/arg1") };
            rule.Arg0Namespace = "/org/example/test/arg0";

            Assert.Equal(rule.ToString(),
                "type='signal',interface='org.example.Test',member='TestSignal',path='/org/example/test',arg2='arg 2',arg1path='/org/example/test/arg1',arg0namespace='/org/example/test/arg0'");
        }

        [Fact]
        public void Equality()
        {
            var rule1 = new SignalMatchRule {
                Interface = "org.example.Test",
                Member = "TestSignal",
                Path = "/org/example/test",
                Args = new[] { (2, "arg 2") },
                ArgPaths = new[] { (1, "/org/example/test/arg1") },
                Arg0Namespace = "/org/example/test/arg0",
            };

            var rule2 = new SignalMatchRule {
                Interface = "org.example.Test",
                Member = "TestSignal",
                Path = "/org/example/test",
                Args = new[] { (2, "arg 2") },
                ArgPaths = new[] { (1, "/org/example/test/arg1") },
                Arg0Namespace = "/org/example/test/arg0",
            };

            Assert.Equal(rule1, rule2);

            var rule3 = new SignalMatchRule{
                Interface = "org.example.Test",
                Member = "TestSignal",
                Path = "/org/example/test",
            };

            Assert.NotEqual(rule1, rule3);
        }
    }
}
