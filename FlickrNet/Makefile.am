CSC = gmcs

LIBFLICKRNET_CSFILES =				\
	$(srcdir)/AssemblyInfo.cs \
	$(srcdir)/Auth.cs \
	$(srcdir)/Blogs.cs \
	$(srcdir)/Cache.cs \
	$(srcdir)/Categories.cs \
	$(srcdir)/Contacts.cs \
	$(srcdir)/Context.cs \
	$(srcdir)/DateGranularity.cs \
	$(srcdir)/ExifPhoto.cs \
	$(srcdir)/Flickr.cs \
	$(srcdir)/FlickrConfigurationManager.cs \
	$(srcdir)/FlickrConfigurationSettings.cs \
	$(srcdir)/FlickrException.cs \
	$(srcdir)/Groups.cs \
	$(srcdir)/Licenses.cs \
	$(srcdir)/OnlineUsers.cs \
	$(srcdir)/Person.cs \
	$(srcdir)/PhotoCounts.cs \
	$(srcdir)/PhotoInfo.cs \
	$(srcdir)/Photos.cs \
	$(srcdir)/PhotoSearchOptions.cs \
	$(srcdir)/PhotoSets.cs \
	$(srcdir)/Response.cs \
	$(srcdir)/Sizes.cs \
	$(srcdir)/Uploader.cs \
	$(srcdir)/UploadProgressEvent.cs \
	$(srcdir)/User.cs \
	$(srcdir)/Utils.cs 

libflickrnetlibdir = $(libdir)/f-spot
libflickrnetlib_DATA =		\
	FlickrNet.dll


FlickrNet.dll: $(LIBFLICKRNET_CSFILES)
	$(CSC) -r:System.Web.dll -out:$@ /target:library $(LIBFLICKRNET_CSFILES)

all: FlickrNet.dll

EXTRA_DIST =							\
	$(LIBFLICKRNET_CSFILES)	\
	License.txt

CLEANFILES =							\
	FlickrNet.dll

