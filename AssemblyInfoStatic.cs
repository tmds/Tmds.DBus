// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

// This AssemblyInfo file is used in builds that aren't driven by autoconf, eg. Visual Studio

using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyFileVersion("0.7.0")]
[assembly: AssemblyInformationalVersion("0.7.0")]
[assembly: AssemblyVersion("1.0")]
[assembly: AssemblyTitle ("dbus-sharp")]
[assembly: AssemblyDescription ("D-Bus IPC protocol library and CLR binding")]
[assembly: AssemblyCopyright ("Copyright (C) Alp Toker and others")]

#if STRONG_NAME
[assembly: InternalsVisibleTo ("dbus-sharp-tests, PublicKey=0024000004800000440000000602000000240000525341318001000011000000ffbfaa640454654de78297fde2d22dd4bc4b0476fa892c3f8575ad4f048ce0721ce4109f542936083bc4dd83be5f7f97")]
[assembly: InternalsVisibleTo ("dbus-monitor, PublicKey=0024000004800000440000000602000000240000525341318001000011000000ffbfaa640454654de78297fde2d22dd4bc4b0476fa892c3f8575ad4f048ce0721ce4109f542936083bc4dd83be5f7f97")]
[assembly: InternalsVisibleTo ("dbus-daemon, PublicKey=0024000004800000440000000602000000240000525341318001000011000000ffbfaa640454654de78297fde2d22dd4bc4b0476fa892c3f8575ad4f048ce0721ce4109f542936083bc4dd83be5f7f97")]
[assembly: InternalsVisibleTo ("dbus-sharp-glib, PublicKey=0024000004800000440000000602000000240000525341318001000011000000ffbfaa640454654de78297fde2d22dd4bc4b0476fa892c3f8575ad4f048ce0721ce4109f542936083bc4dd83be5f7f97")]
[assembly: InternalsVisibleTo ("dbus-sharp-proxies, PublicKey=0024000004800000440000000602000000240000525341318001000011000000ffbfaa640454654de78297fde2d22dd4bc4b0476fa892c3f8575ad4f048ce0721ce4109f542936083bc4dd83be5f7f97")]
#else
[assembly: InternalsVisibleTo ("dbus-sharp-tests")]
[assembly: InternalsVisibleTo ("dbus-monitor")]
[assembly: InternalsVisibleTo ("dbus-daemon")]
[assembly: InternalsVisibleTo ("dbus-sharp-glib")]
[assembly: InternalsVisibleTo ("dbus-sharp-proxies")]
#endif
