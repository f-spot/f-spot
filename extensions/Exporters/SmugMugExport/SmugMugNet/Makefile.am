include $(top_srcdir)/Makefile.include

ASSEMBLY_NAME = SmugMugNet

ASSEMBLY_SOURCES =				\
	$(srcdir)/SmugMugApi.cs 		\
	$(srcdir)/NoCheckCertificatePolicy.cs

REFS =

PKGS =

ASSEMBLY = $(ASSEMBLY_NAME).dll

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(CSC_LIB) -out:$@ $(PKGS) $(REFS) $(ASSEMBLY_SOURCES)

assemblydir = $(pkglibdir)
assembly_DATA =	$(ASSEMBLY)

EXTRA_DIST = $(ASSEMBLY_SOURCES)

CLEANFILES =			\
	$(ASSEMBLY)		\
	$(ASSEMBLY).mdb
