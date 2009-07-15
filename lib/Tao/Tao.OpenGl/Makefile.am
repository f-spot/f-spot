include $(top_srcdir)/Makefile.include

ASSEMBLY_NAME = Tao.OpenGl

ASSEMBLY_SOURCES =			\
	$(srcdir)/AssemblyInfo.cs 	\
	$(srcdir)/Gl.cs			\
	$(srcdir)/ContextGl.cs

REFS =									\
	-r:$(DIR_TAO_EXTENSIONLOADER)/Tao.OpenGl.ExtensionLoader.dll

PKGS =

ASSEMBLY = $(ASSEMBLY_NAME).dll

EXTRAFLAGS =

build: gl_build

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_NAME)-pre.dll
	MONO_PATH=$(DIR_TAO_EXTENSIONLOADER):. $(RUNTIME) $(DIR_TAO_GLPOSTPROCESS)/Tao.GlPostProcess.exe \
		$< $@ "$(srcdir)/$(ASSEMBLY_NAME).snk" \
		Tao.OpenGl.Gl Tao.OpenGl.ContextGl

$(ASSEMBLY_NAME)-pre.dll: $(ASSEMBLY_SOURCES)
	$(CSC_LIB) -out:$@ $(EXTRAFLAGS) $(PKGS) $(REFS) $(ASSEMBLY_SOURCES)

assemblydir = $(pkglibdir)
assembly_DATA =			\
	$(ASSEMBLY)		\
	$(ASSEMBLY).config
	

EXTRA_DIST =				\
	$(ASSEMBLY_SOURCES)		\
	$(ASSEMBLY_NAME).snk		\
	$(ASSEMBLY).config

CLEANFILES =				\
	$(ASSEMBLY)			\
	$(ASSEMBLY).mdb			\
	$(ASSEMBLY_NAME)-pre.dll	\
	$(ASSEMBLY_NAME)-pre.dll.mdb
