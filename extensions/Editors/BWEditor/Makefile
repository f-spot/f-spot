all: BWEditor.dll

PACKAGES = \
	-pkg:f-spot \
	-pkg:gtk-sharp-2.0

ASSEMBLIES = \
	-r:Mono.Posix	\
	-r:Mono.Simd

RESOURCES = \
	-resource:BWEditor.addin.xml

SOURCES = \
	BWEditor.cs

install: all
	cp *.dll ~/.config/f-spot/addins/

mpack: BWEditor.dll
	mautil p BWEditor.dll

BWEditor.dll: $(SOURCES) BWEditor.addin.xml
	gmcs -target:library -unsafe $(SOURCES) $(PACKAGES) $(ASSEMBLIES) $(RESOURCES)

clean:
	rm -f *.dll *~ *.bak .mpack

PHONY:
	install clean all mpack
