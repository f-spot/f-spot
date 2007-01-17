LIBRARY		= Tao.OpenGl

include $(top_srcdir)/am-config.mk

FILES = \
	AssemblyInfo.cs \
	Gl.cs \
	ContextGl.cs

REFERENCES = \
	-r:$(top_builddir)/Tao/Tao.OpenGl.ExtensionLoader/Tao.OpenGl.ExtensionLoader.dll

BUILD_FILES = $(addprefix $(srcdir)/, $(FILES))

build: gl_build

all: $(ASSEMBLY)

$(ASSEMBLY): Tao.OpenGl-pre.dll
	MONO_PATH=$(MONO_PATH):$(top_builddir)/Tao/Tao.OpenGl.ExtensionLoader:. $(RUN_EXE) $(top_builddir)/Tao/Tao.GlPostProcess/Tao.GlPostProcess.exe \
		$< $@ "$(srcdir)/$(SNKFILE)" \
		Tao.OpenGl.Gl Tao.OpenGl.ContextGl

libtaogldir = $(libdir)/f-spot
libtaogl_DATA = $(ASSEMBLY) Tao.OpenGl.dll.config

Tao.OpenGl-pre.dll: $(FILES)
	mkdir -p $(top_builddir)/
	$(CSC) -debug -out:$@ \
		-target:$(TARGET) \
		$(REFERENCES) \
		$(CSFLAGS) \
		$(BUILD_FILES)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb Tao.OpenGl-pre.dll Tao.OpenGl-pre.dll.mdb
EXTRA_DIST = \
	$(FILES) \
	Tao.OpenGl.snk \
	Tao.OpenGl.dll.config
