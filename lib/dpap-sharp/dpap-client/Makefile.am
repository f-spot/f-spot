include $(top_srcdir)/Makefile.include

ASSEMBLY_NAME = dpap-client

ASSEMBLY_SOURCES = Main.cs

REFS =  \
	-r:../lib/dpap-sharp.dll \
	-r:Mono.Posix 

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
	

