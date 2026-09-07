LINK =

include $(top_srcdir)/build/build.mk

include $(top_srcdir)/build/xbuild.include

EXTRA_DIST += FSpot.dll.config
module_SCRIPTS += FSpot.dll.config

# Mono reads a dllmap from the file next to the assembly that declares the import, so
# lcmsNET's mapping has to be installed alongside lcmsNET.dll or colour management is
# dead on any box without an unversioned liblcms2 symlink.
EXTRA_DIST += lcmsNET.dll.config
module_SCRIPTS += lcmsNET.dll.config
