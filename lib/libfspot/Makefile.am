INCLUDES =							\
	-I $(top_srcdir) -I $(top_srcdir)/lib/		\
	-DLIBEOG_ETTORE_CHANGES=1				\
        -DG_LOG_DOMAIN=\"libf\"					\
	-DG_DISABLE_DEPRECATED -DGTK_DISABLE_DEPRECATED		\
	-DGDK_DISABLE_DEPRECATED -DGNOME_DISABLE_DEPRECATED	\
	-DG_DISABLE_SINGLE_INCLUDES				\
	-DGDK_PIXBUF_DISABLE_SINGLE_INCLUDES			\
	-DGTK_DISABLE_SINGLE_INCLUDES				\
	$(F_CFLAGS)						\
	$(LCMS_CFLAGS)						\
	$(WERROR)

fspotlibdir = $(libdir)/f-spot
fspotlib_LTLIBRARIES = libfspot.la

libfspot_la_SOURCES =				\
	f-image-surface.c			\
	f-image-surface.h			\
	f-pixbuf-unsharp.c			\
	f-pixbuf-utils.c			\
	f-pixbuf-utils.h			\
	f-screen-utils.c

libfspot_la_LIBADD = 					\
	-lX11						\
	$(F_LIBS)					\
	$(LCMS_LIBS)					\
	$(GIO_LIBS)
