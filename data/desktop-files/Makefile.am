desktop_in_files=	\
	f-spot.desktop.in	\
	f-spot-view.desktop.in	\
	f-spot-import.desktop.in

Applicationsdir = $(datadir)/applications
Applications_DATA = $(desktop_files)

desktop_files=$(desktop_in_files:.desktop.in=.desktop)
@INTLTOOL_DESKTOP_RULE@

CLEANFILES =			\
	$(desktop_DATA)

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
