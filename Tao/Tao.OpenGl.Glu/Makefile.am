LIBRARY		= Tao.OpenGl.Glu

DOCS = 1
UNSAFE = 1

include $(top_srcdir)/am-config.mk

FILES = \
	AssemblyInfo.cs \
	Glu.cs

REFERENCES = \
	-r:$(top_builddir)/Tao/Tao.OpenGl/Tao.OpenGl.dll

BUILD_FILES = $(addprefix $(srcdir)/, $(FILES))

all: $(ASSEMBLY)

libtaoopengldir = $(libdir)/f-spot
libtaoopengl_DATA = $(ASSEMBLY) Tao.OpenGl.Glu.dll.config

$(ASSEMBLY): $(FILES)
	mkdir -p $(top_builddir)/
	$(CSC) -debug -out:$@ \
		-target:$(TARGET) \
		$(REFERENCES) \
		$(CSFLAGS) \
		$(BUILD_FILES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = \
	$(FILES) \
	Tao.OpenGl.Glu.snk \
	Tao.OpenGl.Glu.dll.config

