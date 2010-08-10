ASSEMBLY = FSpot.Tools.LiveWebGallery
TARGET = library
LINK = $(REF_FSPOT_EXTENSION_LIVEWEBGALLERY)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	FSpot.Tools.LiveWebGallery/GalleryRequestHandler.cs \
	FSpot.Tools.LiveWebGallery/LiveWebGallery.cs \
	FSpot.Tools.LiveWebGallery/LiveWebGalleryDialog.cs \
	FSpot.Tools.LiveWebGallery/PhotoRequestHandler.cs \
	FSpot.Tools.LiveWebGallery/ResourceRequestHandler.cs \
	FSpot.Tools.LiveWebGallery/SimpleWebServer.cs

RESOURCES =  \
	Resources/LiveWebGallery.addin.xml \
	Resources/ui/empty.png \
	Resources/ui/f-spot.css \
	Resources/ui/f-spot.js \
	Resources/ui/gallery.html \
	Resources/ui/LiveWebGallery.ui

include $(top_srcdir)/build/build.mk
