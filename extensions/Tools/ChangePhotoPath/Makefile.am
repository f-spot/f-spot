addin = ChangePhotoPath

addin_manifest = $(addin).addin.xml

addin_sources = 			\
	ChangePhotoPathController.cs	\
	ChangePhotoPathGui.cs		\
	IChangePhotoPathGui.cs

addin_resources =			\
	$(addin_manifest)		\
	ChangePhotoPath.glade

addin_references =			\
	Mono.Posix

addin_packages =			\
	gtk-sharp-2.0			\
	glade-sharp-2.0

addin_extradist =			\
	ChangePhotoPath.glade

include $(top_srcdir)/Makefile.addins
