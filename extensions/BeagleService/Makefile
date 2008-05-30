NAME = BeagleService

SOURCES = \
	BeagleService.cs

PACKAGES = \
	-pkg:f-spot

ASSEMBLIES = \

RESOURCES = \
	-resource:$(NAME).addin.xml

ASSEMBLY = $(NAME).dll

all: $(ASSEMBLY)

install: all
	mkdir -p ~/.gnome2/f-spot/addins/
	cp *.dll ~/.gnome2/f-spot/addins/

mpack: $(ASSEMBLY)
	mautil p $(ASSEMBLY)

$(ASSEMBLY): $(SOURCES) $(NAME).addin.xml
	gmcs -target:library -o:$(ASSEMBLY) $(SOURCES) $(PACKAGES) $(ASSEMBLIES) $(RESOURCES)

clean:
	rm -f *.dll *~ *.bak .mpack

PHONY:
	install clean all mpack
