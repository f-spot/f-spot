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
	extensions	\
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

install-data-local:
	-GCONF_CONFIG_SOURCE=$(GCONF_SCHEMA_CONFIG_SOURCE) $(GCONFTOOL) --makefile-install-rule $(srcdir)/$(schema_DATA)

run:
	@pushd bin/; \
	../src/f-spot --uninstalled --debug; \
	popd;


test:
	@pushd tests; \
	make test \
	popd;
