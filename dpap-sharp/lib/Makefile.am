include $(top_srcdir)/Makefile.include

ASSEMBLY_NAME = dpap-sharp

ASSEMBLY_SOURCES =				\
	$(srcdir)/AssemblyInfo.cs		\
	$(srcdir)/Album.cs			\
	$(srcdir)/AuthenticationException.cs	\
	$(srcdir)/BrokenMD5.cs			\
	$(srcdir)/Client.cs			\
	$(srcdir)/ContentCodeBag.cs		\
	$(srcdir)/ContentFetcher.cs		\
	$(srcdir)/ContentParser.cs		\
	$(srcdir)/ContentWriter.cs		\
	$(srcdir)/Database.cs			\
	$(srcdir)/Discovery.cs			\
	$(srcdir)/Hasher.cs			\
	$(srcdir)/LoginException.cs		\
	$(srcdir)/Photo.cs			\
	$(srcdir)/Server.cs			\
	$(srcdir)/ServerInfo.cs			\
	$(srcdir)/User.cs			\
	$(srcdir)/Utility.cs

REFS =					\
	-r:ICSharpCode.SharpZipLib \
	-resource:content-codes

PKGS =					\
	-pkg:mono-zeroconf

ASSEMBLY = $(ASSEMBLY_NAME).dll

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(CSC_LIB) -out:$@ $(PKGS) $(REFS) $(ASSEMBLY_SOURCES)

assemblydir = $(pkglibdir)
assembly_DATA =			\
	$(ASSEMBLY)

EXTRA_DIST =				\
	$(ASSEMBLY_SOURCES)

CLEANFILES =			\
	$(ASSEMBLY)		\
	$(ASSEMBLY).mdb
	

