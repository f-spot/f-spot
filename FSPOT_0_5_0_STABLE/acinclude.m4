### F_CHECK_BERKELEYDB
###
### Check for the right version of the Berkeley DB library.

AC_DEFUN(F_CHECK_BERKELEYDB, [

	AC_ARG_WITH(db4,          [  --with-db4=PREFIX              Location of db4],
		[with_db4_includes="$withval/include"
		 with_db4_libs="$withval/lib"])
	AC_ARG_WITH(db4-includes, [  --with-db4-includes=PATH       Location of db4 includes],
		with_db4_includes="$withval")
	AC_ARG_WITH(db4-libs,     [  --with-db4-libs=PATH           Location of db4 libs],
		with_db4_libs="$withval")
	
	if test -z "$with_db4_libs"; then
		with_db4_libs="/usr/lib"
	fi
	
	AC_CACHE_CHECK([for db4 compiler flags], ac_cv_db4_cflags,
	[
		if test -n "${with_db4_includes}"; then
			ac_cv_db4_cflags="-I$with_db4_includes"
		fi
	])
	DB4_CFLAGS=$ac_cv_db4_cflags
	AC_SUBST(DB4_CFLAGS)
	
	CPPFLAGS_save="$CPPFLAGS"
	CPPFLAGS="$DB4_CFLAGS $CPPFLAGS"
	AC_CHECK_HEADERS(db.h db4/db.h, break)
	
	AC_CACHE_CHECK([db4 header version], ac_cv_db4_header_version,
	[
		AC_TRY_COMPILE([
			#ifdef HAVE_DB4_DB_H
			#include <db4/db.h>
			#else
			#include <db.h>
			#endif
		],[
			#if DB_VERSION_MAJOR != 4
			#error
			#endif
		], :, AC_MSG_ERROR(Found db.h is not version 4))
	
		ac_cv_db4_header_version=4
	])

	AC_CACHE_CHECK([for db4 library name], ac_cv_db4_ldadd,
	[
		LIBS_save="$LIBS"
		ac_cv_db4_ldadd=""
	
		for name in db db4 db-3.1; do
			LIBS="$LIBS_save $with_db4_libs/lib${name}.a"
			AC_TRY_LINK([
				#ifdef HAVE_DB4_DB_H
				#include <db4/db.h>
				#else
				#include <db.h>
				#endif
			],[
				DB *db;
				db_create (&db, 0, 0);
			], [
				ac_cv_db4_ldadd="$with_db4_libs/lib${name}.a"
				break
			])
		done
		LIBS="$LIBS_save"
	
		if test -z "$ac_cv_db4_ldadd"; then
			AC_MSG_ERROR(Could not find db4 library)
		fi
	])
	DB4_LDADD=$ac_cv_db4_ldadd
	AC_SUBST(DB4_LDADD)
	
	AC_CACHE_CHECK([that db4 library version matches header version], ac_cv_db4_lib_version_match,
	[
		LIBS="$DB4_LDADD $LIBS"
		AC_TRY_RUN([
			#ifdef HAVE_DB4_DB_H
			#include <db4/db.h>
			#else
			#include <db.h>
			#endif
	
			int
			main (void)
			{
				int major, minor, patch;
	
				db_version (&major, &minor, &patch);
				return !(major == DB_VERSION_MAJOR &&
					 minor == DB_VERSION_MINOR &&
					 patch == DB_VERSION_PATCH);
			}
		], ac_cv_db4_lib_version_match=yes, ac_cv_db4_lib_version_match=no,
		ac_cv_db4_lib_version_match=yes)
	])
	if test "$ac_cv_db4_lib_version_match" = no; then
		AC_MSG_ERROR(db4 headers and library do not match... multiple copies installed?)
	fi
	
	CPPFLAGS="$CPPFLAGS_save"
	LIBS="$LIBS_save"

])
