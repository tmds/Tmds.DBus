[![Travis](https://api.travis-ci.org/tmds/Tmds.DBus.svg?branch=master)](https://travis-ci.org/tmds/Tmds.DBus)
[![NuGet](https://img.shields.io/nuget/v/Tmds.DBus.svg)](https://www.nuget.org/packages/Tmds.DBus)


# Introduction

From https://www.freedesktop.org/wiki/Software/dbus/

> D-Bus is a message bus system, a simple way for applications to talk to one another. In addition to interprocess communication, D-Bus helps coordinate process lifecycle; it makes it simple and reliable to code a "single instance" application or daemon, and to launch applications and daemons on demand when their services are needed.

Higher-level bindings are available for various popular frameworks and languages (Qt, GLib, Java, Python, etc.). [dbus-sharp](https://github.com/mono/dbus-sharp) (a fork of [ndesk-dbus](http://www.ndesk.org/DBusSharp)) is a C# implementation which targets Mono and .NET 2.0. Tmds.DBus builds on top of the protocol implementation of dbus-sharp and provides an API based on the asynchronous programming model introduced in .NET 4.5. The library targets netstandard 1.3 which means it runs on .NET 4.6 (Windows 7 and later) and .NET Core. You can get Tmds.DBus from NuGet.

# D-Bus Overview

D-Bus consists of a server daemon and clients connecting to the daemon. There is a system daemon (**system bus**) and there are daemons per user session (**session bus**).

Programs connecting to the bus can provide or consume services. A **service** exposes **objects** at specific **paths**. These objects implement **interfaces**, which contain **methods** (RPC targets) and **signals** (events). For example, the `org.freedesktop.NetworkManager` which lives on the *System bus*, exposes an object at `/org/freedesktop/NetworkManager`. This object implements the `org.freedesktop.DBus.Introspectable` interface to perform reflection, the `org.freedesktop.DBus.Properties` interface to get and set properties and the `org.freedesktop.NetworkManager` interface which has signals like `DeviceAdded` and methods such as `ActivateConnection` and `GetDevices`. This latter method returns an array of object paths pointing to other objects exposed by the service.

When a client connects to the daemon, it is assigned a *unique name*. In case it is a service provider, it will register specific *service names*. A service consumer can query the bus for the registered services. Certain services can also be started by the daemon (*activatable* services). This relies on configuration files which tell the daemon what application will provide a specific service.

The `org.freedesktop.DBus.Introspectable` interface has a single method `Introspect` which returns an XML string that describes the object. It contains the interfaces and their signals, methods and properties. It also includes child nodes which describe objects below it in the object path.

Method arguments are annotated as input or output. Signal arguments are always output parameters. Each argument and property has a type-attribute which contains a *signature* string describing the type of the argument/property.

The following block shows an example from the dbus specification:
```
<!DOCTYPE node PUBLIC "-//freedesktop//DTD D-BUS Object Introspection 1.0//EN"
    "http://www.freedesktop.org/standards/dbus/1.0/introspect.dtd">
<node name="/com/example/sample_object">
    <interface name="com.example.SampleInterface">
    <method name="Frobate">
        <arg name="foo" type="i" direction="in"/>
        <arg name="bar" type="s" direction="out"/>
        <arg name="baz" type="a{us}" direction="out"/>
        <annotation name="org.freedesktop.DBus.Deprecated" value="true"/>
    </method>
    <method name="Bazify">
        <arg name="bar" type="(iiu)" direction="in"/>
        <arg name="bar" type="v" direction="out"/>
    </method>
    <method name="Mogrify">
        <arg name="bar" type="(iiav)" direction="in"/>
    </method>
    <signal name="Changed">
        <arg name="new_value" type="b"/>
    </signal>
    <property name="Bar" type="y" access="readwrite"/>
    </interface>
    <node name="child_of_sample_object"/>
    <node name="another_child_of_sample_object"/>
</node>
```
These primitive types are defined in the specification:

Conventional name | Signature | Description
------------------|-----------|------------
BYTE	          | y         | Unsigned 8-bit integer
BOOLEAN	          | b         |	Boolean value: 0 is false, 1 is true, any other value allowed by the marshalling format is invalid
INT16	          | n         |	Signed (two's complement) 16-bit integer
UINT16	          | q         |	Unsigned 16-bit integer
INT32	          | i         |	Signed (two's complement) 32-bit integer
UINT32	          | u         |	Unsigned 32-bit integer
INT64	          | x         |	Signed (two's complement) 64-bit integer (mnemonic: x and t are the first characters in "sixty" not already used for something more common)
UINT64	          | t         |	Unsigned 64-bit integer
DOUBLE	          | d         |	IEEE 754 double-precision floating point
UNIX_FD	          | h         |	Unsigned 32-bit integer representing an index into an out-of-band array of file descriptors, transferred via some platform-specific mechanism (mnemonic: h for handle)
STRING	          | s         |	No extra constraints
OBJECT_PATH	      | o         |	Must be a syntactically valid object path
SIGNATURE	      | g         |	Zero or more single complete types

It is also possible to create aggregate types called **STRUCTS**. Structs signatures are created by concatenating the members and enclosing them with parentheses. For example `(si)` is a struct containing a string and a 32-bit integer.

**ARRAY** types are described by prefixing a type with 'a'. For example, `ao` is an array of object paths.

**DICTIONARIES** are a special type of array with a key and a value type. Their signature string is `a{<keytype><valuetype>}`. For example `a{is}` is a dictionary which maps 32-bit signed integers to strings.

There is also an any type (**VARIANT**) which can contain one other type. This variant's signature string is `v`.

Combining these rules allows us to interprete a signature found in the introspection XML. For example: `aa{sv}` describes an array (`a`) of dictionaries `a{}` with a string key (`s`) and an variant (`v`) value.

You can learn more about D-Bus at https://www.freedesktop.org/wiki/Software/dbus/.

# Modelling

## Objects Types and Interfaces

To model a D-Bus interface using Tmds.MDns we create a .NET interface with the `DBusInterface` attribute and inherit `IDBusObject`.

```
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

```
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
UNIX_FD	          | h         |	not supported
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

The `float` type is not part of the D-Bus specification. It was implemented as part of ndesk-dbus and is supported by Tmds.DBus. The UNIX_FD type is not supported by Tmds.DBus.

## Methods

A D-Bus method is modeled by a method in the .NET interface. The method must to return `Task` for methods without a return value and `Task<T>` for methods with a return value. Following async naming conventions, we add `Async` to the method name. In case a method has multiple out-arguments, these must be combined in a struct/class as public fields or a C# 7 tuple. The input arguments of the D-Bus method are combined with a final `CancellationToken` parameter.

```
[DBusInterface("org.mpris.MediaPlayer2.TrackList")]
public interface ITrackList
{
    // 1 input argument with signature: `ao`
    // 1 output parameter with signature `aa{sv}`
    Task<IDictionary<string, object>[]> GetTracksMetadataAsync(ObjectPath[] trackIds, CancellationToken cancellationToken = default(CancellationToken));
}
```

To differentiate between a single output parameter of type STRUCT and multiple output parameters, use the `Argument` attribute to indicate the return value represents a single argument in the STRUCT case.

```
struct RetVal
{
    public string arg1;
    public int    arg2;
}
[DBusInterface("tmds.dbus.example.structret")]
public interface ITrackList
{
    // 2 output parameter with signatures `s` and `i`
    Task<RetVal> MultipleOutAsync(CancellationToken cancellationToken = default(CancellationToken));
    // 1 output parameter with signatures `(si)`
    [ret:Argument]
    Task<RetVal> SingleStructOutAsync(CancellationToken cancellationToken = default(CancellationToken));
}

// or using C# 7 tuples
[DBusInterface("tmds.dbus.example.structret")]
public interface ITrackList
{
    // 2 output parameter with signatures `s` and `i`
    Task<(string arg1, int arg2)> MultipleOutAsync(CancellationToken cancellationToken = default(CancellationToken));
    // 1 output parameter with signatures `(si)`
    [ret:Argument]
    Task<(string arg1, int arg2)> SingleStructOutAsync(CancellationToken cancellationToken = default(CancellationToken));
}
```

In case the return type of a method is `Task<object>` the method may me modeled as a generic method of Task<T>. This makes it unnecessary to cast the returned value when calling the method.

```
[DBusInterface("tmds.dbus.example.variantreturn")]
public interface ITrackList
{
    // user needs to cast, e.g. (ObjectPath)(await FooAsync())
    Task<object> FooAsync(CancellationToken cancellationToken = default(CancellationToken));
    // return value already casted, e.g. await BarAsync<ObjectPath>()
    Task<T> BarAsync<T>(CancellationToken cancellationToken = default(CancellationToken));
}
```

## Signals

A D-Bus signal is modeled by a method in the .NET interface which matches the D-Bus signal name prefixed with `Watch` and sufixed with `Async` suffix. The method needs to return `Task<IDisposable>`. The returned `IDisposable` can be used to unsubscribe from the signal. The method has a handler and a `CancellationToken` parameter. The handler must be of type `Action` for signals without parameters and of type `Action<T>` for methods which do have parameters. In case there are multiple parameters, they must be wrapped in a struct/class as public fields or use a C# 7 tuple. Similar to the method output parameter, the `ArgumentAttribute` can be set on the `Action` to distinguish between a single STRUCT being returned (attribute set) or multiple arguments (no attribute).

```
[DBusInterface("org.freedesktop.NetworkManager")]
public interface INetworkManager : IDBusObject
{
    // DeviceAdded signal with a single ObjectPath argument
    Task<IDisposable> WatchDeviceAddedAsync(Action<ObjectPath> handler, CancellationToken cancellationToken = default(CancellationToken))));
}
```

## Properties

Properties are defined per interface and accessed using `org.freedesktop.DBus.Properties`. This interface includes a signal for change detection but it depends on the object or even the specific property whether the signal is emitted. The `org.freedesktop.DBus.Properties` interface is modeled by adding a `GetAsync`, `SetAsync`, `GetAllAsync` and `WatchPropertiesAsync` with specific signatures to the D-Bus interface.

```
[DBusInterface("org.mpris.MediaPlayer2.TrackList")]
public interface ITrackList
{
    Task<T> GetAsync<T>(string prop, CancellationToken cancellationToken = default(CancellationToken));
    Task<IDictionary<string, object>> GetAllAsync(CancellationToken cancellationToken = default(CancellationToken));
    Task SetAsync(string prop, object val, CancellationToken cancellationToken = default(CancellationToken));
    Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler, CancellationToken cancellationToken = default(CancellationToken));
}
```

You may want to list properties in a separate class and even define extension methods to access them.

```
class TrackListProperties
{
    public ObjectPath[] Tracks;
}

static class TrackListPropertyExtensions
{
    public static Task<ObjectPath[]> GetTracksAsync(this ITrackList trackList, CancellationToken cancellationToken = default(CancellationToken))
    {
        return trackList.GetAsync<ObjectPath[]>(nameof(TrackListProperties.Tracks), cancellationToken);
    }
}
```

By adding the `Dictionary` attribute to the properties class, we can use it as the return type of `GetAllAsync`.

```
[Dictionary]
class TrackListProperties
{
    // ...
}

[DBusInterface("org.mpris.MediaPlayer2.TrackList")]
public interface ITrackList
{
    // ...
    Task<TrackListProperties> GetAllAsync(CancellationToken cancellationToken = default(CancellationToken));
    // ...
}
```

# Example

In this section we'll write a simple application that retrieves the introspection XML of a D-Bus object. Our program will take 3 arguments. The first argument must be `--session` or `--system` to connect to the session bus or system bus. The next two arguments specify the service name and the object path.

If you don't have dotnet on your machine, install it from http://dot.net.

First we create a `project.json` file and list `Tmds.DBus` as a dependency. You can use the latest version for the Tmds.DBus package available on NuGet: https://www.nuget.org/packages/Tmds.DBus/.

```
{
  "buildOptions": {
    "emitEntryPoint": true
  },
  "dependencies": {
    "Tmds.DBus": "0.3.0",
    "Microsoft.NETCore.App": {
      "type": "platform",
      "version": "1.0.0"
    }
  },
  "frameworks": {
    "netcoreapp1.0": { }
  }
}
```

Now we can fetch our dependencies by executing.

```
dotnet restore
```

Create a new file `Program.cs` with an empty `Introspect.Program` class.

```
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace Introspect
{
    public class Program
    {
	}
}
```

Let's add our `Main` method to the `Program` class. The `Main` method will parse the command line arguments and pass them to `InspectAsync`.

```
public static int Main(string[] args)
{
    if (args.Length != 3)
    {
        Console.WriteLine("Usage: --session/--system <servicename> <objectpath>");
        return -1;
    }
    bool sessionNotSystem = args[0] != "--system";
    var service = args[1];
    var objectPath = args[2];
    Task.Run(() => InspectAsync(sessionNotSystem, service, objectPath)).Wait();
    return 0;
}
```

The `org.freedesktop.DBus.Introspectable` interface is described in the D-Bus specification:

> This interface has one method:
>
>  org.freedesktop.DBus.Introspectable.Introspect (out STRING xml_data)
>
>
> Objects instances may implement Introspect which returns an XML description of the object, including its interfaces (with signals and methods), objects below it in the object path tree, and its properties.

As explained in this document, we can model this interface as shown in the next code block. Add the `IIntrospectable` interface in the `Introspect` namespace above the class definition.

```
[DBusInterface("org.freedesktop.DBus.Introspectable")]
public interface IIntrospectable : IDBusObject
{
    Task<string> IntrospectAsync(CancellationToken cancellationToken = default(CancellationToken));
}
```

We'll now add `InspectAsync` to the `Program` class. `InspectAsync` connects to D-Bus and then creates a proxy object that implements the `IIntrospectable` interface. This object is used to call the `Introspect` method and the return value is printed out.

```
private static async Task InspectAsync(bool sessionNotSystem, string serviceName, string objectPath)
{
    using (var connection = new Connection(sessionNotSystem ? Address.Session : Address.System))
    {
        await connection.ConnectAsync(OnDisconnect);
        var introspectable = connection.CreateProxy<IIntrospectable>(serviceName, objectPath);
        var xml = await introspectable.IntrospectAsync();
        Console.WriteLine(xml);
    }
}
```

In our connect call we specify an `OnDisconnect` callback. This method is called when we disconnect by calling `Dispose` or when we unexpectedly get thrown of the bus. In the latter case the exception argument will be set.

```
public static void OnDisconnect(Exception e)
{
    if (e != null)
    {
        Console.WriteLine($"Connection closed: {e.Message}");
    }
}
```

We're done coding. After we've compiled our program, we can run it to inspect an object.

```
dotnet build
dotnet run --system org.freedesktop.NetworkManager /org/freedesktop/NetworkManager
```

The complete code can be found under `samples/Introspect`
