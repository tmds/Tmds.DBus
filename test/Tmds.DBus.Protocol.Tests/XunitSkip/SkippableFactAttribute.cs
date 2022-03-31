using Xunit;
using Xunit.Sdk;

namespace XunitSkip
{
    [XunitTestCaseDiscoverer("XunitSkip.XunitExtensions.SkippableFactDiscoverer", "Tmds.DBus.Tests")]
    public class SkippableFactAttribute : FactAttribute { }
}
