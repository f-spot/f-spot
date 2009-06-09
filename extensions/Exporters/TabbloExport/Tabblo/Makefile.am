include $(top_srcdir)/Makefile.include

ASSEMBLY_NAME = Mono.Tabblo

ASSEMBLY_SOURCES = \
	$(srcdir)/AssemblyInfo.cs \
	$(srcdir)/Connection.cs \
	$(srcdir)/IPreferences.cs \
	$(srcdir)/MultipartRequest.cs \
	$(srcdir)/Picture.cs \
	$(srcdir)/TabbloException.cs \
	$(srcdir)/TotalUploadProgress.cs \
	$(srcdir)/Uploader.cs \
	$(srcdir)/UploadProgressEventArgs.cs \
	$(srcdir)/UploadProgressEventHandler.cs

REFS = \
       -r:Mono.Posix.dll \
       -r:System.Web.dll

PKGS =

ASSEMBLY = $(ASSEMBLY_NAME).dll

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(CSC_LIB) -out:$@ $(PKGS) $(REFS) $(ASSEMBLY_SOURCES)

assemblydir = $(pkglibdir)
assembly_DATA =	$(ASSEMBLY)

EXTRA_DIST =			\
	$(ASSEMBLY_SOURCES)

CLEANFILES =			\
	$(ASSEMBLY)		\
	$(ASSEMBLY).mdb
