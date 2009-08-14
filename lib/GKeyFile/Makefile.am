include $(top_srcdir)/Makefile.include

ASSEMBLY_NAME = gkeyfile-sharp

ASSEMBLY_SOURCES =

ASSEMBLY_CUSTOMS =			\
	$(srcdir)/GKeyFile.custom

GENERATED =				\
	$(srcdir)/generated/Error.cs	\
	$(srcdir)/generated/Flags.cs	\
	$(srcdir)/generated/GKeyFile.cs
	
REFS =

PKGS =					\
	-pkg:glib-sharp-2.0

ASSEMBLY = $(ASSEMBLY_NAME).dll

all: $(ASSEMBLY)

gkeyfile-api.raw: gkeyfile-sharp-sources.xml
	$(GAPI2PARSER) gkeyfile-sharp-sources.xml

gkeyfile-api.xml: gkeyfile-api.raw GKeyFile.metadata
	cp gkeyfile-api.raw gkeyfile-api.xml
	$(GAPI2FIXUP) --api=gkeyfile-api.xml --metadata=GKeyFile.metadata

.generated_timestamp: gkeyfile-api.xml $(CUSTOMS)
	$(GAPI2CODEGEN) --outdir=generated --customdir=$(srcdir) --generate gkeyfile-api.xml
	touch $@
$(ASSEMBLY): .generated_timestamp $(ASSEMBLY_SOURCES) $(ASSEMBLY_CUSTOMS)
	$(CSC_LIB) -unsafe -out:$@ $(PKGS) $(REFS) $(ASSEMBLY_SOURCES) $(srcdir)/generated/*.cs

assemblydir = $(pkglibdir)
assembly_DATA =			\
	$(ASSEMBLY)

EXTRA_DIST =				\
	$(ASSEMBLY_SOURCES)		\
	$(ASSEMBLY_CUSTOMS)		\
	$(ASSEMBLY_NAME)-sources.xml	\
	gkeyfile-api.raw		\
	GKeyFile.metadata		\
	gkeyfile-api.xml		\
	.generated_timestamp		\
	$(GENERATED)

CLEANFILES =			\
	$(ASSEMBLY)		\
	$(ASSEMBLY).mdb
