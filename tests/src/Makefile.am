ASSEMBLY = FSpot.Tests
TARGET = library
LINK = $(REF_FSPOT_TESTS)
SOURCES = \
	SafeUriTest.cs \
	Query/LogicalTerm.cs \
	Cms/Cms.cs

RESOURCES = \
	../../icons/f-spot-32.png


include $(top_srcdir)/build/build.mk


test: $(ASSEMBLY_FILE)
	export MONO_PATH=$(DIR_BIN):$(top_srcdir)/lib/gio-sharp/gio/:$(top_srcdir)/lib/gtk-sharp-beans; nunit-console -noshadow -nologo $(ASSEMBLY_FILE)

