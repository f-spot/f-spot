ASSEMBLY = FSpot.Exporters.LiveWebGallery
TARGET = library
LINK = $(REF_FSPOT_EXTENSION_LIVEWEBGALLERY)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES = \
	GalleryRequestHandler.cs \
	LiveWebGallery.cs \
	LiveWebGalleryDialog.cs \
	PhotoRequestHandler.cs \
	ResourceRequestHandler.cs \
	SimpleWebServer.cs

RESOURCES = \
	LiveWebGallery.addin.xml \
	ui/LiveWebGallery.ui \
	ui/gallery.html \
	ui/f-spot.css \
	ui/f-spot.js

include $(top_srcdir)/build/build.mk
