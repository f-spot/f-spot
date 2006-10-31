CSC = gmcs -g

SEMWEB_CSFILES =            			\
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

ASSEMBLIES = 					\
	-r:System.Data				\
	-r:System.Web

SemWeblibdir = $(libdir)/f-spot
SemWeblib_DATA = \
	SemWeb.dll

SemWeb.dll: $(SEMWEB_CSFILES)
	$(CSC) -out:$@ -t:library $(SEMWEB_CSFILES) $(ASSEMBLIES)

all: SemWeb.dll

EXTRA_DIST =					\
	$(SEMWEB_CSFILES)

CLEANFILES =					\
	SemWeb.dll.mdb				\
	SemWeb.dll
