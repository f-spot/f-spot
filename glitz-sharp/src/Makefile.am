CSC=gmcs
CSFLAGS=-debug

LIBGLITZSHARP_CSFILES = \
	$(srcdir)/Buffer.cs	\
	$(srcdir)/Context.cs	\
	$(srcdir)/Glitz.cs	\
	$(srcdir)/Drawable.cs	\
	$(srcdir)/Surface.cs	\
	$(srcdir)/TextureObject.cs

%.exe %.dll %.module:
	$(CSC) $(CSFLAGS) -out:$@ -t:$(TARGET) $(addprefix -pkg:,$(PKGS)) $(addprefix -r:,$(REFS)) $(addprefix -r:,$(filter %.dll,$^)) $(addprefix -addmodule:,$(filter %.module,$^)) $(filter %.cs,$^)

%.exe: TARGET = exe

%.dll: TARGET = library

%.module: TARGET = module

NDesk.Glitz.dll: $(LIBGLITZSHARP_CSFILES)

all: NDesk.Glitz.dll

libglitzsharplibdir = $(libdir)/f-spot
libglitzsharplib_DATA =		\
	NDesk.Glitz.dll

EXTRA_DIST =				\
	$(LIBDBUSSHARP_CSFILES)

CLEANFILES =			\
	NDesk.Glitz.dll		\
	NDesk.Glitz.dll.mdb
