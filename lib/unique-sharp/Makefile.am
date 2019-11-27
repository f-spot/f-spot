LINK =

include $(top_srcdir)/build/build.mk

include $(top_srcdir)/build/xbuild.include


fetch:
	rm -rf fetch
	git clone git://gitorious.org/unique-sharp/unique-sharp.git fetch

update:
	cp fetch/generator/*.cs generator/
	cp	fetch/unique/*.custom 		\
		fetch/unique/*.cs			\
		fetch/unique/unique-api.raw		\
		fetch/unique/Unique.metadata		\
		unique

.PHONY: fetch update
