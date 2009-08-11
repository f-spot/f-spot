include $(top_srcdir)/Makefile.include

if ENABLE_TESTS

all: FSpot.Tests.dll

SOURCES = 				\
	$(srcdir)/IBrowsableItem.cs		\
	$(srcdir)/ImageTest.cs			\
	$(srcdir)/PhotoStore.cs			\
	$(srcdir)/TagStore.cs			\
	$(srcdir)/ThumbnailGenerator.cs		\
	$(srcdir)/Query/LogicalTerm.cs		\
	$(srcdir)/Cms/Cms.cs			\
	$(srcdir)/Filters/JpegFilter.cs		\
	$(srcdir)/Filters/OrientationFilter.cs

PKGS = 					\
	-pkg:mono-nunit			\
	-pkg:gnome-vfs-sharp-2.0	\
	-pkg:gnome-sharp-2.0		\
	-pkg:gtk-sharp-2.0

REFS =										\
	$(LINK_GPHOTO2)							\
	$(LINK_MONO_ADDINS)						\
	$(LINK_GIOSHARP)						\
	-r:$(top_builddir)/src/f-spot.exe			\
	-r:$(top_builddir)/src/FSpot.Core.dll		\
	-r:$(top_builddir)/src/FSpot.Query.dll	\
	-r:$(top_builddir)/src/FSpot.Utils.dll	\
	-r:$(top_builddir)/src/FSpot.Platform.dll	\
	-r:$(top_builddir)/src/Cms.dll			\
	-r:$(top_builddir)/lib/semweb/SemWeb.dll


RESOURCES =							\
	-resource:$(top_srcdir)/icons/f-spot-32.png,f-spot-32.png

TEST_ASSEMBLIES =				\
	FSpot.Tests.dll

MCSFLAGS = -t:library -d:ENABLE_NUNIT

FSpot.Tests.dll: $(SOURCES)
	gmcs $(MCSFLAGS) $(PKGS) $(REFS) $(SOURCES) $(RESOURCES) -out:FSpot.Tests.dll

test: FSpot.Tests.dll
	export MONO_PATH=$(top_srcdir)/src:$(top_srcdir)/lib/gio-sharp/gio/:$(top_srcdir)/lib/semweb:$(top_srcdir)/lib/gtk-sharp-beans; nunit-console -noshadow -nologo $(TEST_ASSEMBLIES) 

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
