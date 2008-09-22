include $(top_srcdir)/Makefile.include

ASSEMBLY_NAME = NDesk.Glitz

ASSEMBLY_SOURCES =			\
	$(srcdir)/Buffer.cs		\
	$(srcdir)/Context.cs		\
	$(srcdir)/Glitz.cs		\
	$(srcdir)/Drawable.cs		\
	$(srcdir)/Surface.cs		\
	$(srcdir)/TextureObject.cs

REFS =

PKGS =

ASSEMBLY = $(ASSEMBLY_NAME).dll

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(CSC_LIB) -out:$@ -unsafe $(PKGS) $(REFS) $(ASSEMBLY_SOURCES)

assemblydir = $(pkglibdir)
assembly_DATA =	$(ASSEMBLY)

EXTRA_DIST = $(ASSEMBLY_SOURCES)

CLEANFILES =			\
	$(ASSEMBLY)		\
	$(ASSEMBLY).mdb
