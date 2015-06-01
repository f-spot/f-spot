SUBDIRS = desktop-files

$(top_srcdir)/build/addin-xml-string-extractor.exe:
	@pushd $(top_srcdir)/build;
	make addin-xml-string-extractor.exe
	popd;


# Generate a fake source file containing strings for
# translation that are found in our .addin.xml files
ADDIN_XML_FILES = $(shell find $(top_srcdir)/src -name \*.addin.xml | grep -v /obj/)
addin-xml-strings.cs: $(ADDIN_XML_FILES) $(top_srcdir)/build/addin-xml-string-extractor.exe
	@echo "  GEN   $@"
	$(MONO) $(top_srcdir)/build/addin-xml-string-extractor.exe $(ADDIN_XML_FILES) > $@

all: addin-xml-strings.cs

EXTRA_DIST = \
	addin-xml-strings.cs

