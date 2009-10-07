include $(top_srcdir)/Makefile.include

SRCS =					\
	$(srcdir)/Dialog.cs		\
	$(srcdir)/Drawable.cs		\
	$(srcdir)/Global.cs		\
	$(srcdir)/Image.cs		\
	$(srcdir)/ImageMenuItem.cs	\
	$(srcdir)/Widget.cs		\
	$(srcdir)/Format.cs		\
	$(srcdir)/Priority.cs		\
	$(srcdir)/Shell.cs		\
	$(srcdir)/Sources.cs		\
	$(srcdir)/Style.cs		\
	$(srcdir)/Timeout.cs		\
	$(srcdir)/PixbufFormat.cs	\
	$(srcdir)/PixbufLoader.cs	\
	$(srcdir)/AssemblyInfo.cs


CUSTOMS =					\
	$(srcdir)/Builder.custom

ASSEMBLIES =					\
	-pkg:glib-sharp-2.0			\
	$(LINK_GIOSHARP)			\
	-pkg:gtk-sharp-2.0

SNK = snk

all: gtk-sharp-beans.dll

gtkbeans-api.xml: $(srcdir)/gtkbeans-api.raw $(srcdir)/GtkBeans.metadata
	@echo -e "\n*** Massaging the raw api into $@"
	cp $(srcdir)/gtkbeans-api.raw gtkbeans-api.xml
	chmod +w gtkbeans-api.xml
	$(GAPI2FIXUP) --api=gtkbeans-api.xml --metadata=$(srcdir)/GtkBeans.metadata

gtk-api.xml: $(srcdir)/gtk-api.raw $(srcdir)/Gtk.metadata
	@echo -e "\n*** Massaging the raw api into $@"
	cp $(srcdir)/gtk-api.raw gtk-api.xml
	chmod +w gtk-api.xml
	$(GAPI2FIXUP) --api=gtk-api.xml --metadata=$(srcdir)/Gtk.metadata

gtk_generated/Builder.cs: gtk-api.xml gtkbeans-api.xml $(GTK_CUSTOMS)
	@echo -e "\n*** Generating C# code"
	$(GAPI2CODEGEN) $(GLIBSHARP_CFLAGS) --outdir=gtk_generated --customdir=$(srcdir) --generate gtkbeans-api.xml
	$(GAPI2CODEGEN) $(GLIBSHARP_CFLAGS) $(GTKSHARP_CFLAGS) -I:$(top_builddir)/lib/gio-sharp/gio/gio-api.xml --outdir=gtk_generated --customdir=$(srcdir) --generate gtk-api.xml

gtk-sharp-beans.snk: $(SNK)
	cp $< $@

gtk-sharp-beans.dll: gtk_generated/Builder.cs $(SRCS) gtk-sharp-beans.snk
	@echo -e "\n*** Building $@"
	$(CSC) -target:library -out:$@ $(CSC_DEFINES) -unsafe $(ASSEMBLIES) $(SRCS) gtk_generated/*.cs

CLEANFILES = 			\
	gtk-sharp-beans.dll 	\
	gtk-sharp-beans.dll.mdb \
	gtkbeans-api.xml	\
	gtk-api.xml

DISTCLEANFILES =		\
	gtk_generated/*.cs	\
	gtk-sharp-beans.snk

EXTRA_DIST =			\
	$(SRCS)			\
	$(CUSTOMS)		\
	$(SNK)		\
	gtk-sharp-beans.dll.config	\
	Gtk.metadata		\
	GtkBeans.metadata		\
	gtkbeans-api.raw	\
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
		fetch/gtkbeans-api.raw		\
		fetch/Gdk.Sources/*.cs		\
		fetch/GLib.Sources/*.cs		\
		fetch/Gtk.Customs/*.custom	\
		fetch/Gtk.Sources/*.cs		\
		fetch/Gtk.metadata		\
		fetch/GtkBeans.metadata		\
		.

.PHONY: fetch update
