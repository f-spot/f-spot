ADDIN = ChangePhotoPath

all: $(ADDIN).dll

PACKAGES = \
	-pkg:glade-sharp-2.0 \
	-pkg:gtk-sharp-2.0 \
	-pkg:f-spot

ASSEMBLIES =

RESOURCES = \
	-resource:ChangePhotoPath.glade \
	-resource:ChangePhotoPath.addin.xml

SOURCES = \
	ChangePhotoPathController.cs \
	ChangePhotoPathGui.cs \
	IChangePhotoPathGui.cs

$(ADDIN).dll: $(SOURCES) $(ADDIN).addin.xml $(ADDIN).glade
	gmcs -target:library -out:$@ $(SOURCES) $(PACKAGES) $(ASSEMBLIES) $(RESOURCES)

install: all
	cp $(ADDIN).dll ~/.gnome2/f-spot/addins/

mpack: $(ADDIN).dll
	mautil p $(ADDIN).dll

clean:
	rm -f *.dll *~ *.bak *.gladep *.mpack

PHONY:
	install clean all mpack
