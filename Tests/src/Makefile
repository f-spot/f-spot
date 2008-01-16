all: test

TESTASSEMBLY = f-spot.tests.dll

SOURCES = 				\
	IBrowsableItem.cs		\
	TagStore.cs

PKG = 					\
	-pkg:mono-nunit			\
	-pkg:gnome-vfs-sharp-2.0	\
	-pkg:gtk-sharp-2.0

MCSFLAGS = -t:library $(PKG) -r:../../src/f-spot.exe -d:ENABLE_NUNIT

$(TESTASSEMBLY): $(SOURCES)
	gmcs $(MCSFLAGS) $(SOURCES) -out:$(TESTASSEMBLY)

test: $(TESTASSEMBLY)
	export MONO_PATH=../../src:../../semweb:$MONO_PATH; nunit-console2 $(TESTASSEMBLY)

clean:
	rm -f *~ *.dll TestResult.xml *.db
