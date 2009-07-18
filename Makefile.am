SUBDIRS = 		\
	lib 		\
	docs		\
	icons		\
	tools		\
	po			\
	src			\
	extensions	\
	tests

desktop_in_files=	\
	f-spot.desktop.in	\
	f-spot-view.desktop.in	\
	f-spot-import.desktop.in

desktop_files=$(desktop_in_files:.desktop.in=.desktop)
@INTLTOOL_DESKTOP_RULE@

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = f-spot.pc

Applicationsdir = $(datadir)/applications
Applications_DATA = $(desktop_files)

CLEANFILES =			\
	intltool-extract	\
	intltool-update		\
	intltool-merge

EXTRA_DIST =			\
	MAINTAINERS		\
	intltool-extract.in	\
	intltool-merge.in	\
	intltool-update.in	\
	f-spot.pc.in		\
	f-spot.spec		\
	Makefile.include	\
	gnome-doc-utils.make	\
	icon-theme-installer	\
        $(desktop_in_files)     \
        $(desktop_files)

DISTCLEANFILES = \
        intltool-extract        \
        intltool-merge          \
        intltool-update         \
	gnome-doc-utils.make	\
        $(desktop_files)

DISTCHECK_CONFIGURE_FLAGS = --disable-scrollkeeper

UPDATE_DESKTOP = \
	if [ -f $(DESTDIR)$(datadir)/applications/defaults.list ] ; then \
		if which update-desktop-database>/dev/null 2>&1 ; then \
			update-desktop-database; \
		fi \
	fi

install-data-hook: 
	$(UPDATE_DESKTOP)

uninstall-hook: 
	$(UPDATE_DESKTOP)

if ENABLE_TESTS
test:
	@pushd tests/src/; \
	make test; \
	popd;
endif
