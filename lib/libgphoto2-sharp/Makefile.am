SUBDIRS = glue

include $(top_srcdir)/Makefile.include

ASSEMBLY_NAME = libgphoto2-sharp

ASSEMBLY_SOURCES =				\
	$(srcdir)/AssemblyInfo.cs		\
	$(srcdir)/Camera.cs			\
	$(srcdir)/CameraAbilities.cs		\
	$(srcdir)/CameraAbilitiesList.cs	\
	$(srcdir)/CameraCaptureType.cs		\
	$(srcdir)/CameraDriverStatus.cs		\
	$(srcdir)/CameraEventType.cs		\
	$(srcdir)/CameraFile.cs			\
	$(srcdir)/CameraFilePath.cs		\
	$(srcdir)/CameraFilesystem.cs		\
	$(srcdir)/CameraFileOperation.cs	\
	$(srcdir)/CameraFolderOperation.cs	\
	$(srcdir)/CameraList.cs			\
	$(srcdir)/CameraOperation.cs		\
	$(srcdir)/CameraText.cs			\
	$(srcdir)/Context.cs			\
	$(srcdir)/ContextFeedback.cs		\
	$(srcdir)/DeviceType.cs			\
	$(srcdir)/ErrorCodes.cs			\
	$(srcdir)/GPList.cs			\
	$(srcdir)/GPObject.cs			\
	$(srcdir)/Port.cs			\
	$(srcdir)/PortInfo.cs			\
	$(srcdir)/PortInfoList.cs		\
	$(srcdir)/PortSerialParity.cs		\
	$(srcdir)/PortType.cs

REFS =	-r:Mono.Posix

PKGS =

ASSEMBLY = $(ASSEMBLY_NAME).dll

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(CSC_LIB) $(CSC_DEFINES) -unsafe -out:$@ $(PKGS) $(REFS) $(ASSEMBLY_SOURCES)

assemblydir = $(pkglibdir)
assembly_DATA =			\
	$(ASSEMBLY)		\
	$(ASSEMBLY).config

EXTRA_DIST =				\
	$(ASSEMBLY_SOURCES)		\
	$(ASSEMBLY).config.in		\
	AUTHORS				\
	COPYING

CLEANFILES =			\
	$(ASSEMBLY)		\
	$(ASSEMBLY).mdb		\
	$(ASSEMBLY).config
