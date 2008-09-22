include $(top_srcdir)/Makefile.include

PLUGIN_NAME = SmugMugExport

PLUGIN_MANIFEST = $(PLUGIN_NAME).addin.xml

PLUGIN_ASSEMBLY = $(PLUGIN_NAME).dll

PLUGIN_SOURCES =			\
	$(srcdir)/SmugMugExport.cs

REFS =					\
	-r:$(top_builddir)/src/f-spot.exe		\
	-r:$(top_builddir)/src/FSpot.Core.dll	\
	-r:$(top_builddir)/src/FSpot.Utils.dll	\
	$(LINK_SEMWEB)			\
	-r:SmugMugNet/SmugMugNet.dll	\
	$(LINK_KEYRING)			\
	-r:Mono.Posix

PKGS =					\
	-pkg:gtk-sharp-2.0		\
	-pkg:glade-sharp-2.0

RESOURCES =				\
	-resource:$(srcdir)/$(PLUGIN_MANIFEST)	\
	-resource:$(srcdir)/$(PLUGIN_NAME).glade

SUBDIRS =                      \
	SmugMugNet


all: $(PLUGIN_ASSEMBLY)

mpack: $(PLUGIN_ASSEMBLY)
	mautil p $(PLUGIN_ASSEMBLY)

$(PLUGIN_ASSEMBLY): $(PLUGIN_SOURCES) $(PLUGIN_MANIFEST)
	$(MAKE) -C $(SUBDIRS)
	$(CSC_LIB) -out:$@ $(PLUGIN_SOURCES) $(REFS) $(PKGS) $(ASSEMBLIES) $(RESOURCES)

plugindir = $(pkglibdir)/extensions

install-data-hook:
	rm -f $(plugindir)/$(PLUGIN_NAME).addin.xml

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
