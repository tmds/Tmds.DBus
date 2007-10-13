ASSEMBLY = NDesk.DBus
TARGET = $(ASSEMBLY).dll

CSFLAGS = \
	-debug -target:library -unsafe -d:STRONG_NAME -keyfile:../ndesk.snk

LIBFLAGS = \
	-r:Mono.Posix

CSFILES = \
	$(srcdir)/Address.cs \
	$(srcdir)/Authentication.cs \
	$(srcdir)/Bus.cs \
	$(srcdir)/BusObject.cs \
	$(srcdir)/Connection.cs \
	$(srcdir)/DBus.cs \
	$(srcdir)/DProxy.cs \
	$(srcdir)/ExportObject.cs \
	$(srcdir)/Introspection.cs \
	$(srcdir)/Protocol.cs \
	$(srcdir)/Mapper.cs \
	$(srcdir)/MatchRule.cs \
	$(srcdir)/Message.cs \
	$(srcdir)/MessageFilter.cs \
	$(srcdir)/MessageReader.cs \
	$(srcdir)/MessageWriter.cs \
	$(srcdir)/PendingCall.cs \
	$(srcdir)/Signature.cs \
	$(srcdir)/SocketTransport.cs \
	$(srcdir)/Transport.cs \
	$(srcdir)/TypeImplementer.cs \
	$(srcdir)/UnixNativeTransport.cs \
	$(srcdir)/UnixTransport.cs \
	$(srcdir)/Wrapper.cs

all: $(TARGET)

$(TARGET): $(CSFILES) AssemblyInfo.cs
	$(GMCS) -out:$@ $(CSFLAGS) $(LIBFLAGS) $^

install-data-local:
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
	$(CSFILES)
	$(srcdir)/AssemblyInfo.cs.in

CLEANFILES = \
	$(TARGET) \
	$(TARGET).mdb

DISTCLEANFILES = \
	AssemblyInfo.cs

MAINTAINERCLEANFILES = \
	Makefile.in
