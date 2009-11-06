addin = LiveWebGallery

addin_manifest = $(addin).addin.xml

addin_sources =				\
	LiveWebGallery.cs		\
	LiveWebGalleryDialog.cs		\
	SimpleWebServer.cs		\
	GalleryRequestHandler.cs	\
	PhotoRequestHandler.cs		\
	ResourceRequestHandler.cs

addin_references =			\
	Mono.Posix

addin_packages =			\
	gtk-sharp-2.0			\
	gnome-sharp-2.0

addin_resources =			\
	$(addin_manifest)		\
	ui/$(addin).ui			\
	ui/empty.png			\
	ui/f-spot.css			\
	ui/f-spot.js			\
	ui/gallery.html

addin_packages =			\
	gtk-sharp-2.0

addin_extradist =

include $(top_srcdir)/Makefile.addins
