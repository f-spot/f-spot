all: FlipEditor.dll

PACKAGES = \
	-pkg:f-spot \
	-pkg:gtk-sharp-2.0

ASSEMBLIES = \
	-r:Mono.Posix

RESOURCES = \
	-resource:FlipEditor.addin.xml

SOURCES = \
	FlipEditor.cs

install: all
	cp *.dll ~/.config/f-spot/addins/

mpack: FlipEditor.dll
	mautil p FlipEditor.dll

FlipEditor.dll: $(SOURCES) FlipEditor.addin.xml
	gmcs -target:library $(SOURCES) $(PACKAGES) $(ASSEMBLIES) $(RESOURCES)

clean:
	rm -f *.dll *~ *.bak .mpack

PHONY:
	install clean all mpack
