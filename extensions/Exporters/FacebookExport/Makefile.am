include $(top_srcdir)/Makefile.include

PLUGIN_NAME = FacebookExport

PLUGIN_MANIFEST = $(PLUGIN_NAME).addin.xml

PLUGIN_ASSEMBLY = $(PLUGIN_NAME).dll

PLUGIN_SOURCES =			\
	$(srcdir)/FacebookExport.cs			\
	$(srcdir)/FacebookExportDialog.cs		\
	$(srcdir)/FacebookTagPopup.cs			\
	$(srcdir)/Mono.Facebook/Album.cs		\
	$(srcdir)/Mono.Facebook/AssemblyInfo.cs		\
	$(srcdir)/Mono.Facebook/Error.cs		\
	$(srcdir)/Mono.Facebook/Event.cs		\
	$(srcdir)/Mono.Facebook/FacebookException.cs	\
	$(srcdir)/Mono.Facebook/FacebookParam.cs	\
	$(srcdir)/Mono.Facebook/FacebookSession.cs	\
	$(srcdir)/Mono.Facebook/Friend.cs		\
	$(srcdir)/Mono.Facebook/FriendInfo.cs		\
	$(srcdir)/Mono.Facebook/Group.cs		\
	$(srcdir)/Mono.Facebook/Location.cs		\
	$(srcdir)/Mono.Facebook/Notification.cs		\
	$(srcdir)/Mono.Facebook/PeopleList.cs		\
	$(srcdir)/Mono.Facebook/Photo.cs		\
	$(srcdir)/Mono.Facebook/Responses.cs		\
	$(srcdir)/Mono.Facebook/SessionWrapper.cs	\
	$(srcdir)/Mono.Facebook/Tag.cs			\
	$(srcdir)/Mono.Facebook/User.cs			\
	$(srcdir)/Mono.Facebook/Util.cs

REFS =					\
	-r:$(top_builddir)/src/f-spot.exe		\
	-r:$(top_builddir)/src/FSpot.Core.dll	\
	-r:$(top_builddir)/src/FSpot.Utils.dll	\
	-r:$(top_builddir)/src/FSpot.Platform.dll	\
	-r:$(top_builddir)/src/FSpot.Widgets.dll	\
	$(LINK_GTKSHARPBEANS)				\
	$(LINK_KEYRING)                     \
	$(LINK_MONO_ADDINS)					\
	$(LINK_SEMWEB)						\
	$(LINK_GPHOTO2)						\
	$(LINK_GIOSHARP)						\
	-r:Mono.Posix

PKGS =					\
	-pkg:gtk-sharp-2.0

RESOURCES =				\
	-resource:$(srcdir)/$(PLUGIN_MANIFEST)	\
	-resource:$(srcdir)/$(PLUGIN_NAME).ui

all: $(PLUGIN_ASSEMBLY)

mpack: $(PLUGIN_ASSEMBLY)
	mautil p $(PLUGIN_ASSEMBLY)

$(PLUGIN_ASSEMBLY): $(PLUGIN_SOURCES) $(PLUGIN_MANIFEST) $(PLUGIN_NAME).ui
	$(CSC_LIB) -out:$@ -warnaserror $(PLUGIN_SOURCES) $(REFS) $(PKGS) $(ASSEMBLIES) $(RESOURCES)

plugindir = $(pkglibdir)/extensions

plugin_DATA =			\
	$(PLUGIN_ASSEMBLY)

EXTRA_DIST = 			\
	$(PLUGIN_SOURCES)	\
	$(PLUGIN_MANIFEST)	\
	$(PLUGIN_NAME).ui

CLEANFILES =			\
	$(PLUGIN_ASSEMBLY)	\
	$(PLUGIN_ASSEMBLY).mdb	\
	*.mpack
