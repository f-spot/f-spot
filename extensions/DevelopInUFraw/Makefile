all: DevelopInUFRaw.dll

PACKAGES = \
	-pkg:f-spot \
	-pkg:gnome-vfs-sharp-2.0

ASSEMBLIES = \
	-r:Mono.Posix

RESOURCES = \
	-resource:DevelopInUFRaw.addin.xml

SOURCES = \
	DevelopInUFRaw.cs

install: all
	cp *.dll ~/.gnome2/f-spot/addins/

mpack: all
	mautil p DevelopInUFRaw.dll

%.dll: $(SOURCES) DevelopInUFRaw.addin.xml
	gmcs -target:library $(SOURCES) $(PACKAGES) $(ASSEMBLIES) $(RESOURCES)

clean:
	rm -f *.dll *~ *.bak .mpack

PHONY:
	install clean all mpack
