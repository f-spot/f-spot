CSC = gmcs

LIBFLICKRNET_CSFILES =				\
	$(srcdir)/AssemblyInfo.cs \
	$(srcdir)/Auth.cs \
	$(srcdir)/Blogs.cs \
	$(srcdir)/BoundaryBox.cs \
	$(srcdir)/Cache.cs \
	$(srcdir)/Categories.cs \
	$(srcdir)/Comments.cs \
	$(srcdir)/Contacts.cs \
	$(srcdir)/Context.cs \
	$(srcdir)/DateGranularity.cs \
	$(srcdir)/Enums.cs \
	$(srcdir)/ExifPhoto.cs \
	$(srcdir)/Flickr.cs \
	$(srcdir)/FlickrConfigurationManager.cs \
	$(srcdir)/FlickrConfigurationSettings.cs \
	$(srcdir)/FlickrException.cs \
	$(srcdir)/GeoAccuracy.cs \
	$(srcdir)/GeoPermissions.cs \
	$(srcdir)/GroupSearchResults.cs \
	$(srcdir)/Groups.cs \
	$(srcdir)/Licenses.cs \
	$(srcdir)/LockFile.cs \
	$(srcdir)/Methods.cs \
	$(srcdir)/PartialSearchOptions.cs \
	$(srcdir)/PersistentCache.cs \
	$(srcdir)/Person.cs \
	$(srcdir)/Photo.cs \
	$(srcdir)/PhotoCounts.cs \
	$(srcdir)/PhotoDates.cs \
	$(srcdir)/PhotoInfo.cs \
	$(srcdir)/PhotoLocation.cs \
	$(srcdir)/PhotoPermissions.cs \
	$(srcdir)/PhotoSearchExtras.cs \
	$(srcdir)/PhotoSearchOptions.cs \
	$(srcdir)/PhotoSearchOrder.cs \
	$(srcdir)/PhotoSets.cs \
	$(srcdir)/Photos.cs \
	$(srcdir)/Response.cs \
	$(srcdir)/Sizes.cs \
	$(srcdir)/Tags.cs \
	$(srcdir)/UploadProgressEvent.cs \
	$(srcdir)/Uploader.cs \
	$(srcdir)/User.cs \
	$(srcdir)/Utils.cs

libflickrnetlibdir = $(libdir)/f-spot
libflickrnetlib_DATA =		\
	FlickrNet.dll


FlickrNet.dll: $(LIBFLICKRNET_CSFILES)
	$(CSC) -r:System.Web.dll -r:System.Drawing.dll -out:$@ /target:library $(LIBFLICKRNET_CSFILES)

all: FlickrNet.dll

EXTRA_DIST =							\
	$(LIBFLICKRNET_CSFILES)	\
	License.txt

CLEANFILES =							\
	FlickrNet.dll

