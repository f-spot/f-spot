dnl Granularly define the version components
dnl Remember to also update the Display Version
m4_define([fspot_version_major], [0])
m4_define([fspot_version_minor], [7])
m4_define([fspot_version_micro], [1])

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
	["0.7.1"])

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
AC_SUBST([ACLOCAL_AMFLAGS], ["-I build/m4/f-spot -I build/m4/shamrock -I build/m4/shave \${ACLOCAL_FLAGS}"])

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

dnl User help
SHAMROCK_CHECK_GNOME_DOC_UTILS(0.17.3)

dnl Mono and gmcs
SHAMROCK_CHECK_MONO_MODULE(2.2)
SHAMROCK_FIND_MONO_2_0_COMPILER
SHAMROCK_FIND_MONO_RUNTIME
SHAMROCK_CHECK_MONO_2_0_GAC_ASSEMBLIES([
    Mono.Data.SqliteClient
    Mono.Posix
    Mono.Simd
    Mono.Data
    System
    System.Runtime.Remoting
    System.Web
    System.Web.Services
    System.Data
    Mono.Cairo
    ICSharpCode.SharpZipLib
])

FSPOT_CHECK_GTK_SHARP
FSPOT_CHECK_GNOME_KEYRING_SHARP
FSPOT_CHECK_FLICKRNET
SHAMROCK_CHECK_NUNIT



AM_CONDITIONAL(HYENA_PKG_CONFIG, false)


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
GIO_REQUIRED=2.22.0
GTKSHARPBEANS_REQUIRED=2.13.92
GTKSHARP_REQUIRED=2.12.2
GTK_REQUIRED=2.14
NDESK_DBUS_REQUIRED=0.4.2
NDESK_DBUS_GLIB_REQUIRED=0.3.0
MONO_CAIRO_REQUIRED=1.2.5
CAIRO_REQUIRED=1.4.0
LCMS_REQUIRED=1.12
MONOADDINS_REQUIRED=0.3

PKG_CHECK_MODULES(F, libgnome-2.0 >= $LIBGNOME_REQUIRED libgnomeui-2.0 >= $LIBGNOMEUI_REQUIRED glade-sharp-2.0 >= $GTKSHARP_REQUIRED gtk+-2.0 >= $GTK_REQUIRED mono-cairo >= $MONO_CAIRO_REQUIRED cairo >= $CAIRO_REQUIRED mono-addins >= $MONOADDINS_REQUIRED mono-addins-gui >= $MONOADDINS_REQUIRED mono-addins-setup >= $MONOADDINS_REQUIRED)
AC_SUBST(F_CFLAGS)
AC_SUBST(F_LIBS)

PKG_CHECK_MODULES(GIO, gio-2.0 >= $GIO_REQUIRED)
AC_SUBST(GIO_CFLAGS)
AC_SUBST(GIO_LIBS)

CSC_DEFINES=""
PKG_CHECK_MODULES(GNOME_SHARP, gnome-sharp-2.0 >= 2.8)

PKG_CHECK_MODULES(GLIBSHARP, glib-sharp-2.0 >= 2.12)

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

if pkg-config --atleast-version=2.16 gtk+-2.0; then
	CSC_DEFINES="$CSC_DEFINES -d:GTK_2_16"
fi

AC_SUBST(CSC_DEFINES)

dnl Mono.Addins libraries
FSPOT_CHECK_MONO_ADDINS

dnl -- dbus-sharp
PKG_CHECK_MODULES(NDESK_DBUS, ndesk-dbus-1.0 >= $NDESK_DBUS_REQUIRED ndesk-dbus-glib-1.0 >= $NDESK_DBUS_GLIB_REQUIRED)
AC_SUBST(NDESK_DBUS_LIBS)

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
build/pkg-config/Makefile

data/Makefile
data/desktop-files/Makefile
data/desktop-files/f-spot.desktop.in
data/desktop-files/f-spot-view.desktop.in
data/desktop-files/f-spot-import.desktop.in

help/Makefile

lib/GKeyFile/Makefile
lib/gio-sharp/Makefile
lib/gio-sharp/gio/Makefile
lib/gio-sharp/generator/Makefile
lib/gtk-sharp-beans/Makefile
lib/libfspot/Makefile
lib/Makefile
lib/unique-sharp/Makefile
lib/unique-sharp/generator/Makefile
lib/unique-sharp/unique/Makefile
lib/Hyena/Makefile
lib/Hyena/build/Makefile
lib/Hyena/build/pkg-config/Makefile
lib/Hyena/build/m4/Makefile
lib/Hyena/Hyena/Makefile
lib/Hyena/Mono.Data.Sqlite/Makefile
lib/Hyena/Hyena.Data.Sqlite/Makefile
lib/Hyena/Hyena.Gui/Makefile
lib/TagLib/Makefile

docs/Makefile
icons/Makefile
tools/Makefile
po/Makefile.in
src/Cms/FSpot.Cms.dll.config
src/Cms/Makefile
src/Utils/Makefile
src/Bling/Makefile
src/JobScheduler/Makefile
src/Query/Makefile
src/Core/Defines.cs
src/Core/Makefile
src/Core/FSpot.Core.dll.config
src/Widgets/Makefile
src/Widgets/FSpot.Widgets.dll.config
src/Platform/Makefile
src/AssemblyInfo.cs
src/f-spot.exe.config
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
)

cat <<EOF

${PACKAGE}-${VERSION}

  Build Environment
    Install Prefix:    ${prefix}
    Datadir:           ${expanded_datadir}
    Libdir:            ${expanded_libdir}

    C Compiler:        ${CC}
    Mono C# Compiler:  ${MCS} ${GMCS_FLAGS}
    Mono Runtime:      ${MONO}

  Build/Development:
    Unit Tests:        ${do_tests}
    User Help:         ${enable_user_help} (requires gnome-doc-utils >= 0.17.3)

Run 'make' to build F-Spot.

EOF
