ASSEMBLY = dbus-sharp
TARGET = $(ASSEMBLY).dll

CSFLAGS = \
	-debug -target:library -unsafe -d:STRONG_NAME -keyfile:$(top_srcdir)/dbus-sharp.snk

LIBFLAGS = \
	-r:Mono.Posix

CSFILES = \
	$(srcdir)/Address.cs \
	$(srcdir)/AddressEntry.cs \
	$(srcdir)/ArgDirection.cs \
	$(srcdir)/Authentication.cs \
	$(srcdir)/Bus.cs \
	$(srcdir)/BusException.cs \
	$(srcdir)/BusObject.cs \
	$(srcdir)/Connection.cs \
	$(srcdir)/DBus.cs \
	$(srcdir)/DProxy.cs \
	$(srcdir)/DType.cs \
	$(srcdir)/EndianFlag.cs \
	$(srcdir)/Error.cs \
	$(srcdir)/ExportObject.cs \
	$(srcdir)/FieldCode.cs \
	$(srcdir)/Header.cs \
	$(srcdir)/HeaderFlag.cs \
	$(srcdir)/Introspection.cs \
	$(srcdir)/ObjectPath.cs \
	$(srcdir)/ProtocolInformations.cs \
	$(srcdir)/Mapper.cs \
	$(srcdir)/MatchRule.cs \
	$(srcdir)/Message.cs \
	$(srcdir)/MessageFilter.cs \
	$(srcdir)/MessageReader.cs \
	$(srcdir)/MessageType.cs \
	$(srcdir)/MessageWriter.cs \
	$(srcdir)/MethodCall.cs \
	$(srcdir)/MethodReturn.cs \
	$(srcdir)/PendingCall.cs \
	$(srcdir)/Signal.cs \
	$(srcdir)/Signature.cs \
	$(srcdir)/SocketTransport.cs \
	$(srcdir)/Transport.cs \
	$(srcdir)/TypeImplementer.cs \
	$(srcdir)/TypeRental.cs \
	$(srcdir)/Unix.cs \
	$(srcdir)/UnixNativeTransport.cs \
	$(srcdir)/UnixTransport.cs \
	$(srcdir)/DValue.cs \
	$(srcdir)/MessageDumper.cs

all: $(TARGET)

$(TARGET): $(CSFILES) AssemblyInfo.cs
	$(GMCS) -out:$@ $(CSFLAGS) $(LIBFLAGS) $^

install-data-local: $(TARGET)
	@if test -n '$(TARGET)'; then                       \
          echo "$(GACUTIL) /i $(TARGET) /f /gacdir $(DESTDIR)$(libdir)";    \
          $(GACUTIL) /i $(TARGET) /package $(PACKAGE)-$(API_VERSION) /f /gacdir $(libdir) /root $(DESTDIR)$(libdir) || exit 1; \
        fi

uninstall-local:
	@if test -n '$(TARGET)'; then                       \
          echo "$(GACUTIL) /u $(ASSEMBLY) /gacdir $(DESTDIR)$(libdir)";  \
          $(GACUTIL) /u $(ASSEMBLY) /package $(PACKAGE)-$(API_VERSION) /gacdir $(libdir) /root $(DESTDIR)$(libdir) || exit 1;   \
        fi

EXTRA_DIST = \
	$(CSFILES) \
	$(srcdir)/dbus-sharp.dll.config \
	$(srcdir)/AssemblyInfo.cs.in

CLEANFILES = \
	$(TARGET) \
	$(TARGET).mdb

DISTCLEANFILES = \
	AssemblyInfo.cs

MAINTAINERCLEANFILES = \
	Makefile.in
