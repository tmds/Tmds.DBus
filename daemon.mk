CSC=gmcs

SRCS=Daemon.cs Server.cs ServerBus.cs

ifdef USE_GLIB
GLIB_FLAGS=-d:USE_GLIB -pkg:dbus-sharp-glib-1.0 -pkg:glib-sharp-2.0
endif

dbus-daemon.exe: $(SRCS)
	$(CSC) -debug -t:exe -out:$@ -r:dbus-sharp.dll $(GLIB_FLAGS) -keyfile:../ndesk.snk $(SRCS)

