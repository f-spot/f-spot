include $(top_srcdir)/Makefile.include

PLUGIN_NAME = MergeDb

PLUGIN_MANIFEST = $(PLUGIN_NAME).addin.xml

PLUGIN_ASSEMBLY = $(PLUGIN_NAME).dll

PLUGIN_SOURCES =			\
	$(srcdir)/MergeDb.cs		\
	$(srcdir)/MergeDbDialog.cs	\
	$(srcdir)/PickFolderDialog.cs

REFS =					\
	-r:Mono.Posix			\
	-r:$(top_builddir)/src/f-spot.exe		\
	-r:$(top_builddir)/src/FSpot.Core.dll	\
	-r:$(top_builddir)/src/FSpot.Utils.dll	\
	-r:$(top_builddir)/src/FSpot.Query.dll

PKGS =					\
	-pkg:gtk-sharp-2.0		\
	-pkg:glade-sharp-2.0

RESOURCES =				\
	-resource:$(srcdir)/$(PLUGIN_MANIFEST)	\
	-resource:$(srcdir)/$(PLUGIN_NAME).glade

all: $(PLUGIN_ASSEMBLY)

mpack: $(PLUGIN_ASSEMBLY)
	mautil p $(PLUGIN_ASSEMBLY)

$(PLUGIN_ASSEMBLY): $(PLUGIN_SOURCES) $(PLUGIN_MANIFEST) $(PLUGIN_NAME).glade
	$(CSC_LIB) -out:$@ $(PLUGIN_SOURCES) $(REFS) $(PKGS) $(ASSEMBLIES) $(RESOURCES)

plugindir = $(pkglibdir)/extensions

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
