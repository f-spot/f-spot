INCLUDES =							\
	-I $(top_srcdir)					\
	-DLIBEOG_ETTORE_CHANGES=1				\
        -DG_LOG_DOMAIN=\"libf\"					\
	-DG_DISABLE_DEPRECATED -DGTK_DISABLE_DEPRECATED		\
	-DGDK_DISABLE_DEPRECATED -DGNOME_DISABLE_DEPRECATED	\
	$(F_CFLAGS)						\
	$(EXIF_CFLAGS)						\
	$(LCMS_CFLAGS)						\
	$(WERROR)

fspotlibdir = $(libdir)/f-spot
fspotlib_LTLIBRARIES = libfspot.la

libfspot_la_SOURCES =				\
	f-image-surface.c			\
	f-image-surface.h			\
	f-image-view.c				\
	f-image-view.h				\
	f-jpeg-utils.c				\
	f-jpeg-utils.h				\
	f-pixbuf-save.c				\
	f-pixbuf-utils.c			\
	f-pixbuf-utils.h			\
	f-pixbuf-unsharp.c			\
	f-marshal.c				\
	f-marshal.h				\
	f-screen-utils.c			\
	f-utils.c				\
	f-utils.h

libfspot_la_LIBADD = 					\
	$(LCMS_LIBS)					\
	$(EXIF_LIBS)					\
	$(top_builddir)/libjpegtran/libfspotjpegtran.la	\
	$(top_builddir)/libeog/libfspoteog.la


# GLib marshalling cruft

f-marshal.h: f-marshal.list
	( @GLIB_GENMARSHAL@ --prefix=f_marshal $(srcdir)/f-marshal.list --header > f-marshal.tmp \
	&& mv f-marshal.tmp f-marshal.h ) \
	|| ( rm -f f-marshal.tmp && exit 1 )

f-marshal.c: f-marshal.h
	( @GLIB_GENMARSHAL@ --prefix=f_marshal $(srcdir)/f-marshal.list --body > f-marshal.tmp \
	&& mv f-marshal.tmp f-marshal.c ) \
	|| ( rm -f f-marshal.tmp && exit 1 )

MARSHAL_GENERATED = f-marshal.c f-marshal.h


#

BUILT_SOURCES = $(MARSHAL_GENERATED)

EXTRA_DIST =					\
	f-marshal.list
