all: PictureTile.dll

PACKAGES = \
	-pkg:f-spot \
	-pkg:gtk-sharp-2.0 \
	-pkg:glade-sharp-2.0

ASSEMBLIES = \
	-r:../../../lib/gio-sharp/gio/gio-sharp.dll \
	-r:Mono.Posix

RESOURCES = \
	-resource:PictureTile.glade \
	-resource:PictureTile.addin.xml

SOURCES = \
	PictureTile.cs

install: all
	cp *.dll ~/.config/f-spot/addins/

mpack: PictureTile.dll
	mautil p PictureTile.dll

%.dll: %.cs %.glade
	gmcs -target:library $(SOURCES) $(PACKAGES) $(ASSEMBLIES) $(RESOURCES)

clean:
	rm -f *.dll *~ *.bak .mpack *.gladep

PHONY:
	install clean all mpack
