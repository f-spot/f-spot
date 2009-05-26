include ../Makefile.include

SRCS =					\
	$(srcdir)/Dialog.cs		\
	$(srcdir)/Image.cs		\
	$(srcdir)/Widget.cs		\
	$(srcdir)/Format.cs		\
	$(srcdir)/Priority.cs		\
	$(srcdir)/Sources.cs		\
	$(srcdir)/Timeout.cs		\
	$(srcdir)/PixbufFormat.cs	\
	$(srcdir)/AssemblyInfo.cs


CUSTOMS =					\
	$(srcdir)/Builder.custom

ASSEMBLIES =					\
	-pkg:glib-sharp-2.0			\
	$(LINK_GIOSHARP)			\
	-pkg:gtk-sharp-2.0

all: gtk-sharp-beans.dll

gtk-api.xml: $(srcdir)/gtk-api.raw $(srcdir)/Gtk.metadata
	@echo -e "\n*** Massaging the raw api into $@"
	cp $(srcdir)/gtk-api.raw gtk-api.xml
	chmod +w $(builddir)/gtk-api.xml
	gapi2-fixup --api=gtk-api.xml --metadata=$(srcdir)/Gtk.metadata

gtk_generated/Builder.cs: gtk-api.xml $(GTK_CUSTOMS)
	@echo -e "\n*** Generating C# code"
	gapi2-codegen $(GLIBSHARP_CFLAGS) --outdir=gtk_generated --customdir=$(srcdir) --generate gtk-api.xml

gtk-sharp-beans.dll: $(srcdir)/gtk_generated/Builder.cs $(SRCS)
	@echo -e "\n*** Building $@"
	$(CSC) -target:library -out:$@ -unsafe $(ASSEMBLIES) $(SRCS) $(srcdir)/gtk_generated/*.cs

CLEANFILES = 			\
	gtk-sharp-beans.dll 	\
	gtk-sharp-beans.dll.mdb \
	gtk-api.xml

EXTRA_DIST =			\
	$(SRCS)			\
	$(CUSTOMS)		\
	gtk-api.raw

assemblydir = $(pkglibdir)
assembly_DATA =			\
	gtk-sharp-beans.dll	\
	gtk-sharp-beans.dll.config


fetch:
	rm -rf fetch
	git clone git://gitorious.org/gtk-sharp-beans/mainline.git fetch

update:
	cp	fetch/*.cs			\
		fetch/gtk-api.raw		\
		fetch/Gdk.Sources/*.cs		\
		fetch/GLib.Sources/*.cs		\
		fetch/Gtk.Customs/*.custom	\
		fetch/Gtk.Sources/*.cs		\
		fetch/Gtk.metadata		\
		.

.PHONY: fetch update
