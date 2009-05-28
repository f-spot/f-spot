all: MetaPixel.dll

PACKAGES = \
	-pkg:f-spot \
	-pkg:gio-sharp-2.0 \
	-pkg:gtk-sharp-2.0 \
	-pkg:glade-sharp-2.0

ASSEMBLIES = \
	-r:Mono.Posix

RESOURCES = \
	-resource:MetaPixel.glade \
	-resource:MetaPixel.addin.xml

SOURCES = \
	MetaPixel.cs

install: all
	cp *.dll ~/.config/f-spot/addins/

mpack: MetaPixel.dll
	mautil p MetaPixel.dll

%.dll: %.cs %.glade
	gmcs -target:library $(SOURCES) $(PACKAGES) $(ASSEMBLIES) $(RESOURCES)

clean:
	rm -f *.dll *~ *.bak .mpack *.gladep

PHONY:
	install clean all mpack
