pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_in_files = $(wildcard f-spot*.pc.in)
pkgconfig_DATA = $(pkgconfig_in_files:.pc.in=.pc)

%.pc: %.pc.in
	sed "s,\@VERSION\@,$(VERSION),g; s,\@prefix\@,$(prefix),g; s,\@libdir\@,$(libdir),g" < $< > $@

EXTRA_DIST = $(pkgconfig_in_files)

CLEANFILES = $(pkgconfig_DATA)
MAINTAINERCLEANFILES = Makefile.in
