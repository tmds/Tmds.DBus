using Microsoft.CodeAnalysis;

namespace Tmds.DBus.Protocol.SourceGenerator
{
    internal static class DiagnosticDescriptors
    {
        private const string Category = "Tmds.DBus.Protocol.SourceGenerator";

        public static readonly DiagnosticDescriptor MissingNamespace = new DiagnosticDescriptor(
            id: "DBUS1001",
            title: "Missing Namespace metadata",
            messageFormat: "The AdditionalFile '{0}' is missing the required 'Namespace' metadata. Add build_metadata.AdditionalFiles.Namespace in your project file.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnsupportedGeneratorMode = new DiagnosticDescriptor(
            id: "DBUS1002",
            title: "Unsupported generator mode",
            messageFormat: "The AdditionalFile '{0}' uses an unsupported DBusGeneratorMode '{1}'. Only 'Proxy' mode is supported.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnableToReadFile = new DiagnosticDescriptor(
            id: "DBUS1003",
            title: "Unable to read file",
            messageFormat: "Unable to read the AdditionalFile '{0}'",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidXml = new DiagnosticDescriptor(
            id: "DBUS1004",
            title: "Invalid XML format",
            messageFormat: "The AdditionalFile '{0}' contains invalid XML: {1}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MissingInterfaceName = new DiagnosticDescriptor(
            id: "DBUS1005",
            title: "Missing interface name",
            messageFormat: "An <interface> element in '{0}' is missing the required 'name' attribute",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidRootElement = new DiagnosticDescriptor(
            id: "DBUS1006",
            title: "Invalid root element",
            messageFormat: "The AdditionalFile '{0}' must have a <node> element as its root element, but found '<{1}>' instead",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor DuplicateInterface = new DiagnosticDescriptor(
            id: "DBUS1007",
            title: "Duplicate interface name",
            messageFormat: "Interface '{0}' is defined multiple times in namespace '{1}'. Source files: {2}.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor DuplicateClassName = new DiagnosticDescriptor(
            id: "DBUS1008",
            title: "Duplicate class name",
            messageFormat: "Class name '{0}' is used for multiple interfaces in namespace '{1}'. Interface names: {2}. Source files: {3}.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor CodeGenerationFailed = new DiagnosticDescriptor(
            id: "DBUS1009",
            title: "Code generation failed",
            messageFormat: "Failed to generate code for interface '{0}' ('{1}') in namespace '{2}' from source file '{3}': {4}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor CodeGenerationFailedGeneric = new DiagnosticDescriptor(
            id: "DBUS1010",
            title: "Code generation failed",
            messageFormat: "Failed to generate code for namespace '{0}': {1}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}
