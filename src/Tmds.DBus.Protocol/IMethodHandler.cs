namespace Tmds.DBus.Protocol;

#pragma warning disable CS0618 // IMethodHandler is obsolete.

[Obsolete($"Use '{nameof(IPathMethodHandler)}' instead.")]
public interface IMethodHandler
{
    // Path that is handled by this method handler.
    string Path { get; }

    // The message argument is only valid during the call. It must not be stored to extend its lifetime.
    ValueTask HandleMethodAsync(MethodContext context);

    // Controls whether to wait for the handler method to finish executing before reading more messages.
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
                    context.Dispose(force: true);
                }
                catch (Exception ex)
                {
                    context.Disconnect(ex);
                }
            }
        }
    }
}
