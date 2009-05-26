include ../../Makefile.include

UNSAFE = -unsafe
DEBUG = -debug

SRCS =						\
	AssemblyInfo.cs				\
	FileFactory.cs				\
	GioStream.cs

CUSTOMS =

ASSEMBLIES =					\
	-pkg:glib-sharp-2.0

all: gio-sharp.dll

gio-api.xml: $(srcdir)/gio-api.raw $(srcdir)/Gio.metadata
	@echo -e "\n*** Massaging the raw api into $@"
	cp $(srcdir)/gio-api.raw gio-api.xml
	chmod +w $(builddir)/gio-api.xml
	gapi2-fixup --api=gio-api.xml --metadata=$(srcdir)/Gio.metadata

$(top_builddir)/generator/gapi_codegen.exe:
	$(MAKE) -C $(top_builddir)/generator gapi_codegen.exe

generated/File.cs: gio-api.xml $(CUSTOMS) $(builddir)/../generator/gapi_codegen.exe
	@echo -e "\n*** Generating C# code"
	$(MONO) $(builddir)/../generator/gapi_codegen.exe $(GLIBSHARP_CFLAGS) --outdir=generated --customdir=$(srcdir) --generate gio-api.xml

gio-sharp.dll: $(srcdir)/generated/File.cs $(SRCS)
	@echo -e "\n*** Building $@"
	$(CSC) -target:library -out:$@ $(UNSAFE) $(DEBUG) $(ASSEMBLIES) $(SRCS) $(srcdir)/generated/*.cs

CLEANFILES = 			\
	gio-sharp.dll 	\
	gio-sharp.dll.mdb \
	gio-api.xml

EXTRA_DIST =			\
	$(SRCS)			\
	$(CUSTOMS)		\
	gio-api.raw
