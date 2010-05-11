include $(top_srcdir)/Makefile.include

UNSAFE = -unsafe
DEBUG = -debug

SRCS =								\
	$(srcdir)/AssemblyInfo.cs				\
	$(srcdir)/MessageReceivedHandler.cs

CUSTOMS =							\
	$(srcdir)/App.custom					\
	$(srcdir)/MessageData.custom	

SNK = 	$(srcdir)/snk

ASSEMBLIES =					\
	-pkg:glib-sharp-2.0

all: unique-sharp.dll

unique-api.xml: $(srcdir)/unique-api.raw $(srcdir)/Unique.metadata
	@echo -e "\n*** Massaging the raw api into $@"
	cp $(srcdir)/unique-api.raw unique-api.xml
	chmod +w unique-api.xml
	$(GAPI2FIXUP) --api=unique-api.xml --metadata=$(srcdir)/Unique.metadata

$(top_builddir)/unique-sharp/generator/gapi_codegen.exe:
	$(MAKE) -C $(top_builddir)/lib/unique-sharp/generator

generated/App.cs: unique-api.xml $(CUSTOMS) $(builddir)/../generator/gapi_codegen.exe
	@echo -e "\n*** Generating C# code"
	$(MONO) $(top_builddir)/lib/unique-sharp/generator/gapi_codegen.exe $(GLIBSHARP_CFLAGS) --outdir=generated --customdir=$(srcdir) --generate unique-api.xml

unique-sharp.snk: $(SNK)
	cp $< $@

unique-sharp.dll: generated/App.cs $(SRCS) unique-sharp.snk
	@echo -e "\n*** Building $@"
	$(CSC) -target:library -out:$@ $(UNSAFE) $(DEBUG) $(ASSEMBLIES) $(SRCS) generated/*.cs

assemblydir = $(pkglibdir)
assembly_DATA =			\
	unique-sharp.dll	\
	unique-sharp.dll.config


CLEANFILES = 			\
	unique-sharp.dll 	\
	unique-sharp.dll.mdb \
	unique-api.xml

DISTCLEANFILES =		\
	generated/*.cs		\
	unique-sharp.snk

EXTRA_DIST =			\
	$(SRCS)			\
	$(CUSTOMS)		\
	Unique.metadata		\
	$(SNK)			\
	unique-sharp.dll.config	\
	unique-api.raw
