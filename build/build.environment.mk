# Initializers
MONO_BASE_PATH = 
MONO_ADDINS_PATH =

# Install Paths
DEFAULT_INSTALL_DIR = $(pkglibdir)
EXTENSIONS_INSTALL_DIR = $(DEFAULT_INSTALL_DIR)/Extensions

# External libraries to link against, generated from configure
LINK_SYSTEM = -r:System

DIR_BIN = $(top_builddir)/bin

# Cute hack to replace a space with something
colon:= :
empty:=
space:= $(empty) $(empty)

# Build path to allow running uninstalled
RUN_PATH = $(subst $(space),$(colon), $(MONO_BASE_PATH))

