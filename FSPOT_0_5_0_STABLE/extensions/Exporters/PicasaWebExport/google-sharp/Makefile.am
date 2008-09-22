include $(top_srcdir)/Makefile.include

ASSEMBLY_NAME = Mono.Google

ASSEMBLY_SOURCES =				\
	$(srcdir)/AlbumAccess.cs  		\
	$(srcdir)/AssemblyInfo.cs 		\
	$(srcdir)/Authentication.cs  		\
	$(srcdir)/CaptchaException.cs  		\
	$(srcdir)/CreateAlbumException.cs  	\
	$(srcdir)/DeleteAlbumException.cs  	\
	$(srcdir)/GDataApi.cs  			\
	$(srcdir)/GoogleConnection.cs  		\
	$(srcdir)/GoogleService.cs  		\
	$(srcdir)/MultipartRequest.cs  		\
	$(srcdir)/NoCheckCertificatePolicy.cs  	\
	$(srcdir)/PicasaAlbum.cs  		\
	$(srcdir)/PicasaAlbumCollection.cs  	\
	$(srcdir)/PicasaPicture.cs  		\
	$(srcdir)/PicasaPictureCollection.cs  	\
	$(srcdir)/PicasaWeb.cs  		\
	$(srcdir)/UploadPictureException.cs 	\
	$(srcdir)/UploadProgressEventArgs.cs  	\
	$(srcdir)/UploadProgressEventHandler.cs \
	$(srcdir)/XmlUtil.cs

REFS = -r:System.Web

PKGS =

ASSEMBLY = $(ASSEMBLY_NAME).dll

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(CSC_LIB) -out:$@ $(PKGS) $(REFS) $(ASSEMBLY_SOURCES)

assemblydir = $(pkglibdir)
assembly_DATA =	$(ASSEMBLY)

EXTRA_DIST = $(ASSEMBLY_SOURCES)

CLEANFILES =			\
	$(ASSEMBLY)		\
	$(ASSEMBLY).mdb
