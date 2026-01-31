# Tmds.DBus.Protocol.SourceGenerator

`Tmds.DBus.Protocol.SourceGenerator` is a Roslyn source generator that automatically generates C# proxy types from D-Bus interface XML files at compile time.

## Setup your project

`Tmds.DBus.Protocol.SourceGenerator` does not add runtime dependencies to your application. You must also add a reference to `Tmds.DBus.Protocol` to provide the APIs that are targetted by the generator.

D-Bus interface XML files are added to the project file through `AdditionalFiles` elements. These attributes are available:
* `Include`: (required) path to the XML file. A file may include multiple interfaces.
* `Namespace`: (required) .NET namespace for the generated types. The same namespace may be used by different `AdditionalFile` elements.
* `GenerateDBusTypes`: (required) Set to `true` to enable the code generation.

The following shows an example project file:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Tmds.DBus.Protocol" Version="*" />
    <PackageReference Include="Tmds.DBus.Protocol.SourceGenerator" Version="*" />
  </ItemGroup>

  <ItemGroup>
    <AdditionalFiles Include="<path>" Namespace="<namespace>" GenerateDBusTypes="true" />
  </ItemGroup>
</Project>
```

You can find XML interface files in system directories like `/usr/share/dbus-1/interfaces/`. They are sometimes included as part of applications, or they can be found in source repositories of applications implementing them or organizations manging the specification.

If you write a library that targets `netstandard2.0` then you should configure your project to use `<Langversion>` of `11` or higher/`latest`. Additionally, you need a reference to the [PolySharp](https://www.nuget.org/packages/PolySharp) package.

## Generated Code

For each D-Bus interface in the XML file, the source generator creates a C# class with methods, properties, and signal handlers. Consider this simplified example:

```xml
<AdditionalFiles Include="org.mpris.MediaPlayer2.Player.xml" Namespace="Mpris.DBus" GenerateDBusTypes="true" />
```

```xml
<?xml version="1.0" ?>
<node>
  <interface name="org.mpris.MediaPlayer2.Player">
    <method name="Play" />
    <method name="Seek">
      <arg direction="in" type="x" name="Offset"/>
    </method>
    <property name="Volume" type="d" access="readwrite"/>
    <signal name="Seeked">
      <arg name="Position" type="x"/>
    </signal>
  </interface>
</node>
```

The source generator produces a C# class named `Mpris.DBus.Player` (derived from the last component of the interface name) that inherits from <xref:Tmds.DBus.Protocol.DBusObject>.

The class can be instantiated through a constructor, or via an extension method that is generated for <xref:Tmds.DBus.Protocol.DBusService>:

```csharp
DBusConnection connection = ...;

// Using the constructor
var player = new Player(connection, "org.mpris.MediaPlayer2.vlc", "/org/mpris/MediaPlayer2");

// Using the extension method
var service = new DBusService(connection, "org.mpris.MediaPlayer2.vlc");
var player = service.CreatePlayer("/org/mpris/MediaPlayer2");
```

For each D-Bus method, the class includes a corresponding .NET method. The method takes arguments and has a return value corresponding to the D-Bus method.

```csharp
await player.PlayAsync();
await player.SeekAsync(5000);
```

For signals, there is a watch method. To stop watching, you must dispose the `IDisposable` that is returned by the method. 

```csharp
IDisposable disposable = await player.WatchSeekedAsync((Exception? ex, long position) =>
{
  if (ex is not null)
  {
    // Watching stopped due to an exception.
    return;
  }

  Console.WriteLine($"Playback position changed to {position} microseconds");
});
```

For properties, methods are generated for each property to get and set it. There is also a method that enables getting all properties, and one for getting change notifications:

```csharp
// Get a property
double volume = await player.GetVolumeAsync();

// Set a property
await player.SetVolumeAsync(0.8);

// Watch for property changes
PlayerProperties props = (await player.GetPropertiesAsync()).EnsureAllPropertiesSet();
Console.WriteLine($"Volume: {props.Volume}"); // IPlayerProperties.Volume is double?

// Watch for property changes. This returns an IDisposable for unsubscribing.
await player.WatchPropertiesChangedAsync(async (Exception? ex, IPlayerProperties changed) =>
{
    if (ex is null)
    {
        if (changed.HasVolumeChanged)
        {
            double volume = changed.Volume ?? await player.GetVolumeAsync();
            Console.WriteLine($"Volume changed to {volume}");
        }
    }
});
```

The `GetPropertiesAsync` and `WatchPropertiesChangedAsync` return a C# interface named `IXxxProperties`. This interface contains all readable D-Bus properties as nullable properties. When a property is not set, it returns `null`.

Usually, `GetPropertiesAsync` will include all properties. If that is the case, you can call `EnsureAllPropertiesSet` which returns the underlying type that has non-nullable accessors and avoids dealing with the nullability. This method throws an exception if any properties are not set:
```cs
PlayerProperties props = (await player.GetPropertiesAsync()).EnsureAllPropertiesSet();
```

D-Bus property changes may include the value, or it may only indicate the property has changed (i.e. _invalidated_). The `IXxxProperties` interface provides `HasXxxChanged` properties that indicate the property changed/invalidated. When `HasXxxChanged` returns `true`, the `Xxx` property may still return `null` if new value of the property was not included.
