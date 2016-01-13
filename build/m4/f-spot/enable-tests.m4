AC_DEFUN([FSPOT_ENABLE_TESTS],
[
	AC_ARG_ENABLE(tests, AC_HELP_STRING([--enable-tests], [Enable unit tests based on NUnit and Moq via nuget]),
		enable_tests=$enableval, enable_tests="no")

	if test "x$enable_tests" = "xno"; then
		do_tests=no
	else
		SHAMROCK_FIND_PROGRAM(NUGET, nuget, no)
		if test "x$NUGET" = "xno"; then
			do_tests=no
			AC_MSG_WARN([You need to install nuget: tests will not be available])
		else
			do_tests=yes
		fi
	fi
	if test "x$do_tests" = "xyes"; then
		AM_CONDITIONAL(ENABLE_TESTS, true)
		SKIP_UNIT_TEST_PROJECTS="False"
	else
		AM_CONDITIONAL(ENABLE_TESTS, false)
		SKIP_UNIT_TEST_PROJECTS="True"
	fi
	AC_SUBST(SKIP_UNIT_TEST_PROJECTS)
])
