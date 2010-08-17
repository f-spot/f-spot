ASSEMBLY = FSpot.Exporters.SmugMug
TARGET = library
LINK = $(REF_FSPOT_EXTENSION_SMUGMUGEXPORT)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	FSpot.Exporters.SmugMug/SmugMugAccount.cs \
	FSpot.Exporters.SmugMug/SmugMugAccountDialog.cs \
	FSpot.Exporters.SmugMug/SmugMugAccountManager.cs \
	FSpot.Exporters.SmugMug/SmugMugAddAlbum.cs \
	FSpot.Exporters.SmugMug/SmugMugExport.cs

RESOURCES =  \
	Resources/smugmug_add_album_dialog.ui \
	Resources/smugmug_add_dialog.ui \
	Resources/smugmug_export_dialog.ui \
	Resources/SmugMugExport.addin.xml

include $(top_srcdir)/build/build.mk
