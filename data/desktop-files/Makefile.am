desktop_in_files=	\
	f-spot.desktop.in.in	\
	f-spot-view.desktop.in.in	\
	f-spot-import.desktop.in.in

Applicationsdir = $(datadir)/applications
Applications_DATA = $(desktop_files)

desktop_files=	\
	f-spot.desktop	\
	f-spot-view.desktop	\
	f-spot-import.desktop

%.desktop: %.desktop.in.in
	cp $< $@

CLEANFILES =			\
	$(desktop_files)

EXTRA_DIST =			\
	$(desktop_in_files)     \
	$(desktop_files)

DISTCLEANFILES = \
	$(desktop_files)

UPDATE_DESKTOP = \
	if [ -f $(DESTDIR)$(datadir)/applications/defaults.list ] ; then \
		if which update-desktop-database>/dev/null 2>&1 ; then \
			update-desktop-database $(DESTDIR)$(datadir)/applications; \
		fi \
	fi

install-data-hook: 
	$(UPDATE_DESKTOP)

uninstall-hook: 
	$(UPDATE_DESKTOP)
