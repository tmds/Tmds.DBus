namespace Tmds.DBus.Protocol;

public sealed partial class DBusConnection
{
    /// <summary>
    /// Identifies the source of an exception reported through <see cref="DBusConnectionOptions.OnException"/>.
    /// </summary>
    public enum ExceptionSource
    {
        /// <summary>
        /// The exception occurred while reading a signal/matched message.
        /// </summary>
        SignalReader,

        /// <summary>
        /// The exception occurred in a signal/match handler callback.
        /// </summary>
        SignalHandler,

        /// <summary>
        /// The exception occurred in a method handler.
        /// </summary>
        MethodHandler,

        /// <summary>
        /// The connection failed.
        /// </summary>
        ConnectionFailed,
    }

    /// <summary>
    /// Provides context about an exception that occurred.
    /// </summary>
    public sealed class ExceptionContext
    {
        internal ExceptionContext(Exception exception, ExceptionSource source, bool disconnectConnection)
        {
            Exception = exception;
            Source = source;
            DisconnectConnection = disconnectConnection;
        }

        /// <summary>
        /// Gets the exception that occurred.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Gets the source of the exception.
        /// </summary>
        public ExceptionSource Source { get; }

        /// <summary>
        /// Gets or sets whether the connection should be disconnected.
        /// </summary>
        /// <remarks>
        /// <para>The default value depends on the caller. See <see cref="DBusConnectionOptions.OnException"/> for details.</para>
        /// <para>For <see cref="ExceptionSource.ConnectionFailed"/>, setting this property has no effect.</para>
        /// </remarks>
        public bool DisconnectConnection { get; set; }
    }
}
