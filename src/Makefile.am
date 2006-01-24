CSC = mcs
CSFLAGS = -unsafe -debug -nowarn:0618 -nowarn:0169 -nowarn:0612 -d:TEST_METADATA 

F_SPOT_CSDISTFILES =				\
	$(srcdir)/Cairo/Cairo.cs		\
	$(srcdir)/Cairo/Surface.cs		\
	$(srcdir)/Cairo/FontOptions.cs		\
	$(srcdir)/Cairo/Matrix.cs		\
	$(srcdir)/Cairo/Pattern.cs		\
	$(srcdir)/Cairo/Graphics.cs		\
	$(srcdir)/Mail/Base64AttachmentEncoder.cs	\
	$(srcdir)/Mail/EsmtpClient.cs		\
	$(srcdir)/Mail/EsmtpMail.cs		\
	$(srcdir)/Mail/EsmtpStream.cs		\
	$(srcdir)/Mail/IAttachmentEncoder.cs	\
	$(srcdir)/Mail/MailAddressCollection.cs	\
	$(srcdir)/Mail/MailAddress.cs		\
	$(srcdir)/Mail/MailHeader.cs		\
	$(srcdir)/Mail/MailMessageWrapper.cs	\
	$(srcdir)/Mail/MailUtil.cs		\
	$(srcdir)/Mail/MimeTypes.cs		\
	$(srcdir)/Mail/SmtpException.cs		\
	$(srcdir)/Mail/SmtpResponse.cs		\
	$(srcdir)/Mail/ToUUEncodingTransform.cs	\
	$(srcdir)/Mail/UUAttachmentEncoder.cs	\
	$(srcdir)/AsyncPixbufLoader.cs		\
	$(srcdir)/Bim.cs			\
	$(srcdir)/BitConverter.cs		\
	$(srcdir)/PhotoArray.cs 		\
	$(srcdir)/CDExport.cs			\
	$(srcdir)/Ciff.cs			\
	$(srcdir)/ColorDialog.cs		\
	$(srcdir)/CompatFileChooser.cs		\
	$(srcdir)/Core.cs			\
	$(srcdir)/DateCommands.cs		\
	$(srcdir)/Db.cs				\
	$(srcdir)/DCRawFile.cs			\
	$(srcdir)/Delay.cs			\
	$(srcdir)/DirectoryAdaptor.cs		\
	$(srcdir)/DirectoryCollection.cs	\
	$(srcdir)/Exif.cs			\
	$(srcdir)/FlickrExport.cs		\
	$(srcdir)/FlickrRemote.cs		\
	$(srcdir)/FileImportBackend.cs		\
	$(srcdir)/FolderExport.cs		\
	$(srcdir)/FotkiRemote.cs		\
	$(srcdir)/FormClient.cs			\
	$(srcdir)/FullScreenView.cs		\
	$(srcdir)/GalleryRemote.cs		\
	$(srcdir)/GalleryExport.cs		\
	$(srcdir)/GladeDialog.cs		\
	$(srcdir)/Global.cs			\
	$(srcdir)/GroupAdaptor.cs		\
	$(srcdir)/GroupSelector.cs		\
	$(srcdir)/HigMessageDialog.cs		\
	$(srcdir)/Histogram.cs			\
	$(srcdir)/IBrowsableItem.cs		\
	$(srcdir)/IconView.cs			\
	$(srcdir)/ImageFile.cs			\
	$(srcdir)/ImageView.cs			\
	$(srcdir)/ImportBackend.cs		\
	$(srcdir)/ImportCommand.cs		\
	$(srcdir)/ImportStore.cs		\
	$(srcdir)/InfoBox.cs			\
	$(srcdir)/InfoDisplay.cs		\
	$(srcdir)/InternalProcess.cs		\
	$(srcdir)/IOChannel.cs			\
	$(srcdir)/IptcFile.cs			\
	$(srcdir)/JpegFile.cs			\
	$(srcdir)/JpegHeader.cs			\
	$(srcdir)/JpegUtils.cs			\
	$(srcdir)/Cms.cs			\
	$(srcdir)/Loupe.cs			\
	$(srcdir)/MainWindow.cs			\
	$(srcdir)/MetadataStore.cs		\
	$(srcdir)/MrwFile.cs			\
	$(srcdir)/Operation.cs			\
	$(srcdir)/PhotoImageView.cs		\
	$(srcdir)/PhotoLoader.cs		\
	$(srcdir)/PhotoPopup.cs			\
	$(srcdir)/PhotoQuery.cs			\
	$(srcdir)/PhotoStore.cs			\
	$(srcdir)/PhotoTagMenu.cs		\
	$(srcdir)/PhotoVersionCommands.cs	\
	$(srcdir)/PhotoVersionMenu.cs		\
	$(srcdir)/PhotoView.cs			\
	$(srcdir)/PngFile.cs			\
	$(srcdir)/PnmFile.cs			\
	$(srcdir)/PixbufLoader.cs		\
	$(srcdir)/PixbufUtils.cs		\
	$(srcdir)/PixbufCache.cs		\
	$(srcdir)/PixelBuffer.cs		\
	$(srcdir)/PreferenceDialog.cs		\
	$(srcdir)/Preferences.cs 		\
	$(srcdir)/PreviewPopup.cs 		\
	$(srcdir)/PrintDialog.cs		\
	$(srcdir)/ProgressDialog.cs		\
	$(srcdir)/RafFile.cs			\
	$(srcdir)/RotateCommand.cs		\
	$(srcdir)/ScalingIconView.cs		\
	$(srcdir)/SlideView.cs			\
	$(srcdir)/SingleView.cs			\
	$(srcdir)/StockIcons.cs			\
	$(srcdir)/SimpleCalendar.cs		\
	$(srcdir)/SvgFile.cs			\
	$(srcdir)/TagCommands.cs		\
	$(srcdir)/TagMenu.cs			\
	$(srcdir)/TagPopup.cs			\
	$(srcdir)/TagSelectionWidget.cs		\
	$(srcdir)/TagStore.cs			\
	$(srcdir)/TagView.cs			\
	$(srcdir)/ThreadProgressDialog.cs	\
	$(srcdir)/ThumbnailCache.cs		\
	$(srcdir)/ThumbnailGenerator.cs		\
	$(srcdir)/Tiff.cs			\
	$(srcdir)/TimeAdaptor.cs		\
	$(srcdir)/TimeDialog.cs			\
	$(srcdir)/TipWindow.cs			\
	$(srcdir)/Unix.cs			\
	$(srcdir)/Util.cs			\
	$(srcdir)/ThumbnailCommand.cs		\
	$(srcdir)/QueryDisplay.cs		\
	$(srcdir)/QueryView.cs			\
	$(srcdir)/ZoomUtils.cs			\
	$(srcdir)/GPhotoCamera.cs		\
	$(srcdir)/CameraSelectionDialog.cs	\
	$(srcdir)/CameraFileSelectionDialog.cs	\
	$(srcdir)/TagSelectionDialog.cs		\
	$(srcdir)/X3fFile.cs			\
	$(srcdir)/XmpFile.cs			\
	$(srcdir)/main.cs

