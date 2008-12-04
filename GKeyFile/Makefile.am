all: gkeyfile-sharp.dll

gkeyfile-api.raw: gkeyfile-sharp-sources.xml
	gapi2-parser gkeyfile-sharp-sources.xml

gkeyfile-api.xml: gkeyfile-api.raw GKeyFile.metadata
	cp gkeyfile-api.raw gkeyfile-api.xml
	gapi2-fixup --api=gkeyfile-api.xml --metadata=GKeyFile.metadata

.generated_timestamp: gkeyfile-api.xml $(CUSTOMS)
	gapi2-codegen --outdir=generated --customdir=$(srcdir) --generate gkeyfile-api.xml
	touch $@

gkeyfile-sharp.dll: .generated_timestamp $(SOURCES) $(CUSTOMS)
	gmcs -unsafe -pkg:glib-sharp-2.0 -target:library -out:$@ $(srcdir)/generated/*.cs $(SOURCES)

CUSTOMS =				\
	$(srcdir)/GKeyFile.custom
	
EXTRA_DIST =				\
	$(CUSTOMS)			\
	gkeyfile-sharp-sources.xml	\
	gkeyfile-api.raw		\
	GKeyFile.metadata		\
	gkeyfile-api.xml		\
	.generated_timestamp		\
	generated/*.cs

CLEANFILES =				\
	gkeyfile-sharp.dll		\
	gkeyfile-sharp.dll.mdb

