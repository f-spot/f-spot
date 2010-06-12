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
		fetch/gio/gio-api-2.22.raw		\
		fetch/gio/Gio.metadata		\
		fetch/gio/gtk-sharp.snk		\
		gio
	mv gio/gio-api-2.22.raw gio/gio-api.raw

.PHONY: fetch update
