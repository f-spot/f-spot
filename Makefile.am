SUBDIRS = 		\
	icons		\
	libeog		\
	libjpegtran	\
	libfspot	\
	libgphoto2-sharp \
	semweb		\
	po		\
	src	

desktop_in_files=	\
	f-spot.desktop.in	\
	f-spot-view.desktop.in

desktop_files=$(desktop_in_files:.desktop.in=.desktop)
@INTLTOOL_DESKTOP_RULE@

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
	f-spot.spec		\
        $(desktop_in_files)     \
        $(desktop_files)

DISTCLEANFILES = \
        intltool-extract        \
        intltool-merge          \
        intltool-update         \
        $(desktop_files)

