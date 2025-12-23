namespace Tmds.DBus.Protocol;

interface IMethodHandlerDictionary
{
    void AddMethodHandlers(IReadOnlyList<IPathMethodHandler> methodHandlers);
    void AddMethodHandler(IPathMethodHandler methodHandler);
    void RemoveMethodHandler(string path);
    void RemoveMethodHandlers(IEnumerable<string> paths);
}