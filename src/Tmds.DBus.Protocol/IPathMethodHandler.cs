namespace Tmds.DBus.Protocol;

/// <summary>
/// Handles D-Bus method calls for a specific object path.
/// </summary>
public interface IPathMethodHandler
{
    /// <summary>
    /// Object path handled by this method handler.
    /// </summary>
    string Path { get; }

    /// <summary>
    /// Returns whether the handler also handles child paths.
    /// </summary>
    bool HandlesChildPaths { get; }

    /// <summary>
    /// Handles a method call.
    /// </summary>
    /// <param name="context">The method context containing the request information and methods to reply.</param>
    /// <remarks>
    /// No additional messages are read from the connection until this method completes.
    /// To handle the request asynchronously after this method completes, set <see cref="MethodContext.DisposesAsynchronously"/>. In this case, you are responsible for disposing the context once handling completes.
    /// </remarks>
    ValueTask HandleMethodAsync(MethodContext context);
}
