all: RawPlusJpeg.dll

PACKAGES = \
	-pkg:gtk-sharp-2.0	\
	-pkg:f-spot

ASSEMBLIES =

RESOURCES = \
	-resource:RawPlusJpeg.addin.xml

SOURCES = \
	RawPlusJpeg.cs

install: all
	cp *.dll ~/.gnome2/f-spot/addins/

%.dll: $(SOURCES) RawPlusJpeg.addin.xml
	gmcs -target:library $(SOURCES) $(PACKAGES) $(ASSEMBLIES) $(RESOURCES)

clean:
	rm -f *.dll *~ *.bak

PHONY:
	install clean all
