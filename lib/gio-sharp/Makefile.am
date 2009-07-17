SUBDIRS =					\
	generator				\
	gio

fetch:
	rm -rf fetch
	git clone git://gitorious.org/gio-sharp/mainline.git fetch

update:
	cp fetch/generator/*.cs generator/
	cp	fetch/gio/*.custom 		\
		fetch/gio/*.cs			\
		fetch/gio/gio-api.raw		\
		fetch/gio/Gio.metadata		\
		fetch/gio/gtk-sharp.snk		\
		gio

.PHONY: fetch update
