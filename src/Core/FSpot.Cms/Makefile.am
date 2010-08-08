ASSEMBLY = FSpot.Cms
ASSEMBLY_BUILD_FLAGS = -unsafe
TARGET = library
LINK = $(REF_FSPOT_CMS)

SOURCES =  \
	Cms/CctTable.cs \
	Cms/CmsException.cs \
	Cms/ColorCIELab.cs \
	Cms/ColorCIELCh.cs \
	Cms/ColorCIExyY.cs \
	Cms/ColorCIEXYZ.cs \
	Cms/Format.cs \
	Cms/GammaTable.cs \
	Cms/IccColorSpace.cs \
	Cms/IccProfileClass.cs \
	Cms/Intent.cs \
	Cms/NativeMethods.cs \
	Cms/Profile.cs \
	Cms/SaveException.cs \
	Cms/Transform.cs

RESOURCES =

include $(top_srcdir)/build/build.mk

EXTRA_DIST += FSpot.Cms.dll.config
module_SCRIPTS += FSpot.Cms.dll.config
