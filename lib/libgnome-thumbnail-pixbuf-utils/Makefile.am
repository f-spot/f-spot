AM_CPPFLAGS =                                                      \
        -I $(top_srcdir) -I $(top_srcdir)/lib/          \
        -DLIBEOG_ETTORE_CHANGES=1                               \
        -DG_LOG_DOMAIN=\"libf\"                                 \
        -DG_DISABLE_DEPRECATED -DGTK_DISABLE_DEPRECATED         \
        -DGDK_DISABLE_DEPRECATED -DGNOME_DISABLE_DEPRECATED     \
        -DG_DISABLE_SINGLE_INCLUDES                             \
        -DGDK_PIXBUF_DISABLE_SINGLE_INCLUDES                    \
        -DGTK_DISABLE_SINGLE_INCLUDES                           \
        $(F_CFLAGS)                                             \
        $(LCMS_CFLAGS)                                          \
        $(WERROR)


fspotlibdir = $(libdir)/f-spot
fspotlib_LTLIBRARIES = libgnomethumbnailpixbufutils.la

libgnomethumbnailpixbufutils_la_SOURCES =                           \
        gnome-thumbnail-pixbuf-utils.c

