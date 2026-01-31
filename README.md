[![NuGet](https://img.shields.io/nuget/v/Tmds.DBus.svg)](https://www.nuget.org/packages/Tmds.DBus)

# CI Packages

CI NuGet packages are built from the `main` branch and pushed to the https://www.myget.org/F/tmds/api/v3/index feed.

NuGet.Config:
```
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="tmds" value="https://www.myget.org/F/tmds/api/v3/index.json" />
  </packageSources>
</configuration>
```

To add a package using `dotnet`:

```
dotnet add package --prerelease Tmds.DBus.Protocol
```

This will add the package to your `csproj` file and use the latest available version.

You can change the package `Version` in the `csproj` file to `*-*`. Then it will restore newer versions when they become available on the CI feed.

# Further Reading

* [D-Bus](docs/dbus.md): Short overview of D-Bus.
* [Tmds.DBus Modelling](docs/modelling.md): Describes how to model D-Bus types in C# for use with Tmds.DBus.
* [Tmds.DBus.Tool](docs/tool.md): Documentation of dotnet dbus tool.
* [How-to](docs/howto.md): Documents some (advanced) use-cases.
* [Tmds.DBus.Protocol](docs/protocol.md): Documentation of the protocol library.
