SUBDIRS = cursors

INCLUDES =							\
	-DG_LOG_DOMAIN=\"libfspoteog\"				\
	-DDATADIR=\""$(datadir)"\"				\
	-DLIBEOG_ETTORE_CHANGES=1				\
	-I$(srcdir) -I$(top_srcdir)				\
	-I$(top_srcdir)/src					\
	-I$(top_builddir)					\
	$(F_CFLAGS)						\
	$(LCMS_CFLAGS)


fspotlibdir = $(libdir)/f-spot
fspotlib_LTLIBRARIES = libfspoteog.la

libfspoteog_files =				\
	access.c				\
	access.h				\
        accessible-image-view-factory.h		\
        accessible-image-view-factory.c		\
        accessible-image-view.h			\
        accessible-image-view.c			\
	eog-image.h				\
	eog-image.c				\
	eog-pixbuf-util.h			\
	eog-pixbuf-util.c			\
	eog-scroll-view.c			\
	eog-scroll-view.h			\
	image-view.c				\
	image-view.h				\
	libeog-marshal.c			\
	libeog-marshal.h			\
	uta.c					\
	uta.h					\
	cursors.c				\
	cursors.h				\
	ui-image.c				\
	ui-image.h				\
	zoom.c					\
	zoom.h

libfspoteog_la_SOURCES = $(libfspoteog_files)


# GLib marshalling cruft

libeog-marshal.h: libeog-marshal.list
	( @GLIB_GENMARSHAL@ --prefix=libeog_marshal $(srcdir)/libeog-marshal.list --header > libeog-marshal.tmp \
	&& mv libeog-marshal.tmp libeog-marshal.h ) \
	|| ( rm -f libeog-marshal.tmp && exit 1 )

libeog-marshal.c: libeog-marshal.h
	( @GLIB_GENMARSHAL@ --prefix=libeog_marshal $(srcdir)/libeog-marshal.list --body > libeog-marshal.tmp \
	&& mv libeog-marshal.tmp libeog-marshal.c ) \
	|| ( rm -f libeog-marshal.tmp && exit 1 )

MARSHAL_GENERATED = libeog-marshal.c libeog-marshal.h


BUILT_SOURCES = $(MARSHAL_GENERATED)
EXTRA_DIST = libeog-marshal.list
