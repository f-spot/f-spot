NAME = FacebookExport

SOURCES = \
	FacebookExport.cs 

PACKAGES = \
	-pkg:glade-sharp-2.0 \
	-pkg:gtk-sharp-2.0 \
	-pkg:f-spot

ASSEMBLIES = \
	-r:Mono.Posix			\
	-r:Mono.Facebook.dll

RESOURCES = \
	-resource:$(NAME).glade \
	-resource:$(NAME).addin.xml

ASSEMBLY = $(NAME).dll

all: $(ASSEMBLY)

install: all
	cp *.dll ~/.gnome2/f-spot/addins/

mpack: $(ASSEMBLY)
	mautil p $(ASSEMBLY)

$(ASSEMBLY): $(SOURCES) $(NAME).addin.xml
	gmcs -target:library -out:$(ASSEMBLY) $(SOURCES) $(PACKAGES) $(ASSEMBLIES) $(RESOURCES) 

clean:
	rm -f $(ASSEMBLY) *~ *.bak *.mpack *.gladep
