CSC_DEBUGFLAGS=-debug
CSC=gmcs $(CSC_DEBUGFLAGS)
MONO_DEBUGFLAGS=--debug
RUNTIME=mono $(MONO_DEBUGFLAGS)

LIBDBUSSHARPGLIB_CSFILES =				\
	$(srcdir)/AssemblyInfo.cs \
	$(srcdir)/GLib.IO.cs \
	$(srcdir)/GLib.cs

%.exe %.dll %.module:
	$(CSC) $(CSFLAGS) -out:$@ -t:$(TARGET) $(addprefix -pkg:,$(PKGS)) $(addprefix -r:,$(REFS)) $(addprefix -r:,$(filter %.dll,$^)) $(addprefix -addmodule:,$(filter %.module,$^)) $(filter %.cs,$^)

%.exe: TARGET = exe

%.dll: TARGET = library

%.module: TARGET = module

all: NDesk.DBus.GLib.dll

NDesk.DBus.GLib.dll: PKGS = glib-sharp-2.0

NDesk.DBus.GLib.dll: ../dbus-sharp/NDesk.DBus.dll $(LIBDBUSSHARPGLIB_CSFILES)

libdbussharpgliblibdir = $(libdir)/f-spot
libdbussharpgliblib_DATA =		\
	NDesk.DBus.GLib.dll		\
	NDesk.DBus.GLib.dll.config

EXTRA_DIST = 				\
	NDesk.DBus.GLib.dll.config	\
	COPYING 			\
	README

CLEANFILES =				\
	NDesk.DBus.GLib.dll		\
	NDesk.DBus.GLib.dll.mdb

DISTCLEANFILES =			\
	makefile
