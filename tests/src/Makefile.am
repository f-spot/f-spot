include $(top_srcdir)/Makefile.include

if ENABLE_TESTS

all: FSpot.Tests.dll

SOURCES = 				\
	IBrowsableItem.cs		\
	ImageTest.cs			\
	PhotoStore.cs			\
	TagStore.cs			\
	ThumbnailGenerator.cs		\
	Query/LogicalTerm.cs		\
	Cms/Cms.cs			\
	Filters/JpegFilter.cs		\
	Filters/OrientationFilter.cs

PKGS = 					\
	-pkg:mono-nunit			\
	-pkg:gnome-vfs-sharp-2.0	\
	-pkg:gnome-sharp-2.0		\
	-pkg:gtk-sharp-2.0

REFS =										\
	$(LINK_GPHOTO2)							\
	$(LINK_MONO_ADDINS)						\
	$(LINK_GIOSHARP)						\
	-r:$(top_srcdir)/src/f-spot.exe			\
	-r:$(top_srcdir)/src/FSpot.Core.dll		\
	-r:$(top_srcdir)/src/FSpot.Query.dll	\
	-r:$(top_srcdir)/src/FSpot.Utils.dll	\
	-r:$(top_srcdir)/src/FSpot.Platform.dll	\
	-r:$(top_srcdir)/src/Cms.dll			\
	-r:$(top_srcdir)/lib/semweb/SemWeb.dll


RESOURCES =							\
	-resource:$(top_srcdir)/icons/f-spot-32.png,f-spot-32.png

TEST_ASSEMBLIES =				\
	FSpot.Tests.dll

MCSFLAGS = -t:library -d:ENABLE_NUNIT

FSpot.Tests.dll: $(SOURCES)
	gmcs $(MCSFLAGS) $(PKGS) $(REFS) $(SOURCES) $(RESOURCES) -out:FSpot.Tests.dll

test: FSpot.Tests.dll
	export MONO_PATH=$(top_srcdir)/src:$(top_srcdir)/gio-sharp/gio/:$(top_srcdir)/lib/semweb:$(top_srcdir)/gtk-sharp-beans; nunit-console -noshadow -nologo $(TEST_ASSEMBLIES) 

CLEANFILES =			\
	FSpot.Tests.dll.mdb	\
	FSpot.Tests.dll		\
	PhotoTestStore.db	\
	TagTestStore.db		\
	TestResult.xml

endif

MAINTAINERCLEANFILES = \
	Makefile.in

EXTRA_DIST =			\
	$(SOURCES)
