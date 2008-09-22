include $(top_srcdir)/Makefile.include

ASSEMBLY_NAME = Tao.OpenGl.Glu

ASSEMBLY_SOURCES =			\
	$(srcdir)/AssemblyInfo.cs 	\
	$(srcdir)/Glu.cs

REFS =								\
	-r:$(DIR_TAO_OPENGL)/Tao.OpenGl.dll

PKGS =

ASSEMBLY = $(ASSEMBLY_NAME).dll

EXTRAFLAGS = -unsafe

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
