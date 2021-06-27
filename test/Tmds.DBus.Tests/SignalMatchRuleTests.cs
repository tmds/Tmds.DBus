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

            Assert.Equal(rule.ToString(), "type='signal',interface='org.example.Test',member='TestSignal',path='/org/example/test'");
        }
    }
}
