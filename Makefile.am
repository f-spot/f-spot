SUBDIRS = \
	build    \
	data     \
	lib      \
	external \
	docs     \
	help     \
	icons    \
	tools    \
	po       \
	src      \
	tests

CLEANFILES = \
	$(schema_DATA)   \
	intltool-extract \
	intltool-update  \
	intltool-merge

EXTRA_DIST = \
	COPYING              \
	MAINTAINERS          \
	intltool-extract.in  \
	intltool-merge.in    \
	intltool-update.in   \
	Makefile.include     \
	gnome-doc-utils.make \
	icon-theme-installer \
	F-Spot.sln

DISTCLEANFILES = \
	intltool-extract        \
	intltool-merge          \
	intltool-update         \
	gnome-doc-utils.make

DISTCHECK_CONFIGURE_FLAGS = --disable-scrollkeeper

all: sln_build

csharp:
	@pushd .; \
	echo ""; \
	echo "Starting csharp with Gtk#, Hyena, FSpot, etc referenced"; \
	csharp -lib:bin/ -pkg:glib-sharp-2.0 -pkg:gtk-sharp-2.0 -r:lib/unique-sharp/unique/unique-sharp.dll -reference:bin/f-spot.exe $(addprefix "-reference:", $(wildcard bin/*.dll)); \
	popd;

run:
	@pushd bin/; \
	../src/Clients/FSpot.Gtk/f-spot --uninstalled --debug; \
	popd;

copy_bin:
	mkdir -p $(DESTDIR)/$(libdir)/f-spot
	cp -r bin/* $(DESTDIR)/$(libdir)/f-spot/

test:
	@pushd tests; \
	make test; \
	popd;

include $(top_srcdir)/build/xbuild.include
