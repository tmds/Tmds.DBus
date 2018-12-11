# Modelling

## Objects Types and Interfaces

To model a D-Bus interface using Tmds.DBus we create a .NET interface with the `DBusInterface` attribute and inherit `IDBusObject`.

```cs
[DBusInterface("org.mpris.MediaPlayer2.Player")]
public interface IPlayer : IDBusObject
{
    // Player members
}

[DBusInterface("org.mpris.MediaPlayer2.TrackList")]
public interface ITrackList : IDBusObject
{
    // TrackList members
}
```

To model a D-Bus ab object type we can either create an empty interface and make it inherit the D-Bus interfaces of the object. Or we can choose one of the interfaces which is typical for that object and make that inherit the other interfaces. In case we we only need one interface of an object, there is no need to model the object separately.

```cs
// option 1
public interface IPlayerObject : IPlayer, ITrackList
{
    // empty
}
// option2
[DBusInterface("org.mpris.MediaPlayer2.Player")]
public interface IPlayer : ITrackList
{
    // Player members
}
```

## Argument Types

The D-Bus types are modeled using the following .NET types.

Conventional name | Signature | .NET Type (C# type)
------------------|-----------|--------------------
BYTE	          | y         | System.Byte (byte)
BOOLEAN	          | b         |	System.Boolean (bool)
INT16	          | n         |	System.Int16 (short)
UINT16	          | q         |	System.UInt16 (ushort)
INT32	          | i         |	System.Int32 (int)
UINT32	          | u         |	System.UInt32 (uint)
INT64	          | x         |	System.Int64 (long)
UINT64	          | t         |	System.UInt64 (ulong)
N/A    	          | f         |	System.Single (float)
DOUBLE	          | d         |	System.Double (double)
UNIX_FD	          | h         |	SafeHandle derived type
STRING	          | s         |	System.String (string)
OBJECT_PATH	      | o         |	ObjectPath, IDBusObject, D-Bus interface, D-Bus object type interface
SIGNATURE	      | g         |	N/A
ARRAY             | a.        | T[], IEnumerable<>, IList<>, ICollection<>
DICTIONARY        | a{..}     | IDictionary, ARRAY of KeyValuePair<,>
SV DICTIONARY     | a{sv}     | [Dictionary] class or struct: public and non-public instance fields
STRUCT            | (...)     | [StructLayout(LayoutKind.Sequential)] class or struct: public and non-public instance fields, C# 7 tuple
VARIANT           | v         | object

The preferred type to represent an ARRAY is `T[]`.
A DICTIONARY can be represented as `IDictionary<TKey, TValue>` but also as `KeyValuePair<TKey, TValue>[]`. The latter can be used to avoid the overhead of adding the elements to a dictionary class.
A STRING to VARIANT DICTIONARY can also be modelled by a class/struct which is decorated with the `Dictionary` attribute. Each field maps to a dictionary entry. The fields may be `null` (nullable structures are also supported). When the dictionary is written, `null` values are skipped. When the dictionary is read, fields without a matching entry will remain at their default value. Fields not mapped in the class/struct are ignored. See the Properties section below for an example.

When an `object` is serialized as a VARIANT, it's underlying type is determined as follows. If the instance type exactly matches one of the types in the above table, that is type used. In case the instance implements `IEnumerable<KeyValuePair<,>` it is serialized as a DICTIONARY. In case it implements the more generic `IEnumerable<>` it is serialized as an ARRAY. If non of the previous match, the object is serialized as a STRUCT.

When an VARIANT is deserialized as an `object` the matching .NET type is used. ARRAY types are deserialized as `T[]`. DICTIONARY types are deserialized as `IDictionary<TKey,TValue>`. STRUCTS are deserialized as `System.ValueTuple`.

The `float` type is not part of the D-Bus specification. It was implemented as part of ndesk-dbus and is supported by Tmds.DBus.

`SafeHandle` derived types can be used to receive and send file descriptors. The derived type must have a `(IntPtr, Boolean)` constructor. When a `SafeHandle` is sent, it will be Disposed. Tmds.DBus provides a `CloseSafeHandle` class, that can be used as a generic `SafeHandle`.

## Methods

A D-Bus method is modeled by a method in the .NET interface. The method must to return `Task` for methods without a return value and `Task<T>` for methods with a return value. Following async naming conventions, we add `Async` to the method name. In case a method has multiple out-arguments, these must be combined in a struct/class as public fields or a C# 7 tuple. The input arguments of the D-Bus method are the method parameters.

```cs
[DBusInterface("org.mpris.MediaPlayer2.TrackList")]
public interface ITrackList
{
    // 1 input argument with signature: `ao`
    // 1 output parameter with signature `aa{sv}`
    Task<IDictionary<string, object>[]> GetTracksMetadataAsync(ObjectPath[] trackIds);
}
```

To differentiate between a single output parameter of type STRUCT and multiple output parameters, use the `Argument` attribute to indicate the return value represents a single argument in the STRUCT case.

```cs
struct RetVal
{
    public string arg1;
    public int    arg2;
}
[DBusInterface("tmds.dbus.example.structret")]
public interface ITrackList
{
    // 2 output parameter with signatures `s` and `i`
    Task<RetVal> MultipleOutAsync();
    // 1 output parameter with signatures `(si)`
    [ret:Argument]
    Task<RetVal> SingleStructOutAsync();
}

// or using C# 7 tuples
[DBusInterface("tmds.dbus.example.structret")]
public interface ITrackList
{
    // 2 output parameter with signatures `s` and `i`
    Task<(string arg1, int arg2)> MultipleOutAsync();
    // 1 output parameter with signatures `(si)`
    [ret:Argument]
    Task<(string arg1, int arg2)> SingleStructOutAsync();
}
```

In case the return type of a method is `Task<object>` the method may me modeled as a generic method of `Task<T>`.

```cs
[DBusInterface("tmds.dbus.example.variantreturn")]
public interface ITrackList
{
    // user needs to cast, e.g. (ObjectPath)(await FooAsync())
    Task<object> FooAsync();
    // return value is typed, e.g. await BarAsync<ObjectPath>()
    Task<T> BarAsync<T>();
}
```

## Signals

A D-Bus signal is modeled by a method in the .NET interface which matches the D-Bus signal name prefixed with `Watch` and sufixed with `Async` suffix. The method needs to return `Task<IDisposable>`. The returned `IDisposable` can be used to unsubscribe from the signal. The method has a handler parameter. The handler must be of type `Action` for signals without parameters and of type `Action<T>` for methods which do have parameters. In case there are multiple parameters, they must be wrapped in a struct/class as public fields or use a C# 7 tuple. Similar to the method output parameter, the `ArgumentAttribute` can be set on the `Action` to distinguish between a single STRUCT being returned (attribute set) or multiple arguments (no attribute). A second action of type `Action<Exception>` can be specified. This action will be called when the Connection is closed.

```cs
[DBusInterface("org.freedesktop.NetworkManager")]
public interface INetworkManager : IDBusObject
{
    // DeviceAdded signal with a single ObjectPath argument
    Task<IDisposable> WatchDeviceAddedAsync(Action<ObjectPath> handler, Action<Exception> onError = null)));
}
```

## Properties

Properties are defined per interface and accessed using `org.freedesktop.DBus.Properties`. This interface includes a signal for change detection but it depends on the object or even the specific property whether the signal is emitted. The `org.freedesktop.DBus.Properties` interface is modeled by adding a `GetAsync`, `SetAsync`, `GetAllAsync` and `WatchPropertiesAsync` with specific signatures to the D-Bus interface.

```cs
[DBusInterface("org.mpris.MediaPlayer2.TrackList")]
public interface ITrackList
{
    Task<T> GetAsync<T>(string prop);
    Task<IDictionary<string, object>> GetAllAsync();
    Task SetAsync(string prop, object val);
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
}
```

You may want to list properties in a separate class and even define extension methods to access them.

```cs
class TrackListProperties
{
    public ObjectPath[] Tracks;
}

static class TrackListPropertyExtensions
{
    public static Task<ObjectPath[]> GetTracksAsync(this ITrackList trackList)
    {
        return trackList.GetAsync<ObjectPath[]>(nameof(TrackListProperties.Tracks));
    }
}
```

By adding the `Dictionary` attribute to the properties class, we can use it as the return type of `GetAllAsync`.

```cs
[Dictionary]
class TrackListProperties
{
    // ...
}

[DBusInterface("org.mpris.MediaPlayer2.TrackList")]
public interface ITrackList
{
    // ...
    Task<TrackListProperties> GetAllAsync();
    // ...
}
```

The fields names may be prefixed with an underscore. Dashes (which would make the field names invalid) can be replaced with underscores.
