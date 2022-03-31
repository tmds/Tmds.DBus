using Xunit;
using Xunit.Sdk;

namespace XunitSkip
{
    [XunitTestCaseDiscoverer("XunitSkip.XunitExtensions.SkippableTheoryDiscoverer", "Tmds.DBus.Tests")]
    public class SkippableTheoryAttribute : TheoryAttribute { }
}
