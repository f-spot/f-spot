SUBDIRS = 		\
	dbus-sharp	\
	dbus-sharp-glib	\
	docs		\
	FlickrNet	\
	SmugMugNet	\
	glitz-sharp	\
	google-sharp	\
	gnome-keyring-sharp \
	icons		\
	libeog		\
	libjpegtran	\
	libfspot	\
	libgphoto2-sharp \
	semweb		\
	Tao		\
	tools		\
	po		\
	src	

desktop_in_files=	\
	f-spot.desktop.in	\
	f-spot-view.desktop.in

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
	am-config.mk		\
        $(desktop_in_files)     \
        $(desktop_files)

DISTCLEANFILES = \
        intltool-extract        \
        intltool-merge          \
        intltool-update         \
        $(desktop_files)

