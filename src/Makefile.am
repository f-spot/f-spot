include $(top_srcdir)/Makefile.include
EXTRAFLAGS = -unsafe -nowarn:0169 -nowarn:0612 -nowarn:0414 -d:TEST_METADATA -d:BROKEN_RSVG $(NUNIT_DEFINES) $(BEAGLE_DEFINES)

F_SPOT_CSDISTFILES =				\
	$(srcdir)/Cairo/Antialias.cs		\
	$(srcdir)/Cairo/Cairo.cs		\
	$(srcdir)/Cairo/Content.cs		\
	$(srcdir)/Cairo/Context.cs		\
	$(srcdir)/Cairo/DirectFBSurface.cs	\
	$(srcdir)/Cairo/Extend.cs		\
	$(srcdir)/Cairo/FillRule.cs		\
	$(srcdir)/Cairo/Filter.cs		\
	$(srcdir)/Cairo/FontExtents.cs		\
	$(srcdir)/Cairo/FontFace.cs		\
	$(srcdir)/Cairo/FontOptions.cs		\
	$(srcdir)/Cairo/FontSlant.cs		\
	$(srcdir)/Cairo/FontType.cs		\
	$(srcdir)/Cairo/FontWeight.cs		\
	$(srcdir)/Cairo/Format.cs		\
	$(srcdir)/Cairo/GlitzSurface.cs		\
	$(srcdir)/Cairo/Glyph.cs		\
	$(srcdir)/Cairo/Gradient.cs		\
	$(srcdir)/Cairo/HintMetrics.cs		\
	$(srcdir)/Cairo/HintStyle.cs		\
	$(srcdir)/Cairo/ImageSurface.cs		\
	$(srcdir)/Cairo/LinearGradient.cs	\
	$(srcdir)/Cairo/LineCap.cs		\
	$(srcdir)/Cairo/LineJoin.cs		\
	$(srcdir)/Cairo/Matrix.cs		\
	$(srcdir)/Cairo/Operator.cs		\
	$(srcdir)/Cairo/Pattern.cs		\
	$(srcdir)/Cairo/PatternType.cs		\
	$(srcdir)/Cairo/PdfSurface.cs		\
	$(srcdir)/Cairo/PSSurface.cs		\
	$(srcdir)/Cairo/RadialGradient.cs	\
	$(srcdir)/Cairo/Rectangle.cs		\
	$(srcdir)/Cairo/SolidPattern.cs		\
	$(srcdir)/Cairo/Status.cs		\
	$(srcdir)/Cairo/SubpixelOrder.cs	\
	$(srcdir)/Cairo/Surface.cs		\
	$(srcdir)/Cairo/SurfacePattern.cs	\
	$(srcdir)/Cairo/SurfaceType.cs		\
	$(srcdir)/Cairo/SvgSurface.cs		\
	$(srcdir)/Cairo/SvgVersion.cs		\
	$(srcdir)/Cairo/TextExtents.cs		\
	$(srcdir)/Cairo/Win32Surface.cs		\
	$(srcdir)/Cairo/XcbSurface.cs		\
	$(srcdir)/Cairo/XlibSurface.cs		\
	$(srcdir)/Cms/Cms.cs			\
	$(srcdir)/Cms/CctTable.cs		\
	$(srcdir)/AsyncPixbufLoader.cs		\
	$(srcdir)/BeagleNotifier.cs		\
	$(srcdir)/BlockProcessor.cs		\
	$(srcdir)/BitConverter.cs		\
	$(srcdir)/PhotoArray.cs 		\
	$(srcdir)/CDExport.cs			\
	$(srcdir)/ColorDialog.cs		\
	$(srcdir)/ColorAdjustment.cs		\
	$(srcdir)/CompatFileChooser.cs		\
	$(srcdir)/ControlOverlay.cs		\
	$(srcdir)/Core.cs			\
	$(srcdir)/DateCommands.cs		\
	$(srcdir)/Db.cs				\
	$(srcdir)/Delay.cs			\
	$(srcdir)/DependentListStore.cs			\
	$(srcdir)/DirectoryAdaptor.cs		\
	$(srcdir)/DirectoryCollection.cs	\
	$(srcdir)/Editors/Editor.cs		\
	$(srcdir)/Editors/SoftFocus.cs		\
	$(srcdir)/Editors/Tilt.cs		\
	$(srcdir)/ExceptionDialog.cs		\
	$(srcdir)/ExportStore.cs		\
	$(srcdir)/Extensions/ExportMenuItemNode.cs	\
	$(srcdir)/Extensions/IExporter.cs	\
	$(srcdir)/Extensions/ITool.cs	\
	$(srcdir)/Extensions/MenuNode.cs	\
	$(srcdir)/Extensions/ToolMenuItemNode.cs	\
	$(srcdir)/Fader.cs			\
	$(srcdir)/FlickrExport.cs		\
	$(srcdir)/FlickrRemote.cs		\
	$(srcdir)/FileImportBackend.cs		\
	$(srcdir)/Filters/ChmodFilter.cs	\
	$(srcdir)/Filters/ColorFilter.cs	\
	$(srcdir)/Filters/FilterRequest.cs	\
	$(srcdir)/Filters/FilterSet.cs		\
	$(srcdir)/Filters/IFilter.cs		\
	$(srcdir)/Filters/JpegFilter.cs		\
	$(srcdir)/Filters/OrientationFilter.cs	\
	$(srcdir)/Filters/ResizeFilter.cs	\
	$(srcdir)/Filters/SharpFilter.cs	\
	$(srcdir)/Filters/TiltFilter.cs		\
	$(srcdir)/Filters/UniqueNameFilter.cs	\
	$(srcdir)/Filters/WhiteListFilter.cs	\
	$(srcdir)/FolderExport.cs		\
	$(srcdir)/FotkiRemote.cs		\
	$(srcdir)/FormClient.cs			\
	$(srcdir)/FullScreenView.cs		\
	$(srcdir)/GalleryRemote.cs		\
	$(srcdir)/GalleryExport.cs		\
	$(srcdir)/GdkGlx.cs			\
	$(srcdir)/GlTransition.cs		\
	$(srcdir)/GladeDialog.cs		\
	$(srcdir)/Global.cs			\
	$(srcdir)/GroupAdaptor.cs		\
	$(srcdir)/GroupSelector.cs		\
	$(srcdir)/Accelerometer.cs		\
	$(srcdir)/HigMessageDialog.cs		\
	$(srcdir)/Histogram.cs			\
	$(srcdir)/IBrowsableItem.cs		\
	$(srcdir)/IconView.cs			\
	$(srcdir)/ImageView.cs			\
	$(srcdir)/ImageTest.cs			\
	$(srcdir)/ImportBackend.cs		\
	$(srcdir)/ImportCommand.cs		\
	$(srcdir)/InfoBox.cs			\
	$(srcdir)/InfoDisplay.cs		\
	$(srcdir)/InfoOverlay.cs		\
	$(srcdir)/InternalProcess.cs		\
	$(srcdir)/IOChannel.cs			\
	$(srcdir)/IQueryCondition.cs		\
	$(srcdir)/ItemAction.cs			\
	$(srcdir)/Imaging/DCRawFile.cs		\
	$(srcdir)/Imaging/Bim.cs		\
	$(srcdir)/Imaging/Ciff.cs		\
	$(srcdir)/Imaging/Exif.cs		\
	$(srcdir)/Imaging/ImageFile.cs		\
	$(srcdir)/Imaging/IptcFile.cs		\
	$(srcdir)/Imaging/JpegFile.cs		\
	$(srcdir)/Imaging/JpegHeader.cs		\
	$(srcdir)/Imaging/JpegUtils.cs		\
	$(srcdir)/Imaging/MrwFile.cs		\
	$(srcdir)/Imaging/OrderedWriter.cs	\
	$(srcdir)/Imaging/PngFile.cs		\
	$(srcdir)/Imaging/PnmFile.cs		\
	$(srcdir)/Imaging/RafFile.cs		\
	$(srcdir)/Imaging/SvgFile.cs		\
	$(srcdir)/Imaging/X3fFile.cs		\
	$(srcdir)/Imaging/XmpFile.cs		\
	$(srcdir)/Imaging/Tiff.cs		\
	$(srcdir)/JobStore.cs			\
	$(srcdir)/Jobs/FSpotCompat.cs		\
	$(srcdir)/Jobs/IInstanceCriticalJob.cs	\
	$(srcdir)/Jobs/IJob.cs			\
	$(srcdir)/Jobs/IntervalHeap.cs		\
	$(srcdir)/Jobs/JobPriority.cs		\
	$(srcdir)/Jobs/JobStatus.cs		\
	$(srcdir)/Jobs/Scheduler.cs		\
	$(srcdir)/Jobs/SyncMetadataJob.cs	\
	$(srcdir)/LastRollDialog.cs		\
	$(srcdir)/Loupe.cs			\
	$(srcdir)/MainWindow.cs			\
	$(srcdir)/MetaStore.cs			\
	$(srcdir)/MetadataStore.cs		\
	$(srcdir)/OpenWithMenu.cs		\
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
	$(srcdir)/PicasaWebExport.cs		\
	$(srcdir)/PixbufLoader.cs		\
	$(srcdir)/PixbufUtils.cs		\
	$(srcdir)/PixbufCache.cs		\
	$(srcdir)/PixelBuffer.cs		\
	$(srcdir)/PreferenceDialog.cs		\
	$(srcdir)/Preferences.cs 		\
	$(srcdir)/PreviewPopup.cs 		\
	$(srcdir)/PrintDialog.cs		\
	$(srcdir)/ProgressDialog.cs		\
	$(srcdir)/QueuedSqliteDatabase.cs	\
	$(srcdir)/RepairDialog.cs		\
	$(srcdir)/RotateCommand.cs		\
	$(srcdir)/RollSet.cs			\
	$(srcdir)/RollStore.cs			\
	$(srcdir)/ScalingIconView.cs		\
	$(srcdir)/SendEmail.cs                  \
	$(srcdir)/SlideView.cs			\
	$(srcdir)/SingleView.cs			\
	$(srcdir)/StockIcons.cs			\
	$(srcdir)/SimpleCalendar.cs		\
	$(srcdir)/SmugMugExport.cs		\
	$(srcdir)/TagCommands.cs		\
	$(srcdir)/TagMenu.cs			\
	$(srcdir)/TagPopup.cs			\
	$(srcdir)/TagQueryWidget.cs		\
	$(srcdir)/TagSelectionWidget.cs		\
	$(srcdir)/TagStore.cs			\
	$(srcdir)/TagView.cs			\
	$(srcdir)/ThreadProgressDialog.cs	\
	$(srcdir)/ThumbnailCache.cs		\
	$(srcdir)/ThumbnailGenerator.cs		\
	$(srcdir)/Texture.cs			\
	$(srcdir)/TextureDisplay.cs             \
	$(srcdir)/Term.cs			\
	$(srcdir)/TimeAdaptor.cs		\
	$(srcdir)/TimeDialog.cs			\
	$(srcdir)/TipWindow.cs			\
	$(srcdir)/Unix.cs			\
	$(srcdir)/Updater.cs			\
	$(srcdir)/Util.cs			\
	$(srcdir)/Vector.cs			\
	$(srcdir)/ThumbnailCommand.cs		\
	$(srcdir)/QueryView.cs			\
	$(srcdir)/QueryWidget.cs		\
	$(srcdir)/ZoomUtils.cs			\
	$(srcdir)/GPhotoCamera.cs		\
	$(srcdir)/CameraSelectionDialog.cs	\
	$(srcdir)/CameraFileSelectionDialog.cs	\
	$(srcdir)/TagSelectionDialog.cs		\
	$(srcdir)/Widgets/CairoUtils.cs		\
	$(srcdir)/Widgets/CompositeUtils.cs	\
	$(srcdir)/Widgets/Dissolve.cs		\
	$(srcdir)/Widgets/FindBar.cs		\
	$(srcdir)/Widgets/GdkUtils.cs		\
	$(srcdir)/Widgets/IEffect.cs		\
	$(srcdir)/Widgets/ITransition.cs	\
	$(srcdir)/Widgets/ImageDisplay.cs	\
	$(srcdir)/Widgets/ImageInfo.cs		\
	$(srcdir)/Widgets/PanZoom.cs		\
	$(srcdir)/Widgets/Push.cs		\
	$(srcdir)/Widgets/Reveal.cs		\
	$(srcdir)/Widgets/ScrolledView.cs	\
	$(srcdir)/Widgets/SoftFocus.cs		\
	$(srcdir)/Widgets/TagEntry.cs		\
	$(srcdir)/Widgets/Tilt.cs		\
	$(srcdir)/Widgets/Wipe.cs		\
	$(srcdir)/XmpTagsImporter.cs		\
	$(srcdir)/main.cs

