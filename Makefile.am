SUBDIRS = 		\
	build 		\
	data 		\
	lib 		\
	docs		\
    help        \
	icons		\
	tools		\
	po			\
	src			\
	tests

schemadir = $(sysconfdir)/gconf/schemas
schema_in_files = f-spot.schemas.in
schema_DATA = $(schema_in_files:.schemas.in=.schemas)
@INTLTOOL_SCHEMAS_RULE@

CLEANFILES =			\
	$(schema_DATA)		\
	intltool-extract	\
	intltool-update		\
	intltool-merge

EXTRA_DIST =			\
	COPYING				\
	MAINTAINERS			\
	intltool-extract.in	\
	intltool-merge.in	\
	intltool-update.in	\
	Makefile.include	\
	gnome-doc-utils.make	\
	icon-theme-installer	\
	$(schema_in_files)

DISTCLEANFILES = \
	intltool-extract        \
	intltool-merge          \
	intltool-update         \
	gnome-doc-utils.make

DISTCHECK_CONFIGURE_FLAGS = --disable-scrollkeeper

if GCONF_SCHEMAS_INSTALL
install-data-local:
	-GCONF_CONFIG_SOURCE=$(GCONF_SCHEMA_CONFIG_SOURCE) $(GCONFTOOL) --makefile-install-rule $(srcdir)/$(schema_DATA)
else
install-data-local:
endif

csharp:
	@pushd .; \
	echo ""; \
	echo "Starting csharp with Gtk#, Hyena, FSpot, etc referenced"; \
	csharp -lib:bin/ -pkg:glib-sharp-2.0 -pkg:gtk-sharp-2.0 -pkg:gconf-sharp-2.0 -pkg:mono-addins -r:lib/gio-sharp/gio/gio-sharp.dll -r:lib/unique-sharp/unique/unique-sharp.dll -reference:bin/f-spot.exe $(addprefix "-reference:", $(wildcard bin/*.dll)); \
	popd;

run:
	@pushd bin/; \
	../src/Clients/MainApp/f-spot --uninstalled --debug; \
	popd;


test:
	@pushd tests; \
	make test \
	popd;
