include $(top_srcdir)/Makefile.include

ASSEMBLY_NAME = NDesk.DBus

ASSEMBLY_SOURCES =			\
	$(srcdir)/Address.cs 		\
	$(srcdir)/AssemblyInfo.cs 	\
	$(srcdir)/Authentication.cs 	\
	$(srcdir)/Bus.cs 		\
	$(srcdir)/BusObject.cs 		\
	$(srcdir)/Connection.cs 	\
	$(srcdir)/DBus.cs 		\
	$(srcdir)/DProxy.cs 		\
	$(srcdir)/ExportObject.cs 	\
	$(srcdir)/Introspection.cs 	\
	$(srcdir)/Mapper.cs 		\
	$(srcdir)/MatchRule.cs 		\
	$(srcdir)/Message.cs 		\
	$(srcdir)/MessageFilter.cs 	\
	$(srcdir)/MessageReader.cs 	\
	$(srcdir)/MessageWriter.cs 	\
	$(srcdir)/PendingCall.cs	\
	$(srcdir)/Protocol.cs 		\
	$(srcdir)/Signature.cs 		\
	$(srcdir)/SocketTransport.cs 	\
	$(srcdir)/Transport.cs 		\
	$(srcdir)/TypeImplementer.cs 	\
	$(srcdir)/UnixNativeTransport.cs\
	$(srcdir)/UnixTransport.cs 	\
	$(srcdir)/Wrapper.cs

REFS = -r:Mono.Posix

PKGS =

ASSEMBLY = $(ASSEMBLY_NAME).dll

EXTRAFLAGS = -unsafe

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(CSC_LIB) -out:$@ $(EXTRAFLAGS) $(PKGS) $(REFS) $(ASSEMBLY_SOURCES)

assemblydir = $(pkglibdir)
assembly_DATA =	$(ASSEMBLY)

EXTRA_DIST =				\
	$(ASSEMBLY_SOURCES)		\
	README				\
	COPYING

CLEANFILES =			\
	$(ASSEMBLY)		\
	$(ASSEMBLY).mdb
