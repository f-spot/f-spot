INCLUDES =							\
	-I $(top_srcdir)					\
	-DLIBEOG_ETTORE_CHANGES=1				\
        -DG_LOG_DOMAIN=\"libf\"					\
	-DG_DISABLE_DEPRECATED -DGTK_DISABLE_DEPRECATED		\
	-DGDK_DISABLE_DEPRECATED -DGNOME_DISABLE_DEPRECATED	\
	$(F_CFLAGS)						\
	$(WERROR)

fspotlibdir = $(prefix)/lib/f-spot
fspotlib_LTLIBRARIES = libfspot.la

libfspot_la_SOURCES =				\
	f-image-view.c				\
	f-image-view.h				\
	f-jpeg-utils.c				\
	f-jpeg-utils.h				\
	f-pixbuf-utils.c			\
	f-pixbuf-utils.h			\
	f-marshal.c				\
	f-marshal.h				\
	f-utils.c				\
	f-utils.h

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
