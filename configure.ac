AC_INIT(F-Spot, 0.6.1.3)
AC_CONFIG_SRCDIR(src/main.cs)

AM_CONFIG_HEADER(config.h)

AM_INIT_AUTOMAKE([tar-ustar])

AM_MAINTAINER_MODE

IT_PROG_INTLTOOL([0.35.0])

AC_PROG_CC
AC_PROG_CXX
AC_ISC_POSIX
AC_HEADER_STDC
AM_PROG_LIBTOOL

GNOME_COMPILE_WARNINGS
AM_PATH_GLIB_2_0

dnl --- Check for mono and gmcs

AC_PATH_PROG(MONO, mono)
AC_PATH_PROG(MCS, gmcs)

CS="C#"
if test ! -x "$MCS"; then
	AC_MSG_ERROR([No $CS compiler found])
fi

dnl -- Initialize docs

GNOME_DOC_INIT

dnl -- Check for mono pc file

AC_MSG_CHECKING([for mono.pc])
if test -z `$PKG_CONFIG --variable=prefix mono`; then
  AC_MSG_ERROR([missing the mono.pc file, usually found in the mono-devel package])
else
  AC_MSG_RESULT([found])
fi

dnl -- check for various mono DLLs that we need.

needed_dlls="Mono.Data.SqliteClient Mono.Posix System.Runtime.Remoting System.Web System.Web.Services Mono.Cairo"
for i in $needed_dlls; do
  AC_MSG_CHECKING([for $i.dll])
  if test ! \( -e `$PKG_CONFIG --variable=prefix mono`/lib/mono/2.0/$i.dll -o -e `$PKG_CONFIG --variable=prefix mono`/lib64/mono/2.0//$i.dll \); then
    AC_MSG_ERROR([missing required mono 2.0 DLL: $i.dll])
  else
    AC_MSG_RESULT([found])
  fi
done

dnl --- Required libraries

LIBGNOME_REQUIRED=2.2
LIBGNOMEUI_REQUIRED=2.2
LIBEXIF_REQUIRED_MIN=0.5.7
LIBEXIF_REQUIRED_MAX=0.7.0
GIO_REQUIRED=2.16.0
GIOSHARP_REQUIRED=2.13.92
GTKSHARPBEANS_REQUIRED=2.13.92
GTKSHARP_REQUIRED=2.12.2
MONO_REQUIRED=2.0.0
GTK_REQUIRED=2.14
BEAGLE_REQUIRED=0.3.0
NDESK_DBUS_REQUIRED=0.4.2
NDESK_DBUS_GLIB_REQUIRED=0.3.0
MONO_CAIRO_REQUIRED=1.2.4
dnl -- this check is 	
LCMS_REQUIRED=1.12
dnl -- LIBGPHOTO2_REQUIRED=2.1.4

PKG_CHECK_MODULES(F, libgnome-2.0 >= $LIBGNOME_REQUIRED libgnomeui-2.0 >= $LIBGNOMEUI_REQUIRED libexif >= $LIBEXIF_REQUIRED_MIN libexif < $LIBEXIF_REQUIRED_MAX gtk-sharp-2.0 >= $GTKSHARP_REQUIRED glib-sharp-2.0 >= $GTKSHARP_REQUIRED glade-sharp-2.0 >= $GTKSHARP_REQUIRED gnome-vfs-sharp-2.0 >= $GTKSHARP_REQUIRED gtk+-2.0 >= $GTK_REQUIRED mono >= $MONO_REQUIRED mono-cairo >= $MONO_CAIRO_REQUIRED)
AC_SUBST(F_CFLAGS)
AC_SUBST(F_LIBS)

PKG_CHECK_MODULES(GIO, gio-2.0 >= $GIO_REQUIRED)
AC_SUBST(GIO_CFLAGS)
AC_SUBST(GIO_LIBS)

CSC_DEFINES=""
PKG_CHECK_MODULES(GNOME_SHARP, gnome-sharp-2.0 >= 2.8)

PKG_CHECK_MODULES(GLIBSHARP, glib-sharp-2.0 >= 2.12)

if pkg-config --atleast-version=1.2.5 mono-cairo; then
   CSC_DEFINES="$CSC_DEFINES -d:CAIRO_1_2_5"
fi

if pkg-config --atleast-version=2.13.0 gtk-sharp-2.0 && pkg-config --max-version=2.14 gtk-sharp-2.0; then
	AC_MSG_ERROR([There's a svn version of gtk-sharp 2.13.x installed. Uninstall it.])
fi

dnl - Choose PreferenceBackend (default to gconf)
AC_ARG_ENABLE([gconf],[AC_HELP_STRING([--disable-gconf], [build without gconf preference backend])],,)

AM_CONDITIONAL(NOGCONF, test "x$enable_gconf" = "xno")

if test "x$enable_gconf" = "xno"; then
  CSC_DEFINES="$CSC_DEFINES -d:NOGCONF"
else
  PKG_CHECK_MODULES(GCONF_SHARP, gconf-sharp-2.0 >= $GTKSHARP_REQUIRED)
  if pkg-config --atleast-version=2.18 gconf-sharp-2.0; then
    CSC_DEFINES="$CSC_DEFINES -d:GCONF_SHARP_2_18"
  fi
  if pkg-config --atleast-version=2.20.2 gconf-sharp-2.0; then
    CSC_DEFINES="$CSC_DEFINES -d:GCONF_SHARP_2_20_2"
  fi
fi


PKG_CHECK_MODULES(BEAGLE, beagle-0.0 >= $BEAGLE_REQUIRED,
[
	dnl this is a work around to avoid linking against beagles Util.dll
	BEAGLE_DIR=`$PKG_CONFIG --variable=dlldir beagle-0.0`	
	LINK_BEAGLE="-r:$BEAGLE_DIR/Beagle"
	CSC_DEFINES="$CSC_DEFINES -d:ENABLE_BEAGLE"
],
[
	AC_MSG_RESULT([beagle not found])
])
AC_SUBST(BEAGLE_DIR)
AC_SUBST(LINK_BEAGLE)


dnl -- dbus-sharp: link or bundle ?
PKG_CHECK_MODULES(NDESK_DBUS, ndesk-dbus-1.0 >= $NDESK_DBUS_REQUIRED ndesk-dbus-glib-1.0 >= $NDESK_DBUS_GLIB_REQUIRED,
[
	LINK_DBUS="-pkg:ndesk-dbus-1.0 -pkg:ndesk-dbus-glib-1.0"
	DIR_DBUS=""
	DIR_DBUS_GLIB=""
	PATH_DBUS=""
],
[
	LINK_DBUS='-r:$(top_builddir)/lib/dbus-sharp/NDesk.DBus.dll -r:$(top_builddir)/lib/dbus-sharp-glib/NDesk.DBus.GLib.dll'
	DIR_DBUS="dbus-sharp"
	DIR_DBUS_GLIB="dbus-sharp-glib"
	PATH_DBUS='../lib/dbus-sharp:../lib/dbus-sharp-glib:'
])
AC_SUBST(DIR_DBUS)
AC_SUBST(DIR_DBUS_GLIB)
AC_SUBST(LINK_DBUS)
AC_SUBST(PATH_DBUS)

dnl -- nunit
PKG_CHECK_MODULES(NUNIT, nunit >= $NUNIT_REQUIRED, 
	do_tests="yes", do_tests="no")

AC_SUBST(NUNIT_LIBS)
AM_CONDITIONAL(ENABLE_TESTS, test "x$do_tests" = "xyes")

if test "x$do_tests" = "xno"; then
	PKG_CHECK_MODULES(NUNIT, mono-nunit >= 2.0, 
		do_tests="yes", do_tests="no")
	
	AC_SUBST(NUNIT_LIBS)
	AM_CONDITIONAL(ENABLE_TESTS, test "x$do_tests" = "xyes")

	if test "x$do_tests" = "xno"; then
		NUNIT_DEFINES=''
		AC_MSG_WARN([Could not find nunit: tests will not be available.])
	else
		NUNIT_DEFINES='-d:ENABLE_NUNIT'
	fi
	AC_SUBST(NUNIT_DEFINES)
fi


dnl --- GConf

AC_PATH_PROG(GCONFTOOL, gconftool-2, no)
AM_GCONF_SOURCE_2


dnl --- libjpeg

AC_CHECK_LIB(jpeg, jpeg_start_decompress, [],
	     [AC_MSG_ERROR([libjpeg not found])], -lm)
AC_CHECK_HEADER(jpeglib.h, , [AC_MSG_ERROR([libjpeg not found])])
LIBJPEG='-ljpeg'

dnl --- lcms

PKG_CHECK_MODULES(LCMS, lcms >= $LCMS_REQUIRED, [], 
	[AC_CHECK_LIB(lcms, cmsCreateBCHSWabstractProfile, [],
		     [AC_MSG_ERROR([liblcms not found])], -lm)
	AC_CHECK_HEADER(lcms.h, , [AC_MSG_ERROR([liblcms not found])])
	LCMS_LIBS='-llcms'
	LCMS_CFLAGS=''])


dnl --- libgphoto2-sharp

PKG_CHECK_MODULES(LIBGPHOTO2, libgphoto2 >= 2.1.4)

if pkg-config --atleast-version=2.4 libgphoto2; then
	CSC_DEFINES="$CSC_DEFINES -d:GPHOTO2_2_4"
fi

AC_SUBST(CSC_DEFINES)

dnl --- libexif version check

LIBEXIF_API_CHANGE=0.6.0
PKG_CHECK_MODULES(EXIF, libexif >= $LIBEXIF_API_CHANGE)
AC_SUBST(EXIF_CFLAGS)
AC_SUBST(EXIF_LIBS)

	 
PKG_CHECK_MODULES(LIBEXIF_VERSION_CHECK, libexif >= 0.6.12, EXIF_SOVERSION=12, EXIF_SOVERSION=10)
AC_SUBST(EXIF_SOVERSION)

dnl --- Set up paths

AC_ARG_WITH(turtle, [ --with-turtle=DIR                set path to source directory for turtle])
AM_CONDITIONAL(WITH_TURTLE, test "x$with_turtle" != "xno")
if test "x$with_turtle" != "xno"; then
   TURTLEDIR=$with_turtle

   AC_SUBST(TURTLEDIR)
fi

AC_ARG_WITH(gnome-screensaver,[ --with-gnome-screensaver=PREFIX  set gnome gnome-screensaver prefix])

if test "x$with_gnome_screensaver" != "x"; then
	gnome_screensaver_prefix=$with_gnome_screensaver
else
	gnome_screensaver_prefix=$prefix		   
fi

PKG_CHECK_EXISTS(gnome-screensaver, have_gs_pkgconfig=yes, have_gs_pkgconfig=no)
if test "$have_gs_pkgconfig" = "yes"; then
	gs_pc_prefix=`$PKG_CONFIG --variable=prefix gnome-screensaver`
	gs_pc_saverdir=`$PKG_CONFIG --variable=privlibexecdir gnome-screensaver`
	gs_pc_themesdir=`$PKG_CONFIG --variable=themesdir gnome-screensaver`
fi

if test "$have_gs_pkgconfig" = "yes" && test "x$gs_pc_prefix" = "x$gnome_screensaver_prefix"; then
	GNOME_SCREENSAVER_SAVERDIR=$gs_pc_saverdir
elif test -d $gnome_screensaver_prefix/lib/gnome-screensaver; then
	GNOME_SCREENSAVER_SAVERDIR=$gnome_screensaver_prefix/lib/gnome-screensaver/gnome-screensaver
else 
	GNOME_SCREENSAVER_SAVERDIR=$gnome_screensaver_prefix/libexec/gnome-screensaver
fi

if test "$have_gs_pkgconfig" = "yes" && test "x$gs_pc_prefix" = "x$gnome_screensaver_prefix"; then
	GNOME_SCREENSAVER_THEMESDIR=$gs_pc_themesdir
else
	GNOME_SCREENSAVER_THEMESDIR=$gnome_screensaver_prefix/share/applications/screensavers
fi

AC_SUBST(GNOME_SCREENSAVER_SAVERDIR)
AC_SUBST(GNOME_SCREENSAVER_THEMESDIR)

ICONS_DIRECTORY=${datadir}/${PACKAGE}-${VERSION}

AC_SUBST(ICONS_DIRECTORY)

dnl -- Intl

GETTEXT_PACKAGE=f-spot
AC_SUBST(GETTEXT_PACKAGE)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE,"$GETTEXT_PACKAGE", [Gettext package])
AM_GLIB_GNU_GETTEXT

dnl --- Prologue

AC_SUBST(LIBJPEG)
AC_SUBST(LCMS_LIBS)
AC_SUBST(LCMS_CFLAGS)
AC_SUBST(CFLAGS)
AC_SUBST(CPPFLAGS)
AC_SUBST(LDFLAGS)

expanded_libdir=`( case $prefix in NONE) prefix=$ac_default_prefix ;; *) ;; esac
		   case $exec_prefix in NONE) exec_prefix=$prefix ;; *) ;; esac
		   eval echo $libdir )`
AC_SUBST(expanded_libdir)

AC_CONFIG_FILES([src/f-spot],[chmod +x src/f-spot])

GNOME_ICON_THEME_PREFIX=`$PKG_CONFIG --variable=prefix gnome-icon-theme`
AC_SUBST(GNOME_ICON_THEME_PREFIX)

AC_OUTPUT(
Makefile
lib/dbus-sharp/Makefile
lib/dbus-sharp-glib/Makefile
lib/dpap-sharp/Makefile
lib/dpap-sharp/lib/Makefile
lib/dpap-sharp/dpap-client/Makefile
lib/dpap-sharp/dpap-server/Makefile
lib/dpap-sharp/DPAPService/Makefile
lib/dpap-sharp/DPAPBrowser/Makefile
lib/GKeyFile/Makefile
lib/gio-sharp/Makefile
lib/gio-sharp/gio/Makefile
lib/gio-sharp/generator/Makefile
lib/glitz-sharp/Makefile
lib/glitz-sharp/src/Makefile
lib/gnome-keyring-sharp/Makefile
lib/gtk-sharp-beans/Makefile
lib/libfspot/Makefile
lib/libgphoto2-sharp/Makefile
lib/libjpegtran/Makefile
lib/Makefile
lib/mono-addins/Makefile
lib/mono-addins/Mono.Addins.Gui/Makefile
lib/mono-addins/Mono.Addins/Makefile
lib/mono-addins/Mono.Addins.Setup/Makefile
lib/semweb/Makefile
docs/Makefile
icons/Makefile
tools/Makefile
po/Makefile.in
src/Core/Defines.cs
src/AssemblyInfo.cs
src/f-spot.exe.config
src/Cms.dll.config
src/FSpot.Widgets.dll.config
src/Makefile
extensions/Makefile
extensions/Exporters/Makefile
extensions/Exporters/CDExport/Makefile
extensions/Exporters/DefaultExporters/Makefile
extensions/Exporters/FlickrExport/Makefile
extensions/Exporters/FlickrExport/FlickrNet/Makefile
extensions/Exporters/GalleryExport/Makefile
extensions/Exporters/FacebookExport/Makefile
extensions/Exporters/FolderExport/Makefile
extensions/Exporters/SmugMugExport/SmugMugNet/Makefile
extensions/Exporters/SmugMugExport/Makefile
extensions/Exporters/TabbloExport/Makefile
extensions/Exporters/TabbloExport/Tabblo/Makefile
extensions/Exporters/PicasaWebExport/Makefile
extensions/Exporters/PicasaWebExport/google-sharp/Makefile
extensions/Exporters/ZipExport/Makefile
extensions/Services/Makefile
extensions/Services/BeagleService/Makefile
extensions/Services/DBusService/Makefile
extensions/Tools/Makefile
extensions/Tools/RawPlusJpeg/Makefile
extensions/Tools/ChangePhotoPath/Makefile
extensions/Tools/HashJob/Makefile
extensions/Tools/DevelopInUFraw/Makefile
extensions/Tools/MergeDb/Makefile
extensions/Tools/ScreensaverConfig/Makefile
extensions/Transitions/Makefile
extensions/Transitions/CoverTransition/Makefile
tests/Makefile
tests/src/Makefile
f-spot.pc
f-spot.spec
f-spot.desktop.in
f-spot-view.desktop.in
f-spot-import.desktop.in
)
