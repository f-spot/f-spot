all: BlackoutEditor.dll

PACKAGES = \
	-pkg:f-spot \
	-pkg:gtk-sharp-2.0

ASSEMBLIES = \
	-r:Mono.Posix

RESOURCES = \
	-resource:BlackoutEditor.addin.xml

SOURCES = \
	BlackoutEditor.cs

install: all
	cp *.dll ~/.config/f-spot/addins/

mpack: BlackoutEditor.dll
	mautil p BlackoutEditor.dll

BlackoutEditor.dll: $(SOURCES) BlackoutEditor.addin.xml
	gmcs -target:library $(SOURCES) $(PACKAGES) $(ASSEMBLIES) $(RESOURCES)

clean:
	rm -f *.dll *~ *.bak .mpack

PHONY:
	install clean all mpack
