INCLUDES =			\
	-DHAVE_LIBEXIF		\
	$(F_CFLAGS)

fspotlibdir = $(libdir)/f-spot
fspotlib_LTLIBRARIES = libfspotjpegtran.la

libfspotjpegtran_files =			\
	jpeg-data.c				\
	jpeg-data.h				\
	jpeg-marker.c				\
	jpeg-marker.h				\
	jpegtran.c				\
	jpegtran.h				\
	transupp.c				\
	transupp.h

libfspotjpegtran_la_SOURCES =			\
	$(libfspotjpegtran_files)


EXTRA_DIST = README
