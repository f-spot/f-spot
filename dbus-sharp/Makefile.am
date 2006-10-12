CSC_DEBUGFLAGS=-debug
CSC=gmcs $(CSC_DEBUGFLAGS)
MONO_DEBUGFLAGS=--debug
RUNTIME=mono $(MONO_DEBUGFLAGS)

LIBDBUSSHARP_CSFILES =				\
	$(srcdir)/Address.cs \
	$(srcdir)/AssemblyInfo.cs \
	$(srcdir)/Authentication.cs \
	$(srcdir)/Bus.cs \
	$(srcdir)/BusObject.cs \
	$(srcdir)/Connection.cs \
	$(srcdir)/DBus.cs \
	$(srcdir)/DProxy.cs \
	$(srcdir)/ExportObject.cs \
	$(srcdir)/Introspection.cs \
	$(srcdir)/Mapper.cs \
	$(srcdir)/Message.cs \
	$(srcdir)/MessageFilter.cs \
	$(srcdir)/MessageReader.cs \
	$(srcdir)/MessageWriter.cs \
	$(srcdir)/Protocol.cs \
	$(srcdir)/Server.cs \
	$(srcdir)/Signature.cs \
	$(srcdir)/Transport.cs \
	$(srcdir)/UnixNativeTransport.cs \
	$(srcdir)/Wrapper.cs

%.exe %.dll %.module:
	$(CSC) $(CSFLAGS) -out:$@ -t:$(TARGET) $(addprefix -pkg:,$(PKGS)) $(addprefix -r:,$(REFS)) $(addprefix -r:,$(filter %.dll,$^)) $(addprefix -addmodule:,$(filter %.module,$^)) $(filter %.cs,$^)

%.exe: TARGET = exe

%.dll: TARGET = library

%.module: TARGET = module

NDesk.DBus.dll: REFS=Mono.Posix

NDesk.DBus.dll: $(LIBDBUSSHARP_CSFILES)

all: NDesk.DBus.dll

libdbussharplibdir = $(libdir)/f-spot
libdbussharplib_DATA =		\
	NDesk.DBus.dll

EXTRA_DIST =				\
	$(LIBDBUSSHARP_CSFILES)		\
	README				\
	COPYING

CLEANFILES =			\
	NDesk.DBus.dll		\
	NDesk.DBus.dll.mdb
