addin = DBusService

addin_manifest = $(addin).addin.xml

addin_sources =			\
	$(addin).cs		\
	DBusProxy.cs

addin_resources =		\
	$(addin_manifest)

addin_references =
addin_packages =		\
	gtk-sharp-2.0

addin_extraflags =		\
	$(NDESK_DBUS_LIBS)
addin_extradist =

include $(top_srcdir)/Makefile.addins
