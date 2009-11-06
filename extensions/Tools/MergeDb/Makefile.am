addin = MergeDb

addin_manifest = $(addin).addin.xml

addin_sources = 			\
	$(addin).cs			\
	MergeDbDialog.cs		\
	PickFolderDialog.cs

addin_resources =			\
	$(addin_manifest)		\
	$(addin).glade

addin_references =			\
	Mono.Posix

addin_packages =			\
	gtk-sharp-2.0			\
	glade-sharp-2.0

addin_extradist =

include $(top_srcdir)/Makefile.addins

