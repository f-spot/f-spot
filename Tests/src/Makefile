all: test

TESTASSEMBLY = f-spot.tests.dll

SOURCES = 				\
	IBrowsableItem.cs		\
	ImageTest.cs			\
	PhotoStore.cs			\
	TagStore.cs			\
	ThumbnailGenerator.cs		\
	Query/LogicalTerm.cs		\
	Cms/Cms.cs

PKGS = 					\
	-pkg:mono-nunit			\
	-pkg:gnome-vfs-sharp-2.0	\
	-pkg:gnome-sharp-2.0		\
	-pkg:gtk-sharp-2.0

REFS =					\
	-r:../../src/f-spot.exe		\
	-r:../../src/FSpot.Core.dll	\
	-r:../../src/FSpot.Query.dll	\
	-r:../../src/FSpot.Utils.dll	\
	-r:../../src/FSpot.Platform.dll \
	-r:../../src/Cms.dll		\
	-r:../../semweb/SemWeb.dll

RESOURCES =							\
	-resource:../../icons/f-spot-32.png,f-spot-32.png


MCSFLAGS = -t:library -d:ENABLE_NUNIT

$(TESTASSEMBLY): $(SOURCES)
	gmcs $(MCSFLAGS) $(PKGS) $(REFS) $(SOURCES) $(RESOURCES) -out:$(TESTASSEMBLY)

test: $(TESTASSEMBLY)
	export MONO_PATH=../../src:../../semweb; nunit-console2 $(TESTASSEMBLY)

clean:
	rm -f *~ *.dll TestResult.xml *.db
