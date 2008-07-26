include $(top_srcdir)/Makefile.include

ASSEMBLY_NAME = dpap-client

ASSEMBLY_SOURCES =  \
	AssemblyInfo.cs \
	Main.cs

REFS =  \
	-pkg:banshee-1-core \
	-pkg:banshee-1-services \
	-r:../lib/dpap-sharp.dll 

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
	