ASSEMBLIES =					\
	-r:$(top_builddir)/libgphoto2-sharp/libgphoto2-sharp.dll   \
	-r:$(top_builddir)/semweb/SemWeb.dll	\
	-r:System.Data				\
	-r:System.Web				\
	-r:Mono.Data.SqliteClient		\
	-r:Mono.Posix				\
	-r:Mono.Security			\
	-r:ICSharpCode.SharpZipLib		\
	-pkg:dbus-sharp				\
	-pkg:gtkhtml-sharp-2.0			\
	-pkg:glade-sharp-2.0			\
	-pkg:gnome-vfs-sharp-2.0		\
	-pkg:gconf-sharp-2.0

RESOURCES =										\
	-resource:$(top_srcdir)/icons/f-spot-browse.png,f-spot-browse.png		\
	-resource:$(top_srcdir)/icons/f-spot-crop.png,f-spot-crop.png			\
	-resource:$(top_srcdir)/icons/f-spot-camera.png,f-spot-camera.png		\
	-resource:$(top_srcdir)/icons/f-spot-desaturate.png,f-spot-desaturate.png	\
	-resource:$(top_srcdir)/icons/f-spot-edit-image.png,f-spot-edit-image.png	\
	-resource:$(top_srcdir)/icons/f-spot-events.png,f-spot-events.png		\
	-resource:$(top_srcdir)/icons/f-spot-favorite.png,f-spot-favorite.png 		\
	-resource:$(top_srcdir)/icons/f-spot-hidden.png,f-spot-hidden.png 		\
	-resource:$(top_srcdir)/icons/f-spot-loading.png,f-spot-loading.png		\
	-resource:$(top_srcdir)/icons/f-spot-logo.png,f-spot-logo.png			\
	-resource:$(top_srcdir)/icons/f-spot-new-tag.png,f-spot-new-tag.png		\
	-resource:$(top_srcdir)/icons/f-spot-other.png,f-spot-other.png			\
	-resource:$(top_srcdir)/icons/f-spot-people.png,f-spot-people.png 		\
	-resource:$(top_srcdir)/icons/f-spot-places.png,f-spot-places.png 		\
	-resource:$(top_srcdir)/icons/f-spot-question-mark.png,f-spot-question-mark.png \
	-resource:$(top_srcdir)/icons/f-spot-red-eye.png,f-spot-red-eye.png		\
	-resource:$(top_srcdir)/icons/f-spot-rotate-90.png,f-spot-rotate-90.png		\
	-resource:$(top_srcdir)/icons/f-spot-rotate-270.png,f-spot-rotate-270.png	\
	-resource:$(top_srcdir)/icons/f-spot-sepia.png,f-spot-sepia.png			\
	-resource:$(top_srcdir)/icons/f-spot-slideshow.png,f-spot-slideshow.png		\
	-resource:$(top_srcdir)/icons/f-spot-stock_near.png,f-spot-stock_near.png	\
	-resource:$(top_srcdir)/icons/f-spot-stock_far.png,f-spot-stock_far.png		\
	-resource:$(top_srcdir)/icons/f-spot-fullscreen.png,f-spot-fullscreen.png	\
	-resource:$(top_srcdir)/icons/f-spot-simple-white.css,f-spot-simple-white.css	\
	-resource:$(top_srcdir)/icons/f-spot-simple.css,f-spot-simple.css	\
	-resource:$(top_srcdir)/icons/f-spot.js,f-spot.js	\
	-resource:$(srcdir)/dces.rdf,dces.rdf			\
	-resource:$(srcdir)/f-spot.glade,f-spot.glade

fspotlibdir = $(libdir)/f-spot
fspotlib_DATA = f-spot.exe.config	\
		f-spot.exe

bin_SCRIPTS = f-spot

F_SPOT_CSFILES = $(F_SPOT_CSDISTFILES) Defines.cs

f-spot.exe: $(F_SPOT_CSFILES) f-spot.glade dces.rdf
	$(CSC) -out:$@ $(CSFLAGS) $(F_SPOT_CSFILES) $(ASSEMBLIES) $(RESOURCES)

all: f-spot.exe

EXTRA_DIST =					\
	$(F_SPOT_CSDISTFILES)			\
	dces.rdf				\
	f-spot.exe.config.in			\
	f-spot.glade				\
	f-spot.in

CLEANFILES =					\
	f-spot.exe.config			\
	f-spot.exe.mdb				\
	f-spot.exe				\
	f-spot


DISTCLEANFILES = 				\
	makefile
