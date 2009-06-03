include $(top_srcdir)/Makefile.include

ASSEMBLY_NAME = SmugMugNet

ASSEMBLY_SOURCES =				\
	$(srcdir)/SmugMugApi.cs 		\
	$(srcdir)/NoCheckCertificatePolicy.cs

REFS = \
	-r:$(top_builddir)/src/FSpot.Utils.dll	\
	-r:System.Web

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
