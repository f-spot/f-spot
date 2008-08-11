include $(top_srcdir)/Makefile.include

ASSEMBLY_NAME = dpap-server

ASSEMBLY_SOURCES =  \
	AssemblyInfo.cs \
	Main.cs

REFS =					\
	-r:Mono.Posix			\
	-r:ICSharpCode.SharpZipLib 	\
	-r:../lib/dpap-sharp.dll

PKGS = 

ASSEMBLY = $(ASSEMBLY_NAME).exe

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(CSC) -out:$@ $(PKGS) $(REFS) $(ASSEMBLY_SOURCES)

assemblydir = $(pkglibdir)
assembly_DATA =			\
	$(ASSEMBLY)

EXTRA_DIST =				\
	$(ASSEMBLY_SOURCES)

CLEANFILES =			\
	$(ASSEMBLY)		\
	$(ASSEMBLY).mdb
	

