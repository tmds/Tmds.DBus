all: NDesk.DBus.dll monitor.exe

BUS_SOURCES=Address.cs AssemblyInfo.cs Connection.cs Authentication.cs Protocol.cs Message.cs MessageFilter.cs MessageReader.cs MessageWriter.cs Server.cs Transport.cs Wrapper.cs
#UNIX_SOURCES=UnixMonoTransport.cs
UNIX_SOURCES=UnixNativeTransport.cs
CLR_SOURCES=DBus.cs Introspection.cs DProxy.cs Signature.cs

NDesk.DBus.dll: REFS=Mono.Posix

NDesk.DBus.dll: CSFLAGS=-d:PROTO_REPLY_SIGNATURE -d:PROTO_TYPE_SINGLE

NDesk.DBus.dll: $(BUS_SOURCES) $(UNIX_SOURCES) $(CLR_SOURCES)

NDesk.DBus.Ssl.dll: REFS = Mono.Security

NDesk.DBus.Ssl.dll: NDesk.DBus.dll SslTransport.cs

monitor.exe: NDesk.DBus.dll Monitor.cs

introspect.exe: NDesk.DBus.dll Introspect.cs IntrospectionSchema.cs


include ../include.mk
