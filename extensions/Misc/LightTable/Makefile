NAME = LightTable

SOURCES = \
	LightTable.cs

PACKAGES = \
	-pkg:f-spot \
	-pkg:gtk-sharp-2.0 \
	-pkg:silverdesktop \
	-pkg:gtksilver

ASSEMBLIES = \
	-r:Mono.Posix

RESOURCES = \
	-resource:$(NAME).addin.xml \
	-resource:Photo.xaml

ASSEMBLY = $(NAME).dll

all: $(ASSEMBLY)

install: all
	cp *.dll ~/.config/f-spot/addins/

mpack: $(ASSEMBLY)
	mautil p $(ASSEMBLY)

$(ASSEMBLY): $(SOURCES) $(NAME).addin.xml Photo.xaml
	gmcs -target:library -o:$(ASSEMBLY) $(SOURCES) $(PACKAGES) $(ASSEMBLIES) $(RESOURCES)

clean:
	rm -f *.dll *~ *.bak .mpack

PHONY:
	install clean all mpack
