all: NDesk.DBus.dll

BUS_SOURCES=Address.cs AssemblyInfo.cs Bus.cs Connection.cs Authentication.cs Protocol.cs Mapper.cs Message.cs MessageFilter.cs MessageReader.cs MessageWriter.cs Server.cs Transport.cs Wrapper.cs
#UNIX_SOURCES=UnixMonoTransport.cs
UNIX_SOURCES=UnixNativeTransport.cs
CLR_SOURCES=DBus.cs Introspection.cs DProxy.cs Signature.cs

NDesk.DBus.dll: REFS=Mono.Posix

#do not build in the expected reply extension by default

#NDesk.DBus.dll: CSFLAGS=-d:PROTO_REPLY_SIGNATURE

NDesk.DBus.dll:

NDesk.DBus.dll: $(BUS_SOURCES) $(UNIX_SOURCES) $(CLR_SOURCES)


include ../include.mk
