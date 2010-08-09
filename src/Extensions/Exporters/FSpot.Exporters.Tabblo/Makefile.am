ASSEMBLY = FSpot.Exporters.Tabblo
TARGET = library
LINK = $(REF_FSPOT_EXTENSION_TABBLOEXPORT)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	FSpot.Exporters.Tabblo/ApplicationCentricCertificatePolicy.cs \
	FSpot.Exporters.Tabblo/BlindTrustCertificatePolicy.cs \
	FSpot.Exporters.Tabblo/FSpotTraceListener.cs \
	FSpot.Exporters.Tabblo/FSpotUploadProgress.cs \
	FSpot.Exporters.Tabblo/TabbloExport.cs \
	FSpot.Exporters.Tabblo/TabbloExportModel.cs \
	FSpot.Exporters.Tabblo/TabbloExportView.cs \
	FSpot.Exporters.Tabblo/UserDecisionCertificatePolicy.cs

RESOURCES =  \
	Resources/TabbloExport.addin.xml \
	Resources/ui/TabbloExport.ui \
	Resources/ui/TrustError.ui

ASSEMBLY_INFO_SOURCE = AssemblyInfo.cs

include $(top_srcdir)/build/build.mk
