include $(top_srcdir)/Makefile.include

ASSEMBLY_NAME = Tao.GlPostProcess

ASSEMBLY_SOURCES =			\
	$(srcdir)/AssemblyInfo.cs 	\
	$(srcdir)/GlPostProcess.cs

REFS =									\
	-r:$(DIR_TAO_EXTENSIONLOADER)/Tao.OpenGl.ExtensionLoader.dll

PKGS =

ASSEMBLY = $(ASSEMBLY_NAME).exe

EXTRAFLAGS =

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(CSC) -out:$@ $(EXTRAFLAGS) $(PKGS) $(REFS) $(ASSEMBLY_SOURCES)

EXTRA_DIST =				\
	$(ASSEMBLY_SOURCES)		\
	$(ASSEMBLY_NAME).snk

CLEANFILES =			\
	$(ASSEMBLY)		\
	$(ASSEMBLY).mdb
