# Tmds.DBus.Tool

The `Tmds.DBus.Tool` packages extends the dotnet cli to facilitate developing .NET Core applications that use D-Bus.

The tool supports:
- [codegen](#codegen): generate C# code for consuming D-Bus services.
- [list](#list): list the available objects, interfaces and services.
- [monitor](#monitor): prints message bus messages.

The tool can be installed using `dotnet tool install -g Tmds.DBus.Tool`

Now it can be invoked from the command line:
```
$ dotnet restore
$ dotnet dbus --help
```

## codegen

The `codegen` command generates C# based on a bus service or XML files. This C# code is meant as a starting point
and can be further enhanced by the developer (e.g. introducing strongly typed enumerations).

```
$ dotnet dbus codegen --service org.gnome.ScreenSaver
$ dotnet dbus codegen /usr/share/dbus-1/interfaces/*.xml
```

It is possible to use the system bus (or specify an address) using the `--bus` option:
```
$ dotnet dbus codegen --bus system --service org.freedesktop.NetworkManager
```

Additional argument allow to further control the behavior:
* `bus`: bus to use ('session', 'system', <address\>)
* `path`: start introspecting from a specific object path
* `no-recurse`: only introspect the object at `path`
* `namespace`: set the C# namespace
* `output`: set the filename to generate
* `skip`: don't generate C# for certain interfaces
* `interface`: only generate C# for certain interfaces
* `no-ivt`: don't add the InternalsVisibleTo Attribute
* `public`: generate the code with public access modifier to make the types visible to other assemblies

The `interface` argument can also be used to name the interfaces.

For example, by default `org.freedesktop.NetworkManager.Connection.Active` `ActiveConnection` is named `Active`. To name it `ActiveConnection`:
```
$ dotnet dbus codegen --bus system --service org.freedesktop.NetworkManager --interface org.freedesktop.NetworkManager.Connection.Active:ActiveConnection
```

*note*: The `list` command can be used to find out all interface names.

## list

The `list` command can be used to list `interfaces`, `objects`, `services` and `activatable-services`.

```
$ dotnet dbus list services
$ dotnet dbus list activatable-services
$ dotnet dbus list --service org.gnome.SessionManager objects
$ dotnet dbus list --service org.freedesktop.systemd1 interfaces
```

The output when listing objects includes the full object paths and the interfaces the objects implement.

Additional argument allow to further control the behavior:
* `bus`: bus to use ('session', 'system', <address\>)
* `path`: start introspecting from a specific object path
* `no-recurse`: only introspect the object at `path`

The `list` command can also be used to list interfaces from XML files.

```
$ dotnet dbus list interfaces /usr/share/dbus-1/interfaces/*.xml
```

## monitor

The `monitor` command registers the tool as a bus monitor and prints out the message bus messages.