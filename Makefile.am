ASSEMBLY = dbus-sharp
TARGET = $(ASSEMBLY).dll
XBUILD_Q = $(XBUILD) /nologo /verbosity:quiet

all: $(TARGET)

$(TARGET): $(CSFILES) AssemblyInfo.cs
	$(XBUILD_Q) dbus-sharp.csproj

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

.PHONY: $(TARGET)

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
