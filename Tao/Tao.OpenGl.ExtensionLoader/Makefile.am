include $(top_srcdir)/Makefile.include

ASSEMBLY_NAME = Tao.OpenGl.ExtensionLoader

ASSEMBLY_SOURCES =			\
	$(srcdir)/AssemblyInfo.cs 	\
	$(srcdir)/GlExtensionLoader.cs

REFS =

PKGS =

ASSEMBLY = $(ASSEMBLY_NAME).dll

EXTRAFLAGS =

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(CSC_LIB) -out:$@ $(EXTRAFLAGS) $(PKGS) $(REFS) $(ASSEMBLY_SOURCES)

assemblydir = $(pkglibdir)
assembly_DATA =			\
	$(ASSEMBLY)		\
	$(ASSEMBLY).config
	

EXTRA_DIST =				\
	$(ASSEMBLY_SOURCES)		\
	$(ASSEMBLY_NAME).snk		\
	$(ASSEMBLY).config

CLEANFILES =			\
	$(ASSEMBLY)		\
	$(ASSEMBLY).mdb
