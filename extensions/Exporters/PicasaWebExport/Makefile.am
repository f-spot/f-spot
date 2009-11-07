include $(top_srcdir)/Makefile.include

PLUGIN_NAME = PicasaWebExport

PLUGIN_MANIFEST = $(PLUGIN_NAME).addin.xml

PLUGIN_ASSEMBLY = $(PLUGIN_NAME).dll

PLUGIN_SOURCES =			\
	$(srcdir)/PicasaWebExport.cs

REFS =						\
	-r:$(top_builddir)/src/f-spot.exe	\
	-r:$(top_builddir)/src/FSpot.Core.dll	\
	-r:$(top_builddir)/src/FSpot.Utils.dll	\
	$(LINK_SEMWEB)				\
	-r:google-sharp/Mono.Google.dll		\
	$(LINK_KEYRING)				\
	$(LINK_GIOSHARP)			\
	$(LINK_GPHOTO2)				\
	$(LINK_UNIQUESHARP)			\
	-r:System.Web				\
	-r:Mono.Posix

PKGS =					\
	$(LINK_GTKSHARPBEANS)		\
	-pkg:gtk-sharp-2.0		\
	-pkg:glade-sharp-2.0

SUBDIRS = 			\
	google-sharp

RESOURCES =				\
	-resource:$(srcdir)/$(PLUGIN_MANIFEST)	\
	-resource:$(srcdir)/$(PLUGIN_NAME).glade

mpack: $(PLUGIN_ASSEMBLY)
	mautil p $(PLUGIN_ASSEMBLY)

$(PLUGIN_ASSEMBLY): $(PLUGIN_SOURCES) $(PLUGIN_MANIFEST)
	$(CSC_LIB) -out:$@ $(PLUGIN_SOURCES) $(REFS) $(PKGS) $(ASSEMBLIES) $(RESOURCES)

plugindir = $(pkglibdir)/extensions

install-data-hook:
	if [ -w $(DESTDIR)$(plugindir)/$(PLUGIN_NAME).addin.xml ] ; then rm -f $(DESTDIR)$(plugindir)/$(PLUGIN_NAME).addin.xml; fi

plugin_DATA =			\
	$(PLUGIN_ASSEMBLY)

EXTRA_DIST = 			\
	$(PLUGIN_SOURCES)	\
	$(PLUGIN_MANIFEST)	\
	$(PLUGIN_NAME).glade

CLEANFILES =			\
	$(PLUGIN_ASSEMBLY)	\
	$(PLUGIN_ASSEMBLY).mdb	\
	*.mpack
