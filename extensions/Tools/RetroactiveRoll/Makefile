all: RetroactiveRoll.dll

PACKAGES = \
	-pkg:f-spot

ASSEMBLIES = \
	-r:Mono.Posix -r:Mono.Data.SqliteClient

RESOURCES = \
	-resource:RetroactiveRoll.addin.xml

SOURCES = \
	RetroactiveRoll.cs

install: all
	cp *.dll ~/.config/f-spot/addins/

mpack: RetroactiveRoll.dll
	mautil p RetroactiveRoll.dll

%.dll: $(SOURCES) RetroactiveRoll.addin.xml
	gmcs -target:library $(SOURCES) $(PACKAGES) $(ASSEMBLIES) $(RESOURCES)

clean:
	rm -f *.dll *~ *.bak .mpack

PHONY:
	install clean all mpack