ASSEMBLIES = \
	$(LINK_DBUS)				\
	$(LINK_GPHOTO2)				\
	$(LINK_MONO_ADDINS)			\
	$(LINK_FLICKR)				\
	$(LINK_GLITZ)				\
	$(LINK_GOOGLE)				\
	$(LINK_KEYRING)				\
	$(LINK_SMUGMUG)				\
	$(LINK_TAO)				\
	$(LINK_SEMWEB)				\
	$(LINK_BEAGLE)				\
	-r:System.Data				\
	-r:System.Web				\
	-r:Mono.Data.SqliteClient		\
	-r:Mono.GetOptions			\
	-r:Mono.Posix				\
	-r:Mono.Security			\
	-r:ICSharpCode.SharpZipLib		\
	$(NUNIT_PKG)				\
	-pkg:gtkhtml-sharp-2.0			\
	-pkg:glade-sharp-2.0			\
	-pkg:gnome-vfs-sharp-2.0		\
	-pkg:gconf-sharp-2.0


RESOURCES =										\
	-resource:$(top_srcdir)/icons/f-spot-adjust-colors.png,f-spot-adjust-colors.png	\
	-resource:$(top_srcdir)/icons/f-spot-autocolor.png,f-spot-autocolor.png	\
	-resource:$(top_srcdir)/icons/f-spot-browse.png,f-spot-browse.png		\
	-resource:$(top_srcdir)/icons/f-spot-crop.png,f-spot-crop.png			\
	-resource:$(top_srcdir)/icons/f-spot-camera.png,f-spot-camera.png		\
	-resource:$(top_srcdir)/icons/f-spot-desaturate.png,f-spot-desaturate.png	\
	-resource:$(top_srcdir)/icons/f-spot-edit-image.png,f-spot-edit-image.png	\
	-resource:$(top_srcdir)/icons/f-spot-events.png,f-spot-events.png		\
	-resource:$(top_srcdir)/icons/f-spot-favorite.png,f-spot-favorite.png 		\
	-resource:$(top_srcdir)/icons/f-spot-hidden.png,f-spot-hidden.png 		\
	-resource:$(top_srcdir)/icons/f-spot-horizon.png,f-spot-horizon.png 		\
	-resource:$(top_srcdir)/icons/f-spot-imported-xmp-tags.png,f-spot-imported-xmp-tags.png	\
	-resource:$(top_srcdir)/icons/f-spot-loading.png,f-spot-loading.png		\
	-resource:$(top_srcdir)/icons/f-spot-new-tag.png,f-spot-new-tag.png		\
	-resource:$(top_srcdir)/icons/f-spot-not.png,f-spot-not.png			\
	-resource:$(top_srcdir)/icons/f-spot-other.png,f-spot-other.png			\
	-resource:$(top_srcdir)/icons/f-spot-people.png,f-spot-people.png 		\
	-resource:$(top_srcdir)/icons/f-spot-places.png,f-spot-places.png 		\
	-resource:$(top_srcdir)/icons/f-spot-question-mark.png,f-spot-question-mark.png \
	-resource:$(top_srcdir)/icons/f-spot-red-eye.png,f-spot-red-eye.png		\
	-resource:$(top_srcdir)/icons/f-spot-rotate-90.png,f-spot-rotate-90.png		\
	-resource:$(top_srcdir)/icons/f-spot-rotate-270.png,f-spot-rotate-270.png	\
	-resource:$(top_srcdir)/icons/f-spot-sepia.png,f-spot-sepia.png			\
	-resource:$(top_srcdir)/icons/f-spot-slideshow.png,f-spot-slideshow.png		\
	-resource:$(top_srcdir)/icons/f-spot-soft-focus.png,f-spot-soft-focus.png	\
	-resource:$(top_srcdir)/icons/f-spot-stock_near.png,f-spot-stock_near.png	\
	-resource:$(top_srcdir)/icons/f-spot-stock_far.png,f-spot-stock_far.png		\
	-resource:$(top_srcdir)/icons/f-spot-fullscreen.png,f-spot-fullscreen.png	\
	-resource:$(top_srcdir)/icons/f-spot-view-restore.png,f-spot-view-restore.png	\
	-resource:$(top_srcdir)/icons/f-spot-simple-white.css,f-spot-simple-white.css	\
	-resource:$(top_srcdir)/icons/f-spot-simple.css,f-spot-simple.css		\
	-resource:$(top_srcdir)/icons/f-spot.js,f-spot.js				\
	-resource:$(top_srcdir)/icons/f-spot-logo-tango.svg,f-spot-logo-tango.svg	\
	-resource:$(top_srcdir)/icons/f-spot-16.png,f-spot-16.png			\
	-resource:$(top_srcdir)/icons/f-spot-22.png,f-spot-22.png			\
	-resource:$(top_srcdir)/icons/f-spot-32.png,f-spot-32.png			\
	-resource:$(top_srcdir)/icons/f-spot-48.png,f-spot-48.png			\
	-resource:$(srcdir)/dces.rdf,dces.rdf						\
	-resource:$(srcdir)/f-spot.glade,f-spot.glade					\
	-resource:$(srcdir)/FSpot.addin.xml,FSpot.addin.xml

fspotlibdir = $(libdir)/f-spot
fspotlib_DATA = f-spot.exe.config	\
		f-spot.exe		

bin_SCRIPTS = f-spot

F_SPOT_CSFILES = $(F_SPOT_CSDISTFILES)	\
	Defines.cs			\
	AssemblyInfo.cs

f-spot.exe: $(F_SPOT_CSFILES) f-spot.glade dces.rdf
	$(CSC) -target:winexe -out:$@ $(EXTRAFLAGS) $(F_SPOT_CSFILES) $(ASSEMBLIES) $(RESOURCES)

all: f-spot.exe

EXTRA_DIST =					\
	$(F_SPOT_CSDISTFILES)			\
	dces.rdf				\
	f-spot.exe.config.in			\
	f-spot.glade				\
	f-spot.in				\
	FSpot.addin.xml


CLEANFILES =					\
	f-spot.exe.config			\
	f-spot.exe.mdb				\
	f-spot.exe				\
	f-spot


DISTCLEANFILES = 				\
	makefile

