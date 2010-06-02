ASSEMBLY = FSpot.Cms
ASSEMBLY_BUILD_FLAGS = -unsafe
TARGET = library
LINK = $(REF_FSPOT_CMS)

SOURCES = \
	CctTable.cs \
	CmsException.cs \
	ColorCIELab.cs \
	ColorCIELCh.cs \
	ColorCIExyY.cs \
	ColorCIEXYZ.cs \
	Format.cs \
	GammaTable.cs \
	IccColorSpace.cs \
	IccProfileClass.cs \
	Intent.cs \
	NativeMethods.cs \
	Profile.cs \
	SaveException.cs \
	Transform.cs

RESOURCES =

include $(top_srcdir)/build/build.mk

EXTRA_DIST += FSpot.Cms.dll.config
module_SCRIPTS += FSpot.Cms.dll.config
