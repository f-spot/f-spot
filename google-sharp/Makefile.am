CSC = mcs

LIBGOOGLESHARP_CSFILES =				\
	$(srcdir)/AlbumAccess.cs  \
	$(srcdir)/Authentication.cs  \
	$(srcdir)/CreateAlbumException.cs  \
	$(srcdir)/DeleteAlbumException.cs  \
	$(srcdir)/GoogleConnection.cs  \
	$(srcdir)/GoogleService.cs  \
	$(srcdir)/MultipartRequest.cs  \
	$(srcdir)/NoCheckCertificatePolicy.cs  \
	$(srcdir)/PicasaAlbum.cs  \
	$(srcdir)/PicasaAlbumCollection.cs  \
	$(srcdir)/PicasaPicture.cs  \
	$(srcdir)/PicasaPictureCollection.cs  \
	$(srcdir)/PicasaV1.cs  \
	$(srcdir)/PicasaWeb.cs  \
	$(srcdir)/UploadPictureException.cs \
	$(srcdir)/UploadProgressEventArgs.cs  \
	$(srcdir)/UploadProgressEventHandler.cs \
	$(srcdir)/XmlUtil.cs

libgooglesharplibdir = $(libdir)/f-spot
libgooglesharplib_DATA =		\
	google-sharp.dll


google-sharp.dll: $(LIBGOOGLESHARP_CSFILES)
	$(CSC) -out:$@ /target:library $(LIBGOOGLESHARP_CSFILES)

all: google-sharp.dll

EXTRA_DIST =							\
	$(LIBGOOGLESHARP_CSFILES)	


CLEANFILES =							\
	google-sharp.dll

