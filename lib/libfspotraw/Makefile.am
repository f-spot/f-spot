include $(top_srcdir)/Makefile.include

LIBRAW = LibRaw-0.8.0-Beta3
INCLUDES = -I $(top_srcdir) -I $(top_srcdir)/lib/libfspotraw/LibRaw $(F_CFLAGS)

CFLAGS = -g -O4
CXXFLAGS = -g -O4

fspotlibdir = $(libdir)/f-spot
fspotlib_LTLIBRARIES = libfspotraw.la
libfspotraw_files = \
	LibRaw/libraw/libraw_const.h 					\
	LibRaw/libraw/libraw.h 							\
	LibRaw/libraw/libraw_version.h 					\
	LibRaw/libraw/libraw_types.h 					\
	LibRaw/libraw/libraw_internal.h 				\
	LibRaw/libraw/libraw_alloc.h 					\
	LibRaw/libraw/libraw_datastream.h 				\
	LibRaw/internal/var_defines.h 					\
	LibRaw/internal/libraw_internal_funcs.h 		\
	LibRaw/internal/dcraw_fileio.cpp 				\
	LibRaw/internal/defines.h 						\
	LibRaw/internal/foveon.cpp 						\
	LibRaw/internal/dcraw_common.cpp 				\
	LibRaw/dcraw/dcraw.c 							\
	LibRaw/src/libraw_c_api.cpp 					\
	LibRaw/src/libraw_cxx.cpp 						\
	fspot-librawloader.cpp 							\
	fspot-librawloader.h

libfspotraw_la_SOURCES = \
	$(libfspotraw_files)

extract: $(LIBRAW).tar.gz
	rm -rf LibRaw
	tar xvpzf $(LIBRAW).tar.gz
	mv $(LIBRAW) LibRaw
	git add LibRaw

EXTRA_DIST = 	\
	LibRaw/COPYRIGHT
