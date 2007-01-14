LIBRARY_DEST	= $(DESTDIR)/lib
BINDIR		= $(DESTDIR)/bin
ASSEMBLY_DLL	= $(LIBRARY).dll
ASSEMBLY_EXE	= $(PROGRAM).exe
ASSEMBLY_WINEXE	= $(PROGRAM_WIN).exe
ASSEMBLY_NAME	= $(LIBRARY)
SYMBOLS		= 
CSFLAGS		= /unsafe
SNKFILE = $(LIBRARY).snk

# If ASSEMBLY_DLL has been set, and thus, is not ".dll", assume we're a 
# library, and not an executable
TARGET	= library
ASSEMBLY	= $(ASSEMBLY_DLL)

GACUTIL_FLAGS = 
RUN_EXE = mono
