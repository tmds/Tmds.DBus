using System.Reflection;

namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    public object ReadVariant() => Read<object>();
}
