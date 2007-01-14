PROGRAM		= Tao.GlPostProcess
TARGET        = exe
ASSEMBLY_EXE    = $(PROGRAM).exe
ASSEMBLY      = $(ASSEMBLY_EXE)
ASSEMBLY_NAME   = $(LIBRARY)


FILES = \
	AssemblyInfo.cs \
	GlPostProcess.cs

REFERENCES = \
	-r:$(top_builddir)/Tao/Tao.OpenGl.ExtensionLoader/Tao.OpenGl.ExtensionLoader.dll

BUILD_FILES = $(addprefix $(srcdir)/, $(FILES))

all: $(ASSEMBLY)

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
	Tao.GlPostProcess.snk
