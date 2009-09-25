include $(top_srcdir)/Makefile.include

ASSEMBLY_NAME = libgphoto2-sharp

ASSEMBLY_SOURCES =			\
	$(srcdir)/AssemblyInfo.cs	\
	$(srcdir)/Camera.cs		\
	$(srcdir)/CameraAbilitiesList.cs\
	$(srcdir)/CameraFile.cs		\
	$(srcdir)/CameraFilesystem.cs	\
	$(srcdir)/CameraList.cs		\
	$(srcdir)/CameraWidget.cs	\
	$(srcdir)/Context.cs		\
	$(srcdir)/ErrorCodes.cs		\
	$(srcdir)/Object.cs		\
	$(srcdir)/Port.cs		\
	$(srcdir)/PortInfo.cs		\
	$(srcdir)/PortInfoList.cs

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
	$(ASSEMBLY).config		\
	AUTHORS				\
	COPYING

CLEANFILES =			\
	$(ASSEMBLY)		\
	$(ASSEMBLY).mdb
