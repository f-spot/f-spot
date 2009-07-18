include $(top_srcdir)/Makefile.include

ASSEMBLY_NAME = NDesk.DBus.GLib

ASSEMBLY_SOURCES =			\
	$(srcdir)/AssemblyInfo.cs 	\
	$(srcdir)/GLib.IO.cs 		\
	$(srcdir)/GLib.cs

REFS = -r:../$(DIR_DBUS)/NDesk.DBus.dll

PKGS =

ASSEMBLY = $(ASSEMBLY_NAME).dll

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(CSC_LIB) -out:$@ $(PKGS) $(REFS) $(ASSEMBLY_SOURCES)

assemblydir = $(pkglibdir)
assembly_DATA =		\
	$(ASSEMBLY)	\
	$(ASSEMBLY).config

EXTRA_DIST =				\
	$(ASSEMBLY_SOURCES)		\
	$(ASSEMBLY).config		\
	README				\
	COPYING

CLEANFILES =			\
	$(ASSEMBLY)		\
	$(ASSEMBLY).mdb
