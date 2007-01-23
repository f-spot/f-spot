noinst_DATA =					\
	f-spot-adjust-colors.png		\
	f-spot-autocolor.png	\
	f-spot-browse.png			\
	f-spot-camera.png			\
	f-spot-crop.png				\
	f-spot-desaturate.png			\
	f-spot-edit-image.png			\
	f-spot-events.png			\
	f-spot-favorite.png			\
	f-spot-fullscreen.png			\
	f-spot-hidden.png			\
	f-spot-horizon.png		\
	f-spot-imported-xmp-tags.png		\
	f-spot-info.png			\
	f-spot-loading.png			\
	f-spot-new-tag.png			\
	f-spot-not.png				\
	f-spot-other.png			\
	f-spot-people.png			\
	f-spot-places.png			\
	f-spot-red-eye.png			\
	f-spot-rotate-90.png			\
	f-spot-rotate-270.png			\
	f-spot-sepia.png			\
	f-spot-slideshow.png			\
	f-spot-soft-focus.png		\
	f-spot-stock_near.png			\
	f-spot-stock_far.png			\
	f-spot-question-mark.png		\
	f-spot-view-restore.png		\
	f-spot.js				\
	f-spot-simple.css			\
	f-spot-simple-white.css

hicolordir = $(DESTDIR)$(datadir)/icons/hicolor

install-data-local:
	$(mkinstalldirs) $(hicolordir)/16x16/apps
	$(INSTALL_DATA) $(srcdir)/f-spot-16.png $(hicolordir)/16x16/apps/f-spot.png
	$(INSTALL_DATA) $(srcdir)/f-spot-16.svg $(hicolordir)/16x16/apps/f-spot.svg
	$(mkinstalldirs) $(hicolordir)/22x22/apps
	$(INSTALL_DATA) $(srcdir)/f-spot-22.png $(hicolordir)/22x22/apps/f-spot.png
	$(INSTALL_DATA) $(srcdir)/f-spot-22.svg $(hicolordir)/22x22/apps/f-spot.svg
	$(mkinstalldirs) $(hicolordir)/24x24/apps
	$(INSTALL_DATA) $(srcdir)/f-spot-24.png $(hicolordir)/24x24/apps/f-spot.png
	$(mkinstalldirs) $(hicolordir)/32x32/apps
	$(INSTALL_DATA) $(srcdir)/f-spot-32.png $(hicolordir)/32x32/apps/f-spot.png
	$(INSTALL_DATA) $(srcdir)/f-spot-32.svg $(hicolordir)/32x32/apps/f-spot.svg
	$(mkinstalldirs) $(hicolordir)/48x48/apps
	$(INSTALL_DATA) $(srcdir)/f-spot-48.png $(hicolordir)/48x48/apps/f-spot.png
	$(INSTALL_DATA) $(srcdir)/f-spot-48.svg $(hicolordir)/48x48/apps/f-spot.svg

	$(mkinstalldirs) $(hicolordir)/scalable/apps
	$(INSTALL_DATA) $(srcdir)/f-spot-logo-tango.svg $(hicolordir)/scalable/apps/f-spot.svg

uninstall-hook:
	rm -f $(hicolordir)/16x16/apps/f-spot.png
	rm -f $(hicolordir)/16x16/apps/f-spot.svg
	rm -f $(hicolordir)/22x22/apps/f-spot.png
	rm -f $(hicolordir)/22x22/apps/f-spot.svg
	rm -f $(hicolordir)/24x24/apps/f-spot.png
	rm -f $(hicolordir)/32x32/apps/f-spot.png
	rm -f $(hicolordir)/32x32/apps/f-spot.svg
	rm -f $(hicolordir)/48x48/apps/f-spot.png
	rm -f $(hicolordir)/48x48/apps/f-spot.svg
	rm -f $(hicolordir)/scalable/apps/f-spot.svg

EXTRA_DIST = 			\
	$(noinst_DATA)		\
	f-spot-16.png		\
	f-spot-16.svg		\
	f-spot-22.png		\
	f-spot-22.svg		\
	f-spot-24.png		\
	f-spot-32.png		\
	f-spot-32.svg		\
	f-spot-48.png		\
	f-spot-48.svg		\
	f-spot-logo-tango.svg
