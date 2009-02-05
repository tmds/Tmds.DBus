CSC=gmcs

SRCS=Daemon.cs Server.cs

dbus-daemon.exe: $(SRCS)
	$(CSC) -debug -t:exe -out:$@ -r:NDesk.DBus.dll -pkg:ndesk-dbus-glib-1.0 -pkg:glib-sharp-2.0 -keyfile:../ndesk.snk $(SRCS)

