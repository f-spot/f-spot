dnl Granularly define the version components
dnl Remember to also update the Display Version
m4_define([fspot_version_major], [2])
m4_define([fspot_version_minor], [9])
m4_define([fspot_version_micro], [0])

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
	["2.9.0"])

m4_define([fspot_api_version],
	[fspot_version_major.fspot_version_minor])

m4_define([fspot_asm_version],
	[fspot_api_version.0.0])

AC_PREREQ(2.52)
AC_INIT([f-spot], fspot_version,
	[http://bugzilla.gnome.org/enter_bug.cgi?product=f-spot])

AM_INIT_AUTOMAKE([1.9 dist-bzip2 tar-ustar dist-zip foreign])
AM_MAINTAINER_MODE
AC_CONFIG_HEADERS([config.h])
AC_CONFIG_MACRO_DIR([build/m4])
AC_SUBST([ACLOCAL_AMFLAGS], ["-I build/m4/shamrock -I build/m4/f-spot \${ACLOCAL_FLAGS}"])

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
SHAMROCK_CHECK_MONO_MODULE(3.2.0)
SHAMROCK_FIND_MONO_2_0_COMPILER
SHAMROCK_FIND_MONO_RUNTIME
SHAMROCK_CHECK_MONO_2_0_GAC_ASSEMBLIES([
    Mono.Posix
    Mono.Simd
    System
    System.Runtime.Remoting
    System.Web
    System.Web.Services
    System.Data
    Mono.Cairo
    ICSharpCode.SharpZipLib
])

FSPOT_CHECK_LIBFSPOT
dnl --- Checks for gtk-sharp and glib-sharp
FSPOT_CHECK_GTK_SHARP
FSPOT_CHECK_GNOME_KEYRING_SHARP
FSPOT_CHECK_FLICKRNET
SHAMROCK_CHECK_NUNIT
FSPOT_CHECK_DBUS_SHARP



# ---------------------------------- <Old build stuff>
dnl --- Check for mono and gmcs

AC_PATH_PROG(MONO, mono)
AC_PATH_PROG(MCS, gmcs)

CS="C#"
if test ! -x "$MCS"; then
	AC_MSG_ERROR([No $CS compiler found])
fi

dnl -- Check for mono pc file

AC_MSG_CHECKING([for mono.pc])
if test -z `$PKG_CONFIG --variable=prefix mono`; then
  AC_MSG_ERROR([missing the mono.pc file, usually found in the mono-devel package])
else
  AC_MSG_RESULT([found])
fi

dnl --- Required libraries

#LIBGNOMEDESKTOP_REQUIRED=2.17
GIO_REQUIRED=3.0
GTK_REQUIRED=3.0
#MONO_CAIRO_REQUIRED=1.2.5
#CAIRO_REQUIRED=1.4.0
LCMS2_REQUIRED=2.4

PKG_CHECK_MODULES(F, gtk+-3.0 >= $GTK_REQUIRED)
AC_SUBST(F_CFLAGS)
AC_SUBST(F_LIBS)

#PKG_CHECK_MODULES(GIO, gio-2.0 >= $GIO_REQUIRED)
#AC_SUBST(GIO_CFLAGS)
#AC_SUBST(GIO_LIBS)

CSC_DEFINES=""
PKG_CHECK_MODULES(GLIBSHARP, glib-sharp-3.0 >= 2.99.1)

AC_SUBST(CSC_DEFINES)

dnl --- lcms
PKG_CHECK_MODULES(LCMS2, lcms2 >= $LCMS2_REQUIRED, [], 
	[AC_CHECK_LIB(lcms2, cmsCreateBCHSWabstractProfile, [],
		     [AC_MSG_ERROR([liblcms2 not found])], -lm)
	AC_CHECK_HEADER(lcms2.h, , [AC_MSG_ERROR([liblcms2 not found])])
	LCMS2_LIBS='-llcms2'
	LCMS2_CFLAGS=''])

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

AC_SUBST(LCMS2_LIBS)
AC_SUBST(LCMS2_CFLAGS)
AC_SUBST(CFLAGS)
AC_SUBST(CPPFLAGS)
AC_SUBST(LDFLAGS)

AC_CONFIG_FILES([src/Clients/MainApp/f-spot],[chmod +x src/Clients/MainApp/f-spot])

GNOME_ICON_THEME_PREFIX=`$PKG_CONFIG --variable=prefix gnome-icon-theme`
AC_SUBST(GNOME_ICON_THEME_PREFIX)

# ---------------------------------- </Old build stuff>

AC_ARG_ENABLE(release,
	AC_HELP_STRING([--enable-release],
		[Use release configuration]),
	enable_release=yes, enable_release=`(test ! -d .svn && test ! -d .git) && echo "yes" || echo "no"`)
AM_CONDITIONAL(ENABLE_RELEASE, test "x$enable_release" = "xyes")
ENABLE_RELEASE=$enable_release
AC_SUBST(ENABLE_RELEASE)
if test "x$enable_release" = "xyes"; then
	GMCS_FLAGS="-define:RELEASE -sdk:4"
# FIXME: Enable as soon as we rock sufficiently
#else
#	GMCS_FLAGS="-warnaserror"
fi
AC_SUBST(GMCS_FLAGS)

AM_CONDITIONAL(HYENA_PKG_CONFIG, false)

AC_ARG_WITH(vendor-build-id,
	AC_HELP_STRING([--with-vendor-build-id=<vendor-build-id>],
		[Set a vendor build ID, recommended for packagers]),
	[vendor_build_id="$withval"], [
		test -d .git \
			&& vendor_build_id="git-checkout" \
			|| vendor_build_id="source-tarball"
	])
BUILD_VENDOR_ID="$vendor_build_id"
BUILD_HOST_OS="$host_os"
BUILD_HOST_CPU="$host_cpu"
BUILD_TIME=`date +"%F %T %Z"`
AC_SUBST(BUILD_VENDOR_ID)
AC_SUBST(BUILD_HOST_OS)
AC_SUBST(BUILD_HOST_CPU)
AC_SUBST(BUILD_TIME)

m4_ifdef([AM_SILENT_RULES], [AM_SILENT_RULES([yes])])

dnl generated files
AC_OUTPUT([
Makefile

build/Makefile
build/m4/Makefile
build/pkg-config/Makefile

data/Makefile
data/desktop-files/Makefile
data/desktop-files/f-spot.desktop.in
data/desktop-files/f-spot-view.desktop.in
data/desktop-files/f-spot-import.desktop.in

help/Makefile

external/Hyena/build/m4/Makefile
external/Hyena/build/Makefile
external/Hyena/build/pkg-config/Makefile
external/Hyena/Hyena.Data.Sqlite/Makefile
external/Hyena/Hyena.Gui/Makefile
external/Hyena/Hyena/Makefile
external/Hyena/Makefile
external/Makefile
lib/libfspot/Makefile
lib/Makefile

src/AssemblyInfo.cs

src/Makefile
src/Core/Makefile
src/Core/FSpot.Bling/Makefile
src/Core/FSpot.Cms/Makefile
src/Core/FSpot.Cms/FSpot.Cms.dll.config
src/Core/FSpot.Core/Makefile
src/Core/FSpot.Core/FSpot.Core.dll.config
src/Core/FSpot.Core/FSpot.Core/Defines.cs
src/Core/FSpot.JobScheduler/Makefile
src/Core/FSpot.Utils/Makefile
src/Core/FSpot.Utils/FSpot.Utils.dll.config
src/Core/FSpot.Query/Makefile
src/Core/FSpot.Gui/Makefile
src/Core/FSpot.Gui/FSpot.Gui.dll.config
src/Core/FSpot.Platform/Makefile

src/Clients/Makefile
src/Clients/MainApp/Makefile
src/Clients/MainApp/f-spot.exe.config

src/Extensions/Makefile
src/Extensions/Editors/Makefile
src/Extensions/Editors/FSpot.Editors.Blackout/Makefile
src/Extensions/Editors/FSpot.Editors.BW/Makefile
src/Extensions/Editors/FSpot.Editors.Flip/Makefile
src/Extensions/Editors/FSpot.Editors.Pixelate/Makefile
src/Extensions/Editors/FSpot.Editors.Resize/Makefile
src/Extensions/Exporters/Makefile
src/Extensions/Exporters/FSpot.Exporters.CD/Makefile
src/Extensions/Exporters/FSpot.Exporters.Flickr/Makefile
src/Extensions/Exporters/FSpot.Exporters.Gallery/Makefile
src/Extensions/Exporters/FSpot.Exporters.Facebook/Makefile
src/Extensions/Exporters/FSpot.Exporters.Folder/Makefile
src/Extensions/Exporters/FSpot.Exporters.SmugMug/Makefile
src/Extensions/Exporters/FSpot.Exporters.Tabblo/Makefile
src/Extensions/Exporters/FSpot.Exporters.PicasaWeb/Makefile
src/Extensions/Exporters/FSpot.Exporters.Zip/Makefile
src/Extensions/Tools/Makefile
src/Extensions/Tools/FSpot.Tools.RawPlusJpeg/Makefile
src/Extensions/Tools/FSpot.Tools.ChangePhotoPath/Makefile
src/Extensions/Tools/FSpot.Tools.DevelopInUFraw/Makefile
src/Extensions/Tools/FSpot.Tools.LiveWebGallery/Makefile
src/Extensions/Tools/FSpot.Tools.MergeDb/Makefile
src/Extensions/Tools/FSpot.Tools.RetroactiveRoll/Makefile
src/Extensions/Tools/FSpot.Tools.ScreensaverConfig/Makefile
src/Extensions/Transitions/Makefile
src/Extensions/Transitions/FSpot.Transitions.Cover/Makefile
src/Extensions/Transitions/FSpot.Transitions.Dissolve/Makefile
src/Extensions/Transitions/FSpot.Transitions.Push/Makefile

docs/Makefile
icons/Makefile
tools/Makefile
po/Makefile.in
tests/Makefile
])

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

if test -d ${expanded_libdir}/${PACKAGE}; then
	if test x$br = xyes; then echo; fi
	echo "  WARNING: An existing F-Spot install is in ${expanded_libdir}/${PACKAGE}"
	echo "           Remove the existing install before installing this build."
	echo "           Installing over an existing install will cause conflicts!"
	echo 
fi

