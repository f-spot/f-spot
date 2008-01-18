include $(top_srcdir)/Makefile.include
EXTRAFLAGS = -unsafe -nowarn:0169 -nowarn:0612 -nowarn:0414 -d:TEST_METADATA -d:BROKEN_RSVG $(NUNIT_DEFINES) $(BEAGLE_DEFINES) $(CSC_DEFINES)

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
	$(srcdir)/Category.cs			\
	$(srcdir)/CDExport.cs			\
	$(srcdir)/ColorDialog.cs		\
	$(srcdir)/ColorAdjustment.cs		\
	$(srcdir)/CompatFileChooser.cs		\
	$(srcdir)/ControlOverlay.cs		\
	$(srcdir)/Core/DbItem.cs		\
	$(srcdir)/Core/Tag.cs			\
	$(srcdir)/Core.cs			\
	$(srcdir)/DateCommands.cs		\
	$(srcdir)/Db.cs				\
	$(srcdir)/DBusProxy.cs			\
	$(srcdir)/Delay.cs			\
	$(srcdir)/DependentListStore.cs		\
	$(srcdir)/DirectoryAdaptor.cs		\
	$(srcdir)/DirectoryCollection.cs	\
	$(srcdir)/Editors/Editor.cs		\
	$(srcdir)/Editors/SoftFocus.cs		\
	$(srcdir)/Editors/Tilt.cs		\
	$(srcdir)/ExceptionDialog.cs		\
	$(srcdir)/ExportStore.cs		\
	$(srcdir)/Extensions/ExportMenuItemNode.cs	\
	$(srcdir)/Extensions/IExporter.cs	\
	$(srcdir)/Extensions/ICommand.cs	\
	$(srcdir)/Extensions/IMenuGenerator.cs	\
	$(srcdir)/Extensions/MenuNode.cs	\
	$(srcdir)/Extensions/OpenWithMenu.cs	\
	$(srcdir)/Extensions/CommandMenuItemNode.cs	\
	$(srcdir)/Extensions/PopupCommands.cs	\
	$(srcdir)/Fader.cs			\
	$(srcdir)/FlickrExport.cs		\
	$(srcdir)/FlickrRemote.cs		\
	$(srcdir)/FileImportBackend.cs		\
	$(srcdir)/FileBrowsableItem.cs		\
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
	$(srcdir)/ImageView.cs			\
	$(srcdir)/ImageTest.cs			\
	$(srcdir)/ImportBackend.cs		\
	$(srcdir)/ImportCommand.cs		\
	$(srcdir)/InfoBox.cs			\
	$(srcdir)/InfoDisplay.cs		\
	$(srcdir)/InfoOverlay.cs		\
	$(srcdir)/InternalProcess.cs		\
	$(srcdir)/IOChannel.cs			\
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
	$(srcdir)/Operation.cs			\
	$(srcdir)/PhotoImageView.cs		\
	$(srcdir)/PhotoLoader.cs		\
	$(srcdir)/PhotoPopup.cs			\
	$(srcdir)/PhotoQuery.cs			\
	$(srcdir)/PhotoStore.cs			\
	$(srcdir)/PhotoTagMenu.cs		\
	$(srcdir)/PhotoVersion.cs		\
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
	$(srcdir)/Query/DateRange.cs		\
	$(srcdir)/Query/IQueryCondition.cs	\
	$(srcdir)/Query/LogicalTerm.cs		\
	$(srcdir)/Query/RatingRange.cs		\
	$(srcdir)/Query/RollSet.cs		\
	$(srcdir)/Query/UntaggedCondition.cs	\
	$(srcdir)/RatingFilter.cs		\
	$(srcdir)/RatingMenu.cs			\
	$(srcdir)/QueuedSqliteDatabase.cs	\
	$(srcdir)/RepairDialog.cs		\
	$(srcdir)/RotateCommand.cs		\
	$(srcdir)/RollStore.cs			\
	$(srcdir)/SendEmail.cs                  \
	$(srcdir)/SlideView.cs			\
	$(srcdir)/SingleView.cs			\
	$(srcdir)/SimpleCalendar.cs		\
	$(srcdir)/SmugMugExport.cs		\
	$(srcdir)/TagCommands.cs		\
	$(srcdir)/TagMenu.cs			\
	$(srcdir)/TagPopup.cs			\
	$(srcdir)/TagQueryWidget.cs		\
	$(srcdir)/TagSelectionWidget.cs		\
	$(srcdir)/TagStore.cs			\
	$(srcdir)/ThreadProgressDialog.cs	\
	$(srcdir)/ThumbnailCache.cs		\
	$(srcdir)/ThumbnailGenerator.cs		\
	$(srcdir)/Texture.cs			\
	$(srcdir)/TextureDisplay.cs             \
	$(srcdir)/Term.cs			\
	$(srcdir)/TimeAdaptor.cs		\
	$(srcdir)/TimeDialog.cs			\
	$(srcdir)/TipWindow.cs			\
	$(srcdir)/UI.Dialog/AboutDialog.cs	\
	$(srcdir)/Unix.cs			\
	$(srcdir)/Updater.cs			\
	$(srcdir)/UriCollection.cs		\
	$(srcdir)/Utils/GtkUtil.cs		\
	$(srcdir)/Utils/ScreenSaver.cs		\
	$(srcdir)/Util.cs			\
	$(srcdir)/Vector.cs			\
	$(srcdir)/ThumbnailCommand.cs		\
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
	$(srcdir)/Widgets/IconView.cs		\
	$(srcdir)/Widgets/ImageDisplay.cs	\
	$(srcdir)/Widgets/ImageInfo.cs		\
	$(srcdir)/Widgets/PanZoom.cs		\
	$(srcdir)/Widgets/Push.cs		\
	$(srcdir)/Widgets/QueryView.cs		\
	$(srcdir)/Widgets/Rating.cs		\
	$(srcdir)/Widgets/Reveal.cs		\
	$(srcdir)/Widgets/SaneTreeView.cs	\
	$(srcdir)/Widgets/ScalingIconView.cs	\
	$(srcdir)/Widgets/ScrolledView.cs	\
	$(srcdir)/Widgets/SoftFocus.cs		\
	$(srcdir)/Widgets/TagEntry.cs		\
	$(srcdir)/Widgets/TagView.cs		\
	$(srcdir)/Widgets/Tilt.cs		\
	$(srcdir)/Widgets/TrayView.cs		\
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
	-r:Mono.Posix				\
	-r:Mono.Security			\
	-r:ICSharpCode.SharpZipLib		\
	$(NUNIT_PKG)				\
	-pkg:gtkhtml-sharp-2.0			\
	-pkg:glade-sharp-2.0			\
	-pkg:gnome-vfs-sharp-2.0		\
	-pkg:gconf-sharp-2.0


RESOURCES =										\
-resource:$(top_srcdir)/icons/f-spot-simple-white.css,f-spot-simple-white.css	\
	-resource:$(top_srcdir)/icons/f-spot-simple.css,f-spot-simple.css		\
	-resource:$(top_srcdir)/icons/f-spot.js,f-spot.js				\
	-resource:$(top_srcdir)/icons/f-spot-not.png,f-spot-not.png			\
	-resource:$(top_srcdir)/icons/f-spot-logo.svg,f-spot-logo.svg			\
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

f-spot.exe: $(F_SPOT_CSFILES) f-spot.glade dces.rdf FSpot.addin.xml
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

