# Note the list below is sorted by alphabet, with the exception of libjpegtran,
# it needs to be built before libfspot.

UNCONDITIONAL_SUBDIRS = \
	GKeyFile 			\
	gio-sharp			\
	glitz-sharp			\
	gnome-keyring-sharp	\
	gtk-sharp-beans		\
	libjpegtran			\
	libfspot			\
	libgphoto2-sharp	\
	mono-addins			\
	semweb				\
	Tao

# Depending on whether the packaged ndesk-dbus is used, we might not want to
# build it, but we will always want to ship it. Also, dpap-sharp isn't built,
# but it is shipped.

SUBDIRS = 				\
	$(DIR_DBUS)			\
	$(DIR_DBUS_GLIB)	\
	$(UNCONDITIONAL_SUBDIRS)

DIST_SUBDIRS = 			\
	dbus-sharp			\
	dbus-sharp-glib		\
	dpap-sharp			\
	$(UNCONDITIONAL_SUBDIRS)

