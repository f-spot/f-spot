@INTLTOOL_DESKTOP_RULE@

themesdir = @GNOME_SCREENSAVER_THEMESDIR@
themes_in_files = 			\
	f-spot-screensaver.desktop.in	\
	$(NULL)

themes_DATA = $(themes_in_files:.desktop.in=.desktop)

EXTRA_DIST = 			\
	$(saver_SCRIPTS)	\
	$(bin_SCRIPTS)		\
	$(themes_in_files)

DISTCLEANFILES = 	\
	$(themes_DATA)


saverdir = @GNOME_SCREENSAVER_SAVERDIR@
saver_SCRIPTS = f-spot-screensaver

bin_SCRIPTS = f-spot-import	\
	f-spot-sqlite-upgrade
