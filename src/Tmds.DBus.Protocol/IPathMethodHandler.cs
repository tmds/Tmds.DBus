namespace Tmds.DBus.Protocol;

public interface IPathMethodHandler
{
    // Path that is handled by this method handler.
    string Path { get; }

    // Whether the handler also handles child paths.
    bool HandlesChildPaths { get; }

    // Handles a method call.
    // No new messages are read until the method completes.
    // The handling can be done out-of-band by setting MethodContext.DisposesAsynchronously. When the async handling is done, the context must be disposed.
    ValueTask HandleMethodAsync(MethodContext context);
}
