LIBRARY		= Tao.OpenGl.ExtensionLoader

include $(top_srcdir)/am-config.mk

FILES = \
	AssemblyInfo.cs \
	GlExtensionLoader.cs

REFERENCES =

BUILD_FILES = $(addprefix $(srcdir)/, $(FILES))

all: $(ASSEMBLY)

$(ASSEMBLY): $(FILES)
	mkdir -p $(top_builddir)/
	$(CSC) -debug -out:$@ \
		-target:$(TARGET) \
		$(REFERENCES) \
		$(CSFLAGS) \
		$(BUILD_FILES)

libtaogldir = $(libdir)/f-spot
libtaogl_DATA = $(ASSEMBLY) Tao.OpenGl.ExtensionLoader.dll.config

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = \
	$(FILES) \
	Tao.OpenGl.ExtensionLoader.snk \
	Tao.OpenGl.ExtensionLoader.dll.config
