SUBDIRS = pkg-config m4

DLL_MAP_VERIFIER_ASSEMBLY = dll-map-verifier.exe
ADDIN_XML_STRING_EXTRACTOR_ASSEMBLY = addin-xml-string-extractor.exe

ALL_TARGETS = $(DLL_MAP_VERIFIER_ASSEMBLY) $(ADDIN_XML_STRING_EXTRACTOR_ASSEMBLY)

all: $(ALL_TARGETS)

$(DLL_MAP_VERIFIER_ASSEMBLY): DllMapVerifier.cs
	$(MCS) -out:$@ $<

$(ADDIN_XML_STRING_EXTRACTOR_ASSEMBLY): AddinXmlStringExtractor.cs
	$(MCS) -out:$@ $<

EXTRA_DIST = \
	private-icon-theme-installer \
	DllMapVerifier.cs \
	dll-map-makefile-verifier \
	AddinXmlStringExtractor.cs

CLEANFILES = *.exe *.pdb
MAINTAINERCLEANFILES = Makefile.in
