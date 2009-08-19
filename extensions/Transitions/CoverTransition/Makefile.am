include $(top_srcdir)/Makefile.include

PLUGIN_NAME = CoverTransition

PLUGIN_MANIFEST = $(PLUGIN_NAME).addin.xml

PLUGIN_ASSEMBLY = $(PLUGIN_NAME).dll

PLUGIN_SOURCES =			\
	$(srcdir)/CoverTransition.cs


REFS =					\
	-r:$(top_builddir)/src/f-spot.exe	\
	-r:$(top_builddir)/src/FSpot.Core.dll	\
	-r:$(top_builddir)/src/FSpot.Utils.dll	\
	-r:$(top_builddir)/src/FSpot.Widgets.dll\
	-r:Mono.Cairo				\
	-pkg:gtk-sharp-2.0			\
	$(LINK_MONO_ADDINS)			\
	$(LINK_SEMWEB)				\
	$(LINK_GPHOTO2)				\
	$(LINK_GIOSHARP)			\
	$(LINK_DBUS)				\
	$(LINK_BEAGLE)

PKGS =

RESOURCES =				\
	-resource:$(srcdir)/$(PLUGIN_MANIFEST)

all: $(PLUGIN_ASSEMBLY)

mpack: $(PLUGIN_ASSEMBLY)
	mautil p $(PLUGIN_ASSEMBLY)

$(PLUGIN_ASSEMBLY): $(PLUGIN_SOURCES) $(PLUGIN_MANIFEST)
	$(CSC_LIB) -out:$@ $(CSC_DEFINES) $(PLUGIN_SOURCES) $(REFS) $(PKGS) $(ASSEMBLIES) $(RESOURCES)

plugindir = $(pkglibdir)/extensions

plugin_DATA =			\
	$(PLUGIN_ASSEMBLY)

EXTRA_DIST = 			\
	$(PLUGIN_SOURCES)	\
	$(PLUGIN_MANIFEST)

CLEANFILES =			\
	$(PLUGIN_ASSEMBLY)	\
	$(PLUGIN_ASSEMBLY).mdb	\
	*.mpack
