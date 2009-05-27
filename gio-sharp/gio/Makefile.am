include ../../Makefile.include

UNSAFE = -unsafe
DEBUG = -debug

SRCS =								\
	$(srcdir)/AssemblyInfo.cs				\
	$(srcdir)/FileFactory.cs				\
	$(srcdir)/GioStream.cs

CUSTOMS =							\
	$(srcdir)/AppInfoAdapter.custom				\
	$(srcdir)/FileAdapter.custom				\
	$(srcdir)/File.custom					\
	$(srcdir)/FileEnumerator.custom

SNK = 	$(srcdir)/gtk-sharp.snk

ASSEMBLIES =					\
	-pkg:glib-sharp-2.0

all: gio-sharp.dll

gio-api.xml: $(srcdir)/gio-api.raw $(srcdir)/Gio.metadata
	@echo -e "\n*** Massaging the raw api into $@"
	cp $(srcdir)/gio-api.raw gio-api.xml
	chmod +w gio-api.xml
	gapi2-fixup --api=gio-api.xml --metadata=$(srcdir)/Gio.metadata

$(top_builddir)/gio-sharp/generator/gapi_codegen.exe:
	$(MAKE) -C $(top_builddir)/gio-sharp/generator

generated/File.cs: gio-api.xml $(CUSTOMS) $(top_builddir)/gio-sharp/generator/gapi_codegen.exe
	@echo -e "\n*** Generating C# code"
	$(MONO) $(top_builddir)/gio-sharp/generator/gapi_codegen.exe $(GLIBSHARP_CFLAGS) --outdir=generated --customdir=$(srcdir) --generate gio-api.xml

gio-sharp.dll: generated/File.cs $(SRCS) $(SNK)
	@echo -e "\n*** Building $@"
	if ! test -e gtk-sharp.snk; then cp $(srcdir)/gtk-sharp.snk .; fi
	$(CSC) -target:library -out:$@ $(UNSAFE) $(DEBUG) $(ASSEMBLIES) $(SRCS) generated/*.cs

assemblydir = $(pkglibdir)
assembly_DATA =			\
	gio-sharp.dll		\
	gio-sharp.dll.config


CLEANFILES = 			\
	gio-sharp.dll 	\
	gio-sharp.dll.mdb \
	gio-api.xml

EXTRA_DIST =			\
	$(SRCS)			\
	$(CUSTOMS)		\
	Gio.metadata		\
	$(SNK)			\
	gio-sharp.dll.config	\
	gio-api.raw
