ASSEMBLY = FSpot.Exporters.TabbloExport
TARGET = library
LINK = $(REF_FSPOT_EXTENSION_TABBLOEXPORT)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES = \
	ApplicationCentricCertificatePolicy.cs \
	BlindTrustCertificatePolicy.cs \
	FSpotTraceListener.cs \
	FSpotUploadProgress.cs \
	TabbloExport.cs \
	TabbloExportModel.cs \
	TabbloExportView.cs \
	UserDecisionCertificatePolicy.cs

RESOURCES = \
	TabbloExport.addin.xml \
	ui/TabbloExport.ui \
	ui/TrustError.ui

ASSEMBLY_INFO_SOURCE = AssemblyInfo.cs

SUBDIRS = \
	Tabblo

include $(top_srcdir)/build/build.mk
