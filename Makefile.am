SUBDIRS = 		\
	$(subdirs)	\
	lib 		\
	$(DIR_DBUS)	\
	$(DIR_DBUS_GLIB)\
	gio-sharp	\
	gtk-sharp-beans	\
	semweb		\
	docs		\
	gnome-keyring-sharp \
	icons		\
	libjpegtran	\
	libfspot	\
	libgphoto2-sharp \
	Tao		\
	tools		\
	po		\
	src		\
	extensions	\
	tests

DIST_SUBDIRS = 		\
	lib 		\
	gio-sharp	\
	gtk-sharp-beans	\
	semweb		\
	dbus-sharp	\
	dbus-sharp-glib	\
	dpap-sharp	\
	docs		\
	gnome-keyring-sharp \
	icons		\
	libjpegtran	\
	libfspot	\
	libgphoto2-sharp \
	Tao		\
	tools		\
	po		\
	src		\
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
