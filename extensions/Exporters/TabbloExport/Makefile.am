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

PLUGIN_INPUT = \
	$(PLUGIN_SOURCES) \
	$(PLUGIN_MANIFEST) \
	ui/$(PLUGIN_NAME).ui \
	ui/TrustError.ui


REFS = \
	-r:$(top_builddir)/src/f-spot.exe \
	-r:$(top_builddir)/src/FSpot.Core.dll \
	-r:$(top_builddir)/src/FSpot.Utils.dll \
	-r:Tabblo/Mono.Tabblo.dll \
	$(LINK_KEYRING)		\
	$(LINK_SEMWEB)		\
	$(LINK_GIOSHARP)	\
	$(LINK_GTKSHARPBEANS)	\	
	$(LINK_GPHOTO2)		\
	$(LINK_UNIQUESHARP)	\
	-r:Mono.Posix.dll

PKGS = \
	-pkg:gtk-sharp-2.0 

SUBDIRS = \
	Tabblo

RESOURCES = \
	-resource:$(srcdir)/$(PLUGIN_MANIFEST) \
	-resource:$(srcdir)/ui/$(PLUGIN_NAME).ui \
	-resource:$(srcdir)/ui/TrustError.ui

mpack: $(PLUGIN_ASSEMBLY)
	mautil p $(PLUGIN_ASSEMBLY)

$(PLUGIN_ASSEMBLY): $(PLUGIN_INPUT)
	$(CSC_LIB) -out:$@ $(PLUGIN_SOURCES) $(REFS) $(PKGS) $(ASSEMBLIES) $(RESOURCES)

plugindir = $(pkglibdir)/extensions

install-data-hook:
	if [ -w $(DESTDIR)$(plugindir)/$(PLUGIN_NAME).addin.xml ] ; then rm -f $(DESTDIR)$(plugindir)/$(PLUGIN_NAME).addin.xml; fi

plugin_DATA =			\
	$(PLUGIN_ASSEMBLY)

EXTRA_DIST = $(PLUGIN_INPUT)

CLEANFILES =			\
	$(PLUGIN_ASSEMBLY)	\
	$(PLUGIN_ASSEMBLY).mdb	\
	*.mpack
