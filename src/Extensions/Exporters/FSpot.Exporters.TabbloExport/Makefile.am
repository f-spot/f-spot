ASSEMBLY = FSpot.Exporters.TabbloExport
TARGET = library
LINK = $(REF_FSPOT_EXTENSION_TABBLOEXPORT)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	FSpot.Exporters.TabbloExport/ApplicationCentricCertificatePolicy.cs \
	FSpot.Exporters.TabbloExport/BlindTrustCertificatePolicy.cs \
	FSpot.Exporters.TabbloExport/FSpotTraceListener.cs \
	FSpot.Exporters.TabbloExport/FSpotUploadProgress.cs \
	FSpot.Exporters.TabbloExport/TabbloExport.cs \
	FSpot.Exporters.TabbloExport/TabbloExportModel.cs \
	FSpot.Exporters.TabbloExport/TabbloExportView.cs \
	FSpot.Exporters.TabbloExport/UserDecisionCertificatePolicy.cs

RESOURCES =  \
	Resources/TabbloExport.addin.xml \
	Resources/ui/TabbloExport.ui \
	Resources/ui/TrustError.ui

ASSEMBLY_INFO_SOURCE = AssemblyInfo.cs

include $(top_srcdir)/build/build.mk
