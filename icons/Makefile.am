theme = hicolor
themedir = $(pkgdatadir)/icons/$(theme)
hicolordir = $(DESTDIR)$(datadir)/icons/hicolor

theme_icons = 					\
	actions,adjust-colors-16.png		\
	actions,adjust-colors-22.png		\
	actions,adjust-colors-24.png		\
	actions,align-horizon-16.png		\
	actions,align-horizon-22.png		\
	actions,align-horizon-24.png		\
	actions,autocolor-16.png		\
	actions,autocolor-22.png		\
	actions,autocolor-24.png		\
	actions,color-desaturate-16.png		\
	actions,color-desaturate-22.png		\
	actions,color-desaturate-24.png		\
	actions,color-sepia-16.png		\
	actions,color-sepia-22.png		\
	actions,crop-16.png			\
	actions,crop-22.png			\
	actions,filter-soft-focus-16.png	\
	actions,filter-soft-focus-22.png	\
	actions,filter-soft-focus-24.png	\
	actions,object-rotate-left.svg		\
	actions,object-rotate-left-16.png	\
	actions,object-rotate-left-22.png	\
	actions,object-rotate-left-24.png	\
	actions,object-rotate-left-32.png	\
	actions,object-rotate-right.svg		\
	actions,object-rotate-right-16.png	\
	actions,object-rotate-right-22.png	\
	actions,object-rotate-right-24.png	\
	actions,object-rotate-right-32.png	\
	actions,rating-blank-16.png		\
	actions,rating-rated-16.png		\
	actions,rating-rated-gray-16.png	\
	actions,red-eye-remove-16.png		\
	actions,red-eye-remove-22.png		\
	actions,red-eye-remove-24.png		\
	actions,tag-16.png			\
	actions,tag-22.png			\
	actions,tag-new-16.png			\
	actions,tag-new-22.png			\
	actions,tag-new-24.png			\
	actions,view-far-16.png			\
	actions,view-far-22.png			\
	actions,view-far-24.png			\
	actions,view-fullscreen.svg		\
	actions,view-fullscreen-16.png		\
	actions,view-fullscreen-22.png		\
	actions,view-fullscreen-24.png		\
	actions,view-fullscreen-32.png		\
	actions,view-near-16.png		\
	actions,view-near-22.png		\
	actions,view-near-24.png		\
	actions,view-restore.svg		\
	actions,view-restore-16.png		\
	actions,view-restore-22.png		\
	actions,view-restore-24.png		\
	actions,view-restore-32.png		\
	apps,f-spot-16.png			\
	apps,f-spot-22.png			\
	apps,f-spot-24.png			\
	apps,f-spot-32.png			\
	apps,f-spot-128.png			\
	apps,f-spot-256.png			\
	devices,camera-photo-16.png		\
	devices,camera-photo-22.png		\
	devices,camera-photo-24.png		\
	devices,camera-photo-32.png		\
	emblems,emblem-event-16.png		\
	emblems,emblem-event-22.png		\
	emblems,emblem-event-24.png		\
	emblems,emblem-event-32.png		\
	emblems,emblem-event.svg		\
	emblems,emblem-people-16.png		\
	emblems,emblem-people-22.png		\
	emblems,emblem-people-24.png		\
	emblems,emblem-people-32.png		\
	emblems,emblem-places-16.png		\
	emblems,emblem-places-22.png		\
	emblems,emblem-places-24.png		\
	emblems,emblem-places-32.png		\
	emblems,emblem-places.svg		\
	emblems,emblem-readonly-16.png		\
	emblems,emblem-readonly-22.png		\
	emblems,emblem-readonly-24.png		\
	emblems,emblem-readonly-32.png		\
	places,film-roll-16.png			\
	places,film-roll-22.png			\
	places,film-roll-24.png			\
	status,mode-browse.svg			\
	status,mode-browse-16.png		\
	status,mode-browse-22.png		\
	status,mode-browse-24.png		\
	status,mode-browse-32.png		\
	status,mode-image-edit-16.png		\
	status,mode-image-edit-22.png		\
	status,mode-image-edit-24.png		

install_icon_exec = $(top_srcdir)/icon-theme-installer \
	-t "$(theme)" \
	-s "$(srcdir)" \
	-d "x$(DESTDIR)" \
	-b "$(themedir)" \
	-m "$(mkinstalldirs)" \
	-x "$(INSTALL_DATA)"

install-data-local:
	@-$(install_icon_exec) -i $(theme_icons)
	$(mkinstalldirs) $(hicolordir)/16x16/apps
	$(INSTALL_DATA) $(srcdir)/f-spot-16.png $(hicolordir)/16x16/apps/f-spot.png
	$(mkinstalldirs) $(hicolordir)/22x22/apps
	$(INSTALL_DATA) $(srcdir)/f-spot-22.png $(hicolordir)/22x22/apps/f-spot.png
	$(mkinstalldirs) $(hicolordir)/24x24/apps
	$(INSTALL_DATA) $(srcdir)/f-spot-24.png $(hicolordir)/24x24/apps/f-spot.png
	$(mkinstalldirs) $(hicolordir)/32x32/apps
	$(INSTALL_DATA) $(srcdir)/f-spot-32.png $(hicolordir)/32x32/apps/f-spot.png
	$(mkinstalldirs) $(hicolordir)/48x48/apps
	$(INSTALL_DATA) $(srcdir)/f-spot-48.png $(hicolordir)/48x48/apps/f-spot.png
	$(mkinstalldirs) $(hicolordir)/128x128/apps
	$(INSTALL_DATA) $(srcdir)/f-spot-128.png $(hicolordir)/128x128/apps/f-spot.png
	$(mkinstalldirs) $(hicolordir)/256x256/apps
	$(INSTALL_DATA) $(srcdir)/f-spot-256.png $(hicolordir)/256x256/apps/f-spot.png

uninstall-hook: 
	@-$(install_icon_exec) -u $(theme_icons)
	rm -f $(hicolordir)/16x16/apps/f-spot.png
	rm -f $(hicolordir)/22x22/apps/f-spot.png
	rm -f $(hicolordir)/24x24/apps/f-spot.png
	rm -f $(hicolordir)/32x32/apps/f-spot.png
	rm -f $(hicolordir)/48x48/apps/f-spot.png
	rm -f $(hicolordir)/128x128/apps/f-spot.png
	rm -f $(hicolordir)/256x256/apps/f-spot.png

	
MAINTAINERCLEANFILES = Makefile.in
EXTRA_DIST = $(wildcard *.png *.svg)	\
	flower.png

