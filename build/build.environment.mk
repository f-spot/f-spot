# Initializers
MONO_BASE_PATH =

# Install Paths
DEFAULT_INSTALL_DIR = $(pkglibdir)
BACKENDS_INSTALL_DIR = $(DEFAULT_INSTALL_DIR)/Backends
EXTENSIONS_INSTALL_DIR = $(DEFAULT_INSTALL_DIR)/Extensions


## Directories
DIR_DOCS = $(top_builddir)/docs
DIR_EXTENSIONS = $(top_builddir)/extensions
DIR_ICONS = $(top_builddir)/icons
DIR_LIBFSPOT = $(top_builddir)/lib/libfspot
DIR_SRC = $(top_builddir)/src
DIR_GTKSHARPBEANS = $(top_builddir)/lib/gtk-sharp-beans
DIR_BIN = $(top_builddir)/bin


# External libraries to link against, generated from configure
LINK_SYSTEM = -r:System
LINK_SYSTEMDATA = -r:System.Data
LINK_SYSTEM_WEB = -r:System.Web
LINK_MONO_CAIRO = -r:Mono.Cairo
LINK_ICSHARP_ZIP_LIB = -r:ICSharpCode.SharpZipLib

LINK_GLIB = $(GLIBSHARP_LIBS)
LINK_GTK = $(GTKSHARP_LIBS)
LINK_FLICKRNET = -pkg:flickrnet

# Gtk Beans
LINK_GTK_BEANS = -r:$(DIR_GTKSHARPBEANS)/gtk-sharp-beans.dll
LINK_GTK_BEANS_DEPS = $(REF_GTK_BEANS) $(LINK_GTK_BEANS)

# Hyena
REF_HYENA = $(LINK_SYSTEM)
LINK_HYENA = -r:$(DIR_BIN)/Hyena.dll
LINK_HYENA_DEPS = $(REF_HYENA) $(LINK_HYENA)

# TagLib
REF_TAGLIB =
LINK_TAGLIB = $(TAGLIB_SHARP_LIBS)
LINK_TAGLIB_DEPS = $(REF_TAGLIB) $(LINK_TAGLIB)

# Hyena.Gui
REF_HYENA_GUI = $(LINK_HYENA_DEPS)
LINK_HYENA_GUI = -r:$(DIR_BIN)/Hyena.Gui.dll
LINK_HYENA_GUI_DEPS = $(REF_HYENA_GUI) $(LINK_HYENA_GUI)

# FSpot.Core
REF_FSPOT_CORE = $(LINK_HYENA_DEPS)
LINK_FSPOT_CORE = -r:$(DIR_BIN)/FSpot.Core.dll

# FSpot (executable)
REF_FSPOT_GTK = $(LINK_FSPOT_CORE_DEPS) \
	$(LINK_GLIB) \
	$(LINK_MONODATA) \
	$(LINK_ICSHARP_ZIP_LIB) \
	$(LINK_HYENA_GUI_DEPS) $(LINK_TAGLIB)

# FIXME: do not link executables
LINK_FSPOT_GTK = -r:$(DIR_BIN)/f-spot.exe
LINK_FSPOT_GTK_DEPS = $(REF_FSPOT_CORE)

# Extensions
REF_FSPOT_EXTENSION_BLACKOUTEDITOR = $(LINK_FSPOT_GTK_DEPS)
REF_FSPOT_EXTENSION_BWEDITOR = $(LINK_FSPOT_GTK_DEPS)
REF_FSPOT_EXTENSION_FLIPEDITOR = $(LINK_FSPOT_GTK_DEPS)
REF_FSPOT_EXTENSION_PIXELATEEDITOR = $(LINK_FSPOT_GTK_DEPS)
REF_FSPOT_EXTENSION_RESIZEEDITOR = $(LINK_FSPOT_GTK_DEPS)
REF_FSPOT_EXTENSION_CDEXPORT = $(LINK_FSPOT_GTK_DEPS)
REF_FSPOT_EXTENSION_FLICKREXPORT = $(LINK_FSPOT_GTK_DEPS) $(LINK_FLICKRNET)
REF_FSPOT_EXTENSION_FOLDEREXPORT = $(LINK_FSPOT_GTK_DEPS) $(LINK_SYSTEM_WEB)
REF_FSPOT_EXTENSION_GALLERYEXPORT = $(LINK_FSPOT_GTK_DEPS)

REF_FSPOT_EXTENSION_ZIPEXPORT = $(LINK_FSPOT_GTK_DEPS)
REF_FSPOT_EXTENSION_CHANGEPHOTOPATH = $(LINK_FSPOT_GTK_DEPS)
REF_FSPOT_EXTENSION_DEVELOPINUFRAW = $(LINK_FSPOT_GTK_DEPS)
REF_FSPOT_EXTENSION_LIVEWEBGALLERY = $(LINK_FSPOT_GTK_DEPS)
REF_FSPOT_EXTENSION_MERGEDB = $(LINK_FSPOT_GTK_DEPS)
REF_FSPOT_EXTENSION_RAWPLUSJPEG = $(LINK_FSPOT_GTK_DEPS)
REF_FSPOT_EXTENSION_RETROACTIVEROLL = $(LINK_FSPOT_GTK_DEPS)
REF_FSPOT_EXTENSION_COVERTRANSITION = $(LINK_FSPOT_GTK_DEPS)




# Cute hack to replace a space with something
colon:= :
empty:=
space:= $(empty) $(empty)

# Build path to allow running uninstalled
RUN_PATH = $(subst $(space),$(colon), $(MONO_BASE_PATH))
