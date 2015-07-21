ASSEMBLY = dbus-sharp
TARGET = $(ASSEMBLY).dll
XBUILD_Q = $(XBUILD) /nologo /verbosity:quiet

# Use the gencssources script to generate this list
CSFILES = Address.cs \
	Authentication.cs \
	Bus.cs \
	BusObject.cs \
	Connection.cs \
	DBus.cs \
	DProxy.cs \
	ExportObject.cs \
	Introspection.cs \
	Mapper.cs \
	TypeImplementer.cs \
	ArgDirection.cs \
	ObjectPath.cs \
	OSHelpers.cs \
	BusException.cs \
	AddressEntry.cs \
	Protocol/DBusStruct.cs \
	Protocol/DType.cs \
	Protocol/DValue.cs \
	Protocol/EndianFlag.cs \
	Protocol/FieldCode.cs \
	Protocol/Header.cs \
	Protocol/HeaderFlag.cs \
	Protocol/MatchRule.cs \
	Protocol/Message.cs \
	Protocol/MessageContainer.cs \
	Protocol/MessageDumper.cs \
	Protocol/MessageFilter.cs \
	Protocol/MessageReader.cs \
	Protocol/MessageType.cs \
	Protocol/MessageWriter.cs \
	Protocol/PendingCall.cs \
	Protocol/ProtocolInformation.cs \
	Protocol/Signature.cs \
	Protocol/SocketTransport.cs \
	Protocol/Transport.cs \
	Transports/UnixNativeTransport.cs \
	Transports/UnixTransport.cs \
	Unix/UnixStream.cs \
	Unix/UnixError.cs \
	Unix/UnixSocket.cs \
	AssemblyInfo.cs

all: $(TARGET)

$(TARGET): $(CSFILES) AssemblyInfo.cs
	$(XBUILD_Q) dbus-sharp.csproj

install-data-local: $(TARGET)
	@if test -n '$(TARGET)'; then                       \
          echo "$(GACUTIL) /i $(TARGET) /package $(PACKAGE)-$(API_VERSION) /f /gacdir $(libdir) /root $(DESTDIR)$(libdir)";    \
          $(GACUTIL) /i $(TARGET) /package $(PACKAGE)-$(API_VERSION) /f /gacdir $(libdir) /root $(DESTDIR)$(libdir) || exit 1; \
        fi

uninstall-local:
	@if test -n '$(TARGET)'; then                       \
          echo "$(GACUTIL) /u $(ASSEMBLY) /package $(PACKAGE)-$(API_VERSION) /gacdir $(libdir) /root $(DESTDIR)$(libdir)";  \
          $(GACUTIL) /u $(ASSEMBLY) /package $(PACKAGE)-$(API_VERSION) /gacdir $(libdir) /root $(DESTDIR)$(libdir) || exit 1;   \
        fi

.PHONY: $(TARGET)

EXTRA_DIST = \
	$(CSFILES) \
	$(srcdir)/dbus-sharp.dll.config \
	$(srcdir)/AssemblyInfo.cs.in \
	$(srcdir)/dbus-sharp.csproj

CLEANFILES = \
	$(TARGET) \
	$(TARGET).mdb

DISTCLEANFILES = \
	AssemblyInfo.cs

MAINTAINERCLEANFILES = \
	Makefile.in
