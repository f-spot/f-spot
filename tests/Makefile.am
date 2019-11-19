include $(top_srcdir)/build/build.environment.mk

EXTRA_DIST = \
	data/f-spot-0.6.1.5.db    \
	data/f-spot-0.6.2.db      \
	data/f-spot-0.7.0-17.2.db \
	data/f-spot-0.7.0-18.0.db

if ENABLE_TESTS

TEST_ASSEMBLIES = *.UnitTest.dll

# get NUnit console runner from nuget package
NUNIT_CONSOLE = $$(echo ../packages/NUnit.ConsoleRunner.*/tools/nunit3-console.exe)

test:
	@chmod +x $(NUNIT_CONSOLE); \
	$(NUNIT_CONSOLE) --labels=ON $(TEST_ASSEMBLIES);

endif

MAINTAINERCLEANFILES = Makefile.in
