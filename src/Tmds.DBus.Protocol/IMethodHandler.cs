namespace Tmds.DBus.Protocol;

#pragma warning disable CS0618 // IMethodHandler is obsolete.

/// <summary>
/// Handles D-Bus method calls for a specific object path. (obsolete)
/// </summary>
/// <remarks>Use <see cref="IPathMethodHandler"/> instead.</remarks>
[Obsolete($"Use '{nameof(IPathMethodHandler)}' instead.")]
public interface IMethodHandler
{
    /// <summary>
    /// Object path handled by this method handler.
    /// </summary>
    string Path { get; }

    /// <summary>
    /// Handles a method call.
    /// </summary>
    /// <param name="context">The method context containing the request information and methods to reply.</param>
    /// <remarks>
    /// No additional messages are read from the connection until this method completes when <see cref="RunMethodHandlerSynchronously"/> returns <see langword="true"/>.
    /// </remarks>
    ValueTask HandleMethodAsync(MethodContext context);

    /// <summary>
    /// Determines whether to wait for the handler method to finish executing before reading more messages.
    /// </summary>
    /// <param name="message">Message being handled.</param>
    bool RunMethodHandlerSynchronously(Message message);
}

static class MethodHandlerExtensions
{
    public static IPathMethodHandler AsPathMethodHandler(this IMethodHandler handler)
    {
        return new Adapter(handler);
    }

    sealed class Adapter : IPathMethodHandler
    {
        private IMethodHandler _handler;

        public Adapter(IMethodHandler handler)
        {
            _handler = handler;
        }

        public string Path => _handler.Path;

        public bool HandlesChildPaths => false;

        public ValueTask HandleMethodAsync(MethodContext context)
        {
            context.CanDispose = false;
            bool runsSync = _handler.RunMethodHandlerSynchronously(context.Request);
            ValueTask task = _handler.HandleMethodAsync(context);
            if (runsSync || task.IsCompleted)
            {
                return task;
            }
            else
            {
                RunAsync(task, context);
                return default;
            }

            async void RunAsync(ValueTask task, MethodContext context)
            {
                try
                {
                    context.CanDispose = true;
                    context.DisposesAsynchronously = true;
                    context.CanDispose = false;

                    await task.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    context.Disconnect(ex);
                }
                finally
                {
                    context.Dispose(force: true);
                }
            }
        }
    }
}
