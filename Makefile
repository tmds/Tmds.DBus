all: NDesk.DBus.dll

BUS_SOURCES=Address.cs AssemblyInfo.cs Bus.cs BusObject.cs Connection.cs ExportObject.cs Authentication.cs Protocol.cs Mapper.cs MatchRule.cs Message.cs MessageFilter.cs MessageReader.cs MessageWriter.cs SocketTransport.cs Transport.cs TypeImplementer.cs Wrapper.cs
#IntrospectionSchema.cs TypeDefiner.cs
#UNIX_SOURCES=UnixTransport.cs UnixMonoTransport.cs
UNIX_SOURCES=UnixTransport.cs UnixNativeTransport.cs
CLR_SOURCES=DBus.cs Introspection.cs DProxy.cs Signature.cs

NDesk.DBus.dll: REFS=Mono.Posix

#do not build in the expected reply extension by default

#NDesk.DBus.dll: CSFLAGS=-d:PROTO_REPLY_SIGNATURE

NDesk.DBus.dll: CSFLAGS=-unsafe -d:STRONG_NAME

NDesk.DBus.dll: $(BUS_SOURCES) $(UNIX_SOURCES) $(CLR_SOURCES) ../ndesk.snk

NDesk.DBus.Portable.dll: CSFLAGS=-unsafe -d:PORTABLE

NDesk.DBus.Portable.dll: $(BUS_SOURCES) $(CLR_SOURCES)

.PHONY:
install: NDesk.DBus.dll
	$(GACUTIL) $(GACUTIL_FLAGS) -i NDesk.DBus.dll -f -package ndesk-dbus-1.0

include ../include.mk
