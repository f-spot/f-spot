include $(top_srcdir)/Makefile.include

PLUGIN_NAME = TabbloExport

PLUGIN_MANIFEST = $(PLUGIN_NAME).addin.xml

PLUGIN_ASSEMBLY = $(PLUGIN_NAME).dll

PLUGIN_SOURCES = \
	$(srcdir)/ApplicationCentricCertificatePolicy.cs \
	$(srcdir)/AssemblyInfo.cs \
	$(srcdir)/BlindTrustCertificatePolicy.cs \
	$(srcdir)/FSpotTraceListener.cs \
	$(srcdir)/FSpotUploadProgress.cs \
	$(srcdir)/TabbloExport.cs \
	$(srcdir)/TabbloExportModel.cs \
	$(srcdir)/TabbloExportView.cs \
	$(srcdir)/UserDecisionCertificatePolicy.cs


REFS = \
	-r:$(top_builddir)/src/f-spot.exe \
	-r:$(top_builddir)/src/FSpot.Core.dll \
	-r:$(top_builddir)/src/FSpot.Utils.dll \
	-r:Tabblo/Mono.Tabblo.dll \
	$(LINK_KEYRING)		\
	$(LINK_MONO_ADDINS)					\
	$(LINK_SEMWEB)						\
	$(LINK_GIOSHARP)						\
	$(LINK_GTKSHARPBEANS)		\	
	$(LINK_GPHOTO2)						\
	-r:Mono.Posix.dll

PKGS = \
	-pkg:gtk-sharp-2.0 \
	-pkg:glade-sharp-2.0 \
	-pkg:gnome-vfs-sharp-2.0

SUBDIRS = \
	Tabblo

RESOURCES = \
	-resource:$(srcdir)/$(PLUGIN_MANIFEST) \
	-resource:$(srcdir)/$(PLUGIN_NAME).glade \
	-resource:$(srcdir)/TrustError.glade

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
	TrustError.glade	\
	$(PLUGIN_NAME).glade

CLEANFILES =			\
	$(PLUGIN_ASSEMBLY)	\
	$(PLUGIN_ASSEMBLY).mdb	\
	*.mpack
