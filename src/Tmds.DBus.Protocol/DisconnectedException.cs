namespace Tmds.DBus.Protocol;

/// <summary>
/// Exception thrown when an operation fails because the connection was disconnected.
/// </summary>
/// <remarks>
/// The <see cref="Exception.InnerException"/> indicates the reason for the disconnect.
/// </remarks>
public class DisconnectedException : Exception
{
    internal DisconnectedException(Exception innerException) : base(innerException.Message, innerException)
    { }
}
