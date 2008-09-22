include $(top_srcdir)/Makefile.include

ASSEMBLY_NAME = FlickrNet

ASSEMBLY_SOURCES =				\
	$(srcdir)/AssemblyInfo.cs 		\
	$(srcdir)/ActivityEvent.cs		\
	$(srcdir)/ActivityItem.cs		\
	$(srcdir)/ApiKeyRequiredException.cs	\
	$(srcdir)/Auth.cs 			\
	$(srcdir)/AuthenticationRequiredException.cs		\
	$(srcdir)/Blogs.cs 			\
	$(srcdir)/BoundaryBox.cs 		\
	$(srcdir)/Cache.cs 			\
	$(srcdir)/Categories.cs 		\
	$(srcdir)/Comments.cs 			\
	$(srcdir)/Contacts.cs 			\
	$(srcdir)/Context.cs 			\
	$(srcdir)/DateGranularity.cs 		\
	$(srcdir)/Enums.cs 			\
	$(srcdir)/ExifPhoto.cs 			\
	$(srcdir)/Flickr.cs 			\
	$(srcdir)/FlickrApiException.cs		\
	$(srcdir)/FlickrWebException.cs		\
	$(srcdir)/FlickrConfigurationManager.cs	\
	$(srcdir)/FlickrConfigurationSettings.cs\
	$(srcdir)/FlickrException.cs 		\
	$(srcdir)/GeoAccuracy.cs 		\
	$(srcdir)/GeoPermissions.cs 		\
	$(srcdir)/GroupSearchResults.cs 	\
	$(srcdir)/Groups.cs 			\
	$(srcdir)/Licenses.cs 			\
	$(srcdir)/LockFile.cs 			\
	$(srcdir)/Methods.cs 			\
	$(srcdir)/PartialSearchOptions.cs 	\
	$(srcdir)/PersistentCache.cs 		\
	$(srcdir)/Person.cs 			\
	$(srcdir)/Photo.cs 			\
	$(srcdir)/PhotoCounts.cs 		\
	$(srcdir)/PhotoDates.cs 		\
	$(srcdir)/PhotoInfo.cs 			\
	$(srcdir)/PhotoLocation.cs 		\
	$(srcdir)/PhotoPermissions.cs 		\
	$(srcdir)/PhotoSearchExtras.cs 		\
	$(srcdir)/PhotoSearchOptions.cs 	\
	$(srcdir)/PhotoSearchOrder.cs 		\
	$(srcdir)/PhotoSets.cs 			\
	$(srcdir)/Photos.cs 			\
	$(srcdir)/Response.cs 			\
	$(srcdir)/ResponseXmlException.cs	\
	$(srcdir)/SafeNativeMethods.cs		\
	$(srcdir)/SignatureRequiredException.cs	\
	$(srcdir)/Sizes.cs 			\
	$(srcdir)/Tags.cs 			\
	$(srcdir)/Uploader.cs 			\
	$(srcdir)/UploadProgressEvent.cs 	\
	$(srcdir)/User.cs 			\
	$(srcdir)/Utils.cs


REFS =  -r:System.Web.dll	\
	-r:System.Drawing.dll

PKGS =

ASSEMBLY = $(ASSEMBLY_NAME).dll

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(CSC_LIB) -out:$@ $(PKGS) $(REFS) $(ASSEMBLY_SOURCES)

assemblydir = $(pkglibdir)
assembly_DATA =	$(ASSEMBLY)

EXTRA_DIST =			\
	$(ASSEMBLY_SOURCES)	\
	License.txt

CLEANFILES =			\
	$(ASSEMBLY)		\
	$(ASSEMBLY).mdb
