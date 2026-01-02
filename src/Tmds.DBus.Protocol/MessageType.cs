namespace Tmds.DBus.Protocol;

/// <summary>
/// Represents the type of a D-Bus message.
/// </summary>
public enum MessageType : byte
{
    /// <summary>
    /// A method call message.
    /// </summary>
    MethodCall = 1,
    /// <summary>
    /// A method return message containing the result of a method call.
    /// </summary>
    MethodReturn = 2,
    /// <summary>
    /// An error message indicating a method call failure.
    /// </summary>
    Error = 3,
    /// <summary>
    /// A signal emission.
    /// </summary>
    Signal = 4
}