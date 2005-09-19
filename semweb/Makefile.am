CSC = mcs -g

SEMWEB_CSFILES =            			\
	$(srcdir)/AssemblyInfo.cs		\
	$(srcdir)/ForwardLogic.cs		\
	$(srcdir)/KnowledgeModel.cs		\
	$(srcdir)/MemoryStore.cs		\
	$(srcdir)/N3Parser.cs			\
	$(srcdir)/N3Writer.cs			\
	$(srcdir)/NamespaceManager.cs		\
	$(srcdir)/Query.cs			\
	$(srcdir)/RdfParser.cs			\
	$(srcdir)/RdfWriter.cs			\
	$(srcdir)/Reasoning.cs			\
	$(srcdir)/Resource.cs			\
	$(srcdir)/RSquary.cs			\
	$(srcdir)/RSquaryFilters.cs		\
	$(srcdir)/Sparql.cs			\
	$(srcdir)/SQLStore.cs			\
	$(srcdir)/Statement.cs			\
	$(srcdir)/Store.cs			\
	$(srcdir)/UriMap.cs			\
	$(srcdir)/Util.cs			\
	$(srcdir)/XmlParser.cs			\
	$(srcdir)/XmlWriter.cs			\
	$(srcdir)/XPathSemWebNavigator.cs

ASSEMBLIES = 					\
	-r:System.Data

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
