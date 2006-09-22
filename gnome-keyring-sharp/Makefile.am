CSC = mcs

LIBGNOMEKEYRING_CSFILES =				\
	$(srcdir)/AccessRights.cs  \
	$(srcdir)/AssemblyInfo.cs \
	$(srcdir)/AttributeType.cs  \
	$(srcdir)/GenericItemData.cs  \
	$(srcdir)/ItemACL.cs  \
	$(srcdir)/ItemData.cs  \
	$(srcdir)/ItemType.cs  \
	$(srcdir)/KeyringException.cs  \
	$(srcdir)/KeyringInfo.cs  \
	$(srcdir)/NetItemData.cs  \
	$(srcdir)/NoteItemData.cs  \
	$(srcdir)/Operation.cs  \
	$(srcdir)/RequestMessage.cs  \
	$(srcdir)/ResponseMessage.cs  \
	$(srcdir)/ResultCode.cs  \
	$(srcdir)/Ring.cs  

ASSEMBLIES =					\
	-r:Mono.Posix	

libgnomekeyringsharplibdir = $(libdir)/f-spot
libgnomekeyringsharplib_DATA =		\
	gnome-keyring-sharp.dll


gnome-keyring-sharp.dll: $(LIBGNOMEKEYRING_CSFILES)
	$(CSC) -out:$@ /target:library $(ASSEMBLIES) $(LIBGNOMEKEYRING_CSFILES)

all: gnome-keyring-sharp.dll

EXTRA_DIST =							\
	$(LIBGNOMEKEYRING_CSFILES)

CLEANFILES =							\
	gnome-keyring-sharp.dll

