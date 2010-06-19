include $(top_srcdir)/Makefile.include

CUSTOMS =			\
	$(srcdir)/GKeyFile.custom

ASSEMBLIES = 			\
	-pkg:glib-sharp-2.0

all: gkeyfile-sharp.dll

gkeyfile-api.raw: gkeyfile-sharp-sources.xml
	$(GAPI2PARSER) gkeyfile-sharp-sources.xml

gkeyfile-api.xml: gkeyfile-api.raw GKeyFile.metadata
	@echo -e "\n*** Massaging the raw api into $@"
	cp gkeyfile-api.raw gkeyfile-api.xml
	chmod +w gkeyfile-api.xml
	$(GAPI2FIXUP) --api=gkeyfile-api.xml --metadata=GKeyFile.metadata

generated/GKeyFile.cs: gkeyfile-api.xml $(CUSTOMS) $(GAPI2CODEGEN)
	@echo -e "\n*** Generating C# code"
	$(GAPI2CODEGEN) --outdir=generated --customdir=$(srcdir) --generate $(srcdir)/gkeyfile-api.xml

gkeyfile-sharp.dll: generated/GKeyFile.cs $(CUSTOMS)
	$(CSC_LIB) -unsafe -out:$@ $(PKGS) $(ASSEMBLIES) $(builddir)/generated/*.cs

assemblydir = $(pkglibdir)
assembly_DATA =			 	\
	gkeyfile-sharp.dll

CLEANFILES =				\
	gkeyfile-sharp.dll		\
	gkeyfile-sharp.dll.mdb	\
	gkeyfile-api.xml

DISTCLEANFILES =			\
	generated/*.cs

EXTRA_DIST =				\
	$(CUSTOMS)			\
	gkeyfile-api.raw		\
	GKeyFile.metadata		\
	gkeyfile-api.xml		\
	gkeyfile-sharp-sources.xml
