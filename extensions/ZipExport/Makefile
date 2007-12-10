NAME = ZipExport

PACKAGES = \
	-pkg:f-spot \
	-pkg:gnome-vfs-sharp-2.0 \
	-pkg:gtk-sharp-2.0 \
	-pkg:glade-sharp-2.0

ASSEMBLIES = \
	-r:ICSharpCode.SharpZipLib \
	-r:Mono.Posix

RESOURCES = \
	-resource:$(NAME).glade \
	-resource:$(NAME).addin.xml 

SOURCES = \
	ZipExport.cs

ASSEMBLY = $(NAME).dll

all: $(ASSEMBLY)

install: all
	cp *.dll ~/.gnome2/f-spot/addins/

mpack: $(ASSEMBLY)
	mautil p $(ASSEMBLY) ZipExport.glade ZipExport.addin.xml

$(ASSEMBLY): $(SOURCES)  
	gmcs -out:$@ -target:library $(SOURCES) $(PACKAGES) $(ASSEMBLIES) $(RESOURCES)

clean:
	rm -f *.dll *~ *.bak *.mpack *.gladep

PHONY:
	install clean all mpack
