include $(top_srcdir)/build/build.environment.mk

EXTRA_DIST = \
	data/f-spot-0.6.1.5.db \
	data/f-spot-0.6.2.db \
	data/f-spot-0.7.0-17.2.db \
	data/f-spot-0.7.0-18.0.db \
	data/taglib-sample.jpg \
	data/taglib-sample.xmp

if ENABLE_TESTS

TEST_ASSEMBLIES = \
	f-spot.exe \
	FSpot.Query.dll \
	FSpot.Utils.dll

ENV_OPTIONS = TZ=America/Chicago LC_ALL=it_IT LANG=it_IT
NUNIT_CONSOLE = $$(echo $$(which nunit-console2 || which nunit-console))
RUNNER = for asm in $${TEST_ASSEMBLIES}; do echo -e "\033[1mRunning tests on $${asm}...\033[0m"; $(ENV_OPTIONS) $(NUNIT_CONSOLE) -nologo -noshadow $$asm; done

test:
	@pushd $(DIR_BIN) &>/dev/null; \
	export MONO_PATH=../lib/gio-sharp/gio:../lib/gtk-sharp-beans:../lib/unique-sharp/unique:../bin:$$MONO_PATH; \
	export TEST_ASSEMBLIES="$(TEST_ASSEMBLIES)"; $(RUNNER); \
	popd &>/dev/null;

%:
	@pushd $(DIR_BIN) &>/dev/null; \
	for i in *.dll; do \
		if [[ $$(echo "$$i" | tr "[:upper:]" "[:lower:]") = "$$(echo "$@" | tr "[:upper:]" "[:lower:]").dll" ]]; then \
			export TEST_ASSEMBLIES="$$i"; $(RUNNER); \
		fi; \
	done;

endif

MAINTAINERCLEANFILES = Makefile.in

