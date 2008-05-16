SUBDIRS = 		\
	$(subdirs)	\
	$(DIR_DBUS)	\
	$(DIR_DBUS_GLIB)\
	docs		\
	glitz-sharp	\
	gnome-keyring-sharp \
	icons		\
	libeog		\
	libjpegtran	\
	libfspot	\
	libgphoto2-sharp \
	Tao		\
	mono-addins	\
	tools		\
	po		\
	src		\
	extensions

DIST_SUBDIRS = 		\
	$(subdirs)	\
	dbus-sharp	\
	dbus-sharp-glib	\
	docs		\
	glitz-sharp	\
	gnome-keyring-sharp \
	icons		\
	libeog		\
	libjpegtran	\
	libfspot	\
	libgphoto2-sharp \
	semweb		\
	Tao		\
	mono-addins	\
	tools		\
	po		\
	src		\
	extensions

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
	if [ "x$(DIR_GIO)" != "x" ] ; then \
		$(install_sh_DATA) $(top_builddir)/gio-sharp/gio/gio-sharp.dll $(libdir)/f-spot/; \
		$(install_sh_DATA) $(top_builddir)/gio-sharp/gio/gio-sharp.dll.config $(libdir)/f-spot/; \
	fi

uninstall-hook: 
	$(UPDATE_DESKTOP)
	if [ "x$(DIR_GIO)" != "x" ] ; then \
		rm -f $(libdir)/f-spot/gio-sharp.dll $(libdir)/f-spot/gio-sharp.dll.config; \
	fi
