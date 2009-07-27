all: ResizeEditor.dll

PACKAGES = \
	-pkg:f-spot \
	-pkg:gtk-sharp-2.0

ASSEMBLIES = \
	-r:Mono.Posix

RESOURCES = \
	-resource:ResizeEditor.addin.xml

SOURCES = \
	ResizeEditor.cs

install: all
	cp *.dll ~/.config/f-spot/addins/

mpack: ResizeEditor.dll
	mautil p ResizeEditor.dll

ResizeEditor.dll: $(SOURCES) ResizeEditor.addin.xml
	gmcs -target:library $(SOURCES) $(PACKAGES) $(ASSEMBLIES) $(RESOURCES)

clean:
	rm -f *.dll *~ *.bak .mpack

PHONY:
	install clean all mpack
