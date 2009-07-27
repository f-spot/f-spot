all: PixelateEditor.dll

PACKAGES = \
	-pkg:f-spot \
	-pkg:gtk-sharp-2.0

ASSEMBLIES = \
	-r:Mono.Posix

RESOURCES = \
	-resource:PixelateEditor.addin.xml

SOURCES = \
	PixelateEditor.cs

install: all
	cp *.dll ~/.config/f-spot/addins/

mpack: PixelateEditor.dll
	mautil p PixelateEditor.dll

PixelateEditor.dll: $(SOURCES) PixelateEditor.addin.xml
	gmcs -target:library $(SOURCES) $(PACKAGES) $(ASSEMBLIES) $(RESOURCES)

clean:
	rm -f *.dll *~ *.bak .mpack

PHONY:
	install clean all mpack
