dnl Granularly define the version components
dnl Remember to also update the Display Version
m4_define([fspot_version_major], [0])
m4_define([fspot_version_minor], [6])
m4_define([fspot_version_micro], [2])

dnl set to 0 when doing an official release
m4_define([fspot_version_pre_release], [0])

m4_define([concat], $1$2$3$4)

dnl create base version string
m4_define([fspot_version], concat(
	fspot_version_major.,
	fspot_version_minor.,
	fspot_version_micro))

dnl create the release version
m4_define([fspot_version],
	m4_if(fspot_version_pre_release, [0],
		fspot_version,
		concat(fspot_version, fspot_version_pre_release)))

dnl this can sometimes differ manually
m4_define([fspot_display_version],
	["0.6.2"])

m4_define([fspot_api_version],
	[fspot_version_major.fspot_version_minor])

m4_define([fspot_asm_version],
	[fspot_api_version.0.0])

AC_PREREQ(2.52)
AC_INIT([f-spot], fspot_version,
	[http://bugzilla.gnome.org/enter_bug.cgi?product=f-spot])

AM_INIT_AUTOMAKE([1.9 dist-bzip2 tar-ustar dist-zip foreign])
AM_MAINTAINER_MODE
AM_CONFIG_HEADER(config.h)
AC_SUBST([ACLOCAL_AMFLAGS], ["-I build/m4/shamrock -I build/m4/shave \${ACLOCAL_FLAGS}"])

IT_PROG_INTLTOOL([0.35.0])
AC_PROG_LIBTOOL
AC_PROG_INSTALL

dnl How to configure for distcheck
DISTCHECK_CONFIGURE_FLAGS="--disable-docs --enable-release"
AC_SUBST(DISTCHECK_CONFIGURE_FLAGS)

dnl Export Version Info
AC_SUBST([API_VERSION], [fspot_api_version])
AC_SUBST([ASM_VERSION], [fspot_asm_version])
AC_SUBST([DISPLAY_VERSION], [fspot_display_version])

dnl Expansions
SHAMROCK_EXPAND_LIBDIR
SHAMROCK_EXPAND_BINDIR
SHAMROCK_EXPAND_DATADIR


dnl Mono and gmcs
SHAMROCK_CHECK_MONO_MODULE(2.2)
SHAMROCK_FIND_MONO_2_0_COMPILER
SHAMROCK_FIND_MONO_RUNTIME
SHAMROCK_CHECK_MONO_2_0_GAC_ASSEMBLIES([
    Mono.Data.SqliteClient
    Mono.Posix
    System.Runtime.Remoting
    System.Web
    System.Web.Services
    Mono.Cairo
])







AC_CONFIG_SRCDIR(src/main.cs)

AC_PROG_CC
AC_PROG_CXX
AC_ISC_POSIX
AC_HEADER_STDC

GNOME_COMPILE_WARNINGS
AM_PATH_GLIB_2_0

dnl --- Check for mono and gmcs

AC_PATH_PROG(MONO, mono)
AC_PATH_PROG(MCS, gmcs)

CS="C#"
if test ! -x "$MCS"; then
	AC_MSG_ERROR([No $CS compiler found])
fi

dnl --- Check for gapi programs

AC_PATH_PROG(GAPI2FIXUP, gapi2-fixup, no)

if test ! -x "$GAPI2FIXUP"; then
	AC_MSG_ERROR([No gapi post-processor found])
fi

AC_PATH_PROG(GAPI2PARSER, gapi2-parser, no)

if test ! -x "$GAPI2PARSER"; then
	AC_MSG_ERROR([No gapi parser found])
fi

AC_PATH_PROG(GAPI2CODEGEN, gapi2-codegen, no)

if test ! -x "$GAPI2CODEGEN"; then
	AC_MSG_ERROR([No gapi code generator found])
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

dnl --- Required libraries

LIBGNOME_REQUIRED=2.2
LIBGNOMEUI_REQUIRED=2.2
LIBEXIF_REQUIRED_MIN=0.5.7
LIBEXIF_REQUIRED_MAX=0.7.0
GIO_REQUIRED=2.16.0
GIOSHARP_REQUIRED=2.13.92
GTKSHARPBEANS_REQUIRED=2.13.92
GTKSHARP_REQUIRED=2.12.2
GTK_REQUIRED=2.14
NDESK_DBUS_REQUIRED=0.4.2
NDESK_DBUS_GLIB_REQUIRED=0.3.0
MONO_CAIRO_REQUIRED=1.2.4
CAIRO_REQUIRED=1.4.0
LCMS_REQUIRED=1.12
LIBGPHOTO2_REQUIRED=2.4
MONOADDINS_REQUIRED=0.3

PKG_CHECK_MODULES(F, libgnome-2.0 >= $LIBGNOME_REQUIRED libgnomeui-2.0 >= $LIBGNOMEUI_REQUIRED libexif >= $LIBEXIF_REQUIRED_MIN libexif < $LIBEXIF_REQUIRED_MAX gtk-sharp-2.0 >= $GTKSHARP_REQUIRED glib-sharp-2.0 >= $GTKSHARP_REQUIRED glade-sharp-2.0 >= $GTKSHARP_REQUIRED gnome-vfs-sharp-2.0 >= $GTKSHARP_REQUIRED gtk+-2.0 >= $GTK_REQUIRED mono-cairo >= $MONO_CAIRO_REQUIRED cairo >= $CAIRO_REQUIRED mono-addins >= $MONOADDINS_REQUIRED mono-addins-gui >= $MONOADDINS_REQUIRED mono-addins-setup >= $MONOADDINS_REQUIRED)
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
  AC_PATH_PROG(GCONFTOOL, gconftool-2)
  AM_GCONF_SOURCE_2
  PKG_CHECK_MODULES(GCONF_SHARP, gconf-sharp-2.0 >= $GTKSHARP_REQUIRED)
  if pkg-config --atleast-version=2.18 gconf-sharp-2.0; then
    CSC_DEFINES="$CSC_DEFINES -d:GCONF_SHARP_2_18"
  fi
  if pkg-config --atleast-version=2.20.2 gconf-sharp-2.0; then
    CSC_DEFINES="$CSC_DEFINES -d:GCONF_SHARP_2_20_2"
  fi
fi

if pkg-config --at-least-version=2.16 gtk+-2.0; then
	CSC_DEFINES="$CSC_DEFINES -d:GTK_2_16"
fi

dnl -- dbus-sharp
PKG_CHECK_MODULES(NDESK_DBUS, ndesk-dbus-1.0 >= $NDESK_DBUS_REQUIRED ndesk-dbus-glib-1.0 >= $NDESK_DBUS_GLIB_REQUIRED)
AC_SUBST(NDESK_DBUS_LIBS)

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


dnl --- libgphoto2

PKG_CHECK_MODULES(LIBGPHOTO2, libgphoto2 >= $LIBGPHOTO2_REQUIRED)

AC_SUBST(CSC_DEFINES)

dnl --- libexif version check

LIBEXIF_API_CHANGE=0.6.0
PKG_CHECK_MODULES(EXIF, libexif >= $LIBEXIF_API_CHANGE)
AC_SUBST(EXIF_CFLAGS)
AC_SUBST(EXIF_LIBS)

	 
PKG_CHECK_MODULES(LIBEXIF_VERSION_CHECK, libexif >= 0.6.12, EXIF_SOVERSION=12, EXIF_SOVERSION=10)
AC_SUBST(EXIF_SOVERSION)

dnl --- libunique check
PKG_CHECK_MODULES(UNIQUE, unique-1.0 >= 1.0)

dnl --- Set up paths

AC_ARG_WITH(turtle, [ --with-turtle=DIR                set path to source directory for turtle])
AM_CONDITIONAL(WITH_TURTLE, test "x$with_turtle" != "xno")
if test "x$with_turtle" != "xno"; then
   TURTLEDIR=$with_turtle

   AC_SUBST(TURTLEDIR)
fi

AC_ARG_WITH(gnome-screensaver,
    [ --with-gnome-screensaver=PREFIX  set gnome gnome-screensaver prefix],
    gnome_screensaver_prefix=$with_gnome_screensaver,
    gnome_screensaver_prefix=$prefix)

AC_ARG_WITH(gnome-screensaver-privlibexecdir,
    [ --with-gnome-screensaver-privlibexecdir=DIR  set gnome-screensaver privlibexecdir],
    GNOME_SCREENSAVER_SAVERDIR=$with_gnome_screensaver_privlibexecdir,
    if test -d $gnome_screensaver_prefix/lib/gnome-screensaver; then
        GNOME_SCREENSAVER_SAVERDIR=$gnome_screensaver_prefix/lib/gnome-screensaver
    else
        GNOME_SCREENSAVER_SAVERDIR=$gnome_screensaver_prefix/libexec/gnome-screensaver
    fi)

AC_ARG_WITH(gnome-screensaver-themesdir,
    [ --with-gnome-screensaver-themesdir=DIR  set gnome-screensaver themesdir],
    GNOME_SCREENSAVER_THEMESDIR=$with_gnome_screensaver_themesdir,
    GNOME_SCREENSAVER_THEMESDIR=$gnome_screensaver_prefix/share/applications/screensavers
    )

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

AC_CONFIG_FILES([src/f-spot],[chmod +x src/f-spot])

GNOME_ICON_THEME_PREFIX=`$PKG_CONFIG --variable=prefix gnome-icon-theme`
AC_SUBST(GNOME_ICON_THEME_PREFIX)

SHAVE_INIT([build/m4/shave], [enable])

AC_OUTPUT(
Makefile

build/Makefile
build/m4/Makefile
build/m4/shave/shave
build/m4/shave/shave-libtool

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
lib/gnome-keyring-sharp/Makefile
lib/gtk-sharp-beans/Makefile
lib/libfspot/Makefile
lib/libgphoto2-sharp/Makefile
lib/libgphoto2-sharp/libgphoto2-sharp.dll.config
lib/libgphoto2-sharp/glue/Makefile
lib/libjpegtran/Makefile
lib/Makefile
lib/semweb/Makefile
lib/unique-sharp/Makefile
lib/unique-sharp/generator/Makefile
lib/unique-sharp/unique/Makefile
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
extensions/Editors/Makefile
extensions/Editors/BlackoutEditor/Makefile
extensions/Editors/BWEditor/Makefile
extensions/Editors/FlipEditor/Makefile
extensions/Editors/PixelateEditor/Makefile
extensions/Editors/ResizeEditor/Makefile
extensions/Exporters/Makefile
extensions/Exporters/CDExport/Makefile
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
extensions/Services/DBusService/Makefile
extensions/Tools/Makefile
extensions/Tools/RawPlusJpeg/Makefile
extensions/Tools/ChangePhotoPath/Makefile
extensions/Tools/HashJob/Makefile
extensions/Tools/DevelopInUFraw/Makefile
extensions/Tools/LiveWebGallery/Makefile
extensions/Tools/MergeDb/Makefile
extensions/Tools/RetroactiveRoll/Makefile
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
