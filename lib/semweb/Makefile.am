include $(top_srcdir)/Makefile.include

ASSEMBLY_NAME = SemWeb

ASSEMBLY_SOURCES =				\
                $(srcdir)/AssemblyInfo.cs       \
                $(srcdir)/NamespaceManager.cs   \
                $(srcdir)/Util.cs               \
                $(srcdir)/UriMap.cs             \
                $(srcdir)/Resource.cs           \
                $(srcdir)/Statement.cs          \
                $(srcdir)/Store.cs              \
                $(srcdir)/MemoryStore.cs        \
                $(srcdir)/SQLStore.cs           \
                $(srcdir)/RdfReader.cs          \
                $(srcdir)/RdfXmlReader.cs       \
                $(srcdir)/N3Reader.cs           \
                $(srcdir)/RdfWriter.cs          \
                $(srcdir)/RdfXmlWriter.cs       \
                $(srcdir)/N3Writer.cs           \
                $(srcdir)/RSquary.cs            \
                $(srcdir)/LiteralFilters.cs     \
                $(srcdir)/Query.cs              \
                $(srcdir)/Inference.cs          \
                $(srcdir)/RDFS.cs               \
                $(srcdir)/Algos.cs              \
                $(srcdir)/Remote.cs             \
                $(srcdir)/XPathSemWebNavigator.cs             

REFS =			\
	-r:System.Data	\
	-r:System.Web

PKGS =

ASSEMBLY = $(ASSEMBLY_NAME).dll

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(CSC_LIB) -out:$@ $(PKGS) $(REFS) $(ASSEMBLY_SOURCES)

assemblydir = $(pkglibdir)
assembly_DATA =	$(ASSEMBLY)

EXTRA_DIST =				\
	$(ASSEMBLY_SOURCES)

CLEANFILES =			\
	$(ASSEMBLY)		\
	$(ASSEMBLY).mdb
