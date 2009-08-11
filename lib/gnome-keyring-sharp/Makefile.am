include $(top_srcdir)/Makefile.include

ASSEMBLY_NAME = gnome-keyring-sharp

ASSEMBLY_SOURCES =			\
	$(srcdir)/AccessRights.cs  	\
	$(srcdir)/AssemblyInfo.cs 	\
	$(srcdir)/AttributeType.cs  	\
	$(srcdir)/GenericItemData.cs  	\
	$(srcdir)/ItemACL.cs  		\
	$(srcdir)/ItemData.cs  		\
	$(srcdir)/ItemType.cs  		\
	$(srcdir)/KeyringException.cs  	\
	$(srcdir)/KeyringInfo.cs  	\
	$(srcdir)/NetItemData.cs  	\
	$(srcdir)/NoteItemData.cs  	\
	$(srcdir)/Operation.cs  	\
	$(srcdir)/RequestMessage.cs  	\
	$(srcdir)/ResponseMessage.cs  	\
	$(srcdir)/ResultCode.cs  	\
	$(srcdir)/Ring.cs  

REFS = -r:Mono.Posix $(NDESK_DBUS_LIBS)

PKGS =

ASSEMBLY = $(ASSEMBLY_NAME).dll

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(CSC_LIB) -out:$@ -d:WITH_DBUS $(PKGS) $(REFS) $(ASSEMBLY_SOURCES)

assemblydir = $(pkglibdir)
assembly_DATA =	$(ASSEMBLY)

EXTRA_DIST = $(ASSEMBLY_SOURCES)

CLEANFILES =			\
	$(ASSEMBLY)		\
	$(ASSEMBLY).mdb
