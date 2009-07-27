ADDIN = SyncCatalog

all: $(ADDIN).dll

PACKAGES = \
	-pkg:gtk-sharp-2.0	\
	-pkg:f-spot

ASSEMBLIES = \
	-r:Mono.Posix

RESOURCES = \
	-resource:$(ADDIN).addin.xml

SOURCES = \
	$(ADDIN).cs

$(ADDIN).dll: $(SOURCES) $(ADDIN).addin.xml
	gmcs -target:library $(SOURCES) $(PACKAGES) $(ASSEMBLIES) $(RESOURCES)

clean:
	rm -f *.dll *~ *.bak *.mpack

install: $(ADDIN).dll
	cp $(ADDIN).dll ~/.config/f-spot/addins/

mpack: $(ADDIN).dll
	mautil p $(ADDIN).dll

PHONY:
	install clean all mpack
