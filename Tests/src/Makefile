all: test

TESTASSEMBLY = f-spot.tests.dll

SOURCES = 				\
	IBrowsableItem.cs		\
	PhotoStore.cs			\
	TagStore.cs			\
	Query/LogicalTerm.cs		

PKGS = 					\
	-pkg:mono-nunit			\
	-pkg:gnome-vfs-sharp-2.0	\
	-pkg:gtk-sharp-2.0

REFS =					\
	-r:../../src/f-spot.exe		\
	-r:../../src/FSpot.Core.dll	\
	-r:../../semweb/SemWeb.dll


MCSFLAGS = -t:library -d:ENABLE_NUNIT

$(TESTASSEMBLY): $(SOURCES)
	gmcs $(MCSFLAGS) $(PKGS) $(REFS) $(SOURCES) -out:$(TESTASSEMBLY)

test: $(TESTASSEMBLY)
	export MONO_PATH=../../src:../../semweb; nunit-console2 $(TESTASSEMBLY)

clean:
	rm -f *~ *.dll TestResult.xml *.db
