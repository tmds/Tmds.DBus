[![NuGet](https://img.shields.io/nuget/v/Tmds.DBus.Protocol.svg)](https://www.nuget.org/packages/Tmds.DBus.Protocol)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Tmds.DBus.Protocol)](https://www.nuget.org/packages/Tmds.DBus.Protocol)
[![GitHub](https://img.shields.io/badge/GitHub-tmds%2FTmds.DBus-blue?logo=github)](https://github.com/tmds/Tmds.DBus)
[![License](https://img.shields.io/github/license/tmds/Tmds.DBus)](https://github.com/tmds/Tmds.DBus/blob/main/COPYING)
![.NET](https://img.shields.io/badge/.NET-Standard%202.0%20%7C%206.0%2B-512BD4)

From https://www.freedesktop.org/wiki/Software/dbus/:

> D-Bus is a message bus system, a simple way for applications to talk to one another. In addition to interprocess
communication, D-Bus helps coordinate process lifecycle; it makes it simple and reliable to code a "single instance"
application or daemon, and to launch applications and daemons on demand when their services are needed.

Tmds.DBus provides .NET libraries for working with D-Bus from .NET.

## Tmds.DBus.Protocol

[Tmds.DBus.Protocol](api/Tmds.DBus.Protocol/Tmds.DBus.Protocol.yml) is a modern, high-performance library that uses types introduced in .NET Core 2.1 (like `Span<T>`) for low-allocation, high-performance protocol implementation.

- Targets .NET Standard 2.0, 2.1, .NET 6.0, 8.0, and 9.0
- Compatible with NativeAOT/Trimming (use .NET 8 or higher)
- High-performance with minimal allocations
- Modern .NET primitives

The following code generators are available for `Tmds.DBus.Protocol`:
- [Tmds.DBus.Tool](tool.md): CLI tool that supports generating proxy only.
- [Tmds.DBus.Generator](generator.md): Roslyn source Generator that supports generating proxy only.
- [affederaffe/Tmds.DBus.SourceGenerator](https://github.com/affederaffe/Tmds.DBus.SourceGenerator): Roslyn source Generator that supports generating proxy and handler types.

## Tmds.DBus

[Tmds.DBus](api/Tmds.DBus/Tmds.DBus.yml) is based on [dbus-sharp](https://github.com/mono/dbus-sharp) (a fork of [ndesk-dbus](http://www.ndesk.org/DBusSharp)). It builds on the protocol implementation of dbus-sharp and provides an API based on the asynchronous programming model introduced in .NET 4.5.

- Targets .NET Standard 2.0 and .NET 6.0
- Runs on .NET Framework 4.6.1+, .NET Core, and .NET 6+
- Async/await support

The following code generators are available for `Tmds.DBus`:
- [Tmds.DBus.Tool](tool.md): CLI tool that supports generating proxy and handler types.

## Contributing

Found a bug or want to request a feature? Please [open an issue on GitHub](https://github.com/tmds/Tmds.DBus/issues).

We welcome pull requests on [GitHub](https://github.com/tmds/Tmds.DBus)! Unless you're making a trivial change, open an issue to discuss the change before making a pull request.
