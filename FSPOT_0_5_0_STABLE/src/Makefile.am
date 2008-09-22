include $(top_srcdir)/Makefile.include
UNSAFE = -unsafe
NOWARN = -nowarn:0169 -nowarn:0612 -nowarn:0414  
EXTRAFLAGS = -d:TEST_METADATA -d:BROKEN_RSVG -d:GSD_2_24 $(CSC_DEFINES)

CMS_CSDISTFILES =				\
	$(srcdir)/Cms/CctTable.cs		\
	$(srcdir)/Cms/ColorCIELab.cs		\
	$(srcdir)/Cms/ColorCIELCh.cs		\
	$(srcdir)/Cms/ColorCIExyY.cs		\
	$(srcdir)/Cms/ColorCIEXYZ.cs		\
	$(srcdir)/Cms/Format.cs			\
	$(srcdir)/Cms/GammaTable.cs		\
	$(srcdir)/Cms/IccColorSpace.cs		\
	$(srcdir)/Cms/Intent.cs			\
	$(srcdir)/Cms/NativeMethods.cs		\
	$(srcdir)/Cms/Profile.cs		\
	$(srcdir)/Cms/Transform.cs

CORE_CSDISTFILES =				\
	$(srcdir)/Core/Animator.cs		\
	$(srcdir)/Core/BrowsableEventArgs.cs	\
	$(srcdir)/Core/BrowsablePointer.cs	\
	$(srcdir)/Core/BrowsablePointerEventArgs.cs	\
	$(srcdir)/Core/Category.cs		\
	$(srcdir)/Core/DbItem.cs		\
	$(srcdir)/Core/Delay.cs			\
	$(srcdir)/Core/Tag.cs			\
	$(srcdir)/Core/Global.cs		\
	$(srcdir)/Core/IBrowsableItem.cs	\
	$(srcdir)/Core/IBrowsableItemChanges.cs	\
	$(srcdir)/Core/IBrowsableCollection.cs	\
	$(srcdir)/Core/IPreferenceBackend.cs	\
	$(srcdir)/Core/PhotoChanges.cs		\
	$(srcdir)/Core/PhotosChanges.cs		\
	$(srcdir)/Core/Roll.cs

QUERY_CSDISTFILES =				\
	$(srcdir)/Query/DateRange.cs		\
	$(srcdir)/Query/IOrderCondition.cs	\
	$(srcdir)/Query/IQueryCondition.cs	\
	$(srcdir)/Query/LogicalTerm.cs		\
	$(srcdir)/Query/OrderByTime.cs		\
	$(srcdir)/Query/RatingRange.cs		\
	$(srcdir)/Query/RollSet.cs		\
	$(srcdir)/Query/UntaggedCondition.cs

UTILS_CSDISTFILES =				\
	$(srcdir)/Utils/Cache.cs		\
	$(srcdir)/Utils/CairoUtils.cs		\
	$(srcdir)/Utils/ConsoleCrayon.cs	\
	$(srcdir)/Utils/DbUtils.cs		\
	$(srcdir)/Utils/GnomeUtil.cs		\
	$(srcdir)/Utils/GdkUtils.cs		\
	$(srcdir)/Utils/GtkUtil.cs		\
	$(srcdir)/Utils/Log.cs			\
	$(srcdir)/Utils/Unix.cs			\
	$(srcdir)/Utils/UriUtils.cs		\
	$(srcdir)/Utils/ScreenSaver.cs

JOBSCHEDULER_CSDISTFILES =				\
	$(srcdir)/JobScheduler/FSpotCompat.cs		\
	$(srcdir)/JobScheduler/IInstanceCriticalJob.cs	\
	$(srcdir)/JobScheduler/IJob.cs			\
	$(srcdir)/JobScheduler/IntervalHeap.cs		\
	$(srcdir)/JobScheduler/JobPriority.cs		\
	$(srcdir)/JobScheduler/JobStatus.cs		\
	$(srcdir)/JobScheduler/Scheduler.cs

WIDGETS_CSDISTFILES =				\
	$(srcdir)/Widgets/ComplexMenuItem.cs	\
	$(srcdir)/Widgets/CustomPrintWidget.cs	\
	$(srcdir)/Widgets/HighlightedBox.cs	\
	$(srcdir)/Widgets/MenuButton.cs		\
	$(srcdir)/Widgets/Rating.cs		\
	$(srcdir)/Widgets/SaneTreeView.cs	\
	$(srcdir)/Widgets/ScrolledView.cs

F_SPOT_CSDISTFILES =				\
	$(srcdir)/AsyncPixbufLoader.cs		\
	$(srcdir)/BlockProcessor.cs		\
	$(srcdir)/BitConverter.cs		\
	$(srcdir)/PhotoArray.cs 		\
	$(srcdir)/PhotoList.cs 		\
	$(srcdir)/ColorAdjustment/Adjustment.cs		\
	$(srcdir)/ColorAdjustment/AutoStretch.cs		\
	$(srcdir)/ColorAdjustment/Desaturate.cs		\
	$(srcdir)/ColorAdjustment/FullColorAdjustment.cs		\
	$(srcdir)/ColorAdjustment/SepiaTone.cs		\
	$(srcdir)/CompatFileChooser.cs		\
	$(srcdir)/ControlOverlay.cs		\
	$(srcdir)/Core.cs			\
	$(srcdir)/Core/ColorManagement.cs	\
	$(srcdir)/Core/Photo.cs			\
	$(srcdir)/Core/PhotoVersion.cs		\
	$(srcdir)/DateCommands.cs		\
	$(srcdir)/Db.cs				\
	$(srcdir)/DependentListStore.cs		\
	$(srcdir)/DirectoryAdaptor.cs		\
	$(srcdir)/DirectoryCollection.cs	\
	$(srcdir)/Editors/Editor.cs		\
	$(srcdir)/Editors/AutoStretchEditor.cs		\
	$(srcdir)/Editors/CropEditor.cs		\
	$(srcdir)/Editors/ColorEditor.cs		\
	$(srcdir)/Editors/DesaturateEditor.cs		\
	$(srcdir)/Editors/RedEyeEditor.cs		\
	$(srcdir)/Editors/SepiaEditor.cs		\
	$(srcdir)/Editors/SoftFocusEditor.cs		\
	$(srcdir)/Editors/TiltEditor.cs		\
	$(srcdir)/Editors/Old/OldEditor.cs		\
	$(srcdir)/ExportStore.cs		\
	$(srcdir)/Extensions/ExportMenuItemNode.cs	\
	$(srcdir)/Extensions/IExporter.cs	\
	$(srcdir)/Extensions/ICommand.cs	\
	$(srcdir)/Extensions/IMenuGenerator.cs	\
	$(srcdir)/Extensions/IService.cs	\
	$(srcdir)/Extensions/MenuNode.cs	\
	$(srcdir)/Extensions/OpenWithMenu.cs	\
	$(srcdir)/Extensions/CommandMenuItemNode.cs	\
	$(srcdir)/Extensions/ComplexMenuItemNode.cs	\
	$(srcdir)/Extensions/PhotoSelectionCondition.cs	\
	$(srcdir)/Extensions/PopupCommands.cs	\
	$(srcdir)/Extensions/ServiceNode.cs	\
	$(srcdir)/Extensions/ViewModeCondition.cs	\
	$(srcdir)/Fader.cs			\
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
	$(srcdir)/Filters/UniqueNameFilter.cs	\
	$(srcdir)/Filters/WhiteListFilter.cs	\
	$(srcdir)/FotkiRemote.cs		\
	$(srcdir)/FormClient.cs			\
	$(srcdir)/FullScreenView.cs		\
	$(srcdir)/GConfPreferenceBackend.cs	\
	$(srcdir)/GdkGlx.cs			\
	$(srcdir)/GlTransition.cs		\
	$(srcdir)/GroupAdaptor.cs		\
	$(srcdir)/GroupSelector.cs		\
	$(srcdir)/Accelerometer.cs		\
	$(srcdir)/Histogram.cs			\
	$(srcdir)/ImageView.cs			\
	$(srcdir)/ImageTest.cs			\
	$(srcdir)/ImportBackend.cs		\
	$(srcdir)/ImportCommand.cs		\
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
	$(srcdir)/Jobs/SyncMetadataJob.cs	\
	$(srcdir)/Jobs/CalculateHashJob.cs	\
	$(srcdir)/Loupe.cs			\
	$(srcdir)/MainWindow.cs			\
	$(srcdir)/MemorySurface.cs		\
	$(srcdir)/MetaStore.cs			\
	$(srcdir)/MetadataStore.cs		\
	$(srcdir)/NullPreferenceBackend.cs	\
	$(srcdir)/Operation.cs			\
	$(srcdir)/PhotoEventArgs.cs		\
	$(srcdir)/PhotoImageView.cs		\
	$(srcdir)/PhotoLoader.cs		\
	$(srcdir)/PhotoPopup.cs			\
	$(srcdir)/PhotoQuery.cs			\
	$(srcdir)/PhotoStore.cs			\
	$(srcdir)/PhotoTagMenu.cs		\
	$(srcdir)/PhotoVersionCommands.cs	\
	$(srcdir)/PhotoVersionMenu.cs		\
	$(srcdir)/PhotoView.cs			\
	$(srcdir)/PixbufLoader.cs		\
	$(srcdir)/PixbufUtils.cs		\
	$(srcdir)/PixbufCache.cs		\
	$(srcdir)/PixelBuffer.cs		\
	$(srcdir)/Preferences.cs 		\
	$(srcdir)/PrintDialog.cs		\
	$(srcdir)/PrintOperation.cs		\
	$(srcdir)/ProgressDialog.cs		\
	$(srcdir)/RatingFilter.cs		\
	$(srcdir)/QueuedSqliteDatabase.cs	\
	$(srcdir)/RotateCommand.cs		\
	$(srcdir)/RollStore.cs			\
	$(srcdir)/SendEmail.cs                  \
	$(srcdir)/SlideView.cs			\
	$(srcdir)/SingleView.cs			\
	$(srcdir)/SimpleCalendar.cs		\
	$(srcdir)/SoftFocus.cs		\
	$(srcdir)/TagCommands.cs		\
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
	$(srcdir)/UI.Dialog/EditExceptionDialog.cs	\
	$(srcdir)/UI.Dialog/ExceptionDialog.cs	\
	$(srcdir)/UI.Dialog/GladeDialog.cs	\
	$(srcdir)/UI.Dialog/HigMessageDialog.cs	\
	$(srcdir)/UI.Dialog/LastRollDialog.cs		\
	$(srcdir)/UI.Dialog/PreferenceDialog.cs		\
	$(srcdir)/UI.Dialog/SelectionRatioDialog.cs	\
	$(srcdir)/UI.Dialog/RepairDbDialog.cs	\
	$(srcdir)/Updater.cs			\
	$(srcdir)/UriCollection.cs		\
	$(srcdir)/Util.cs			\
	$(srcdir)/Vector.cs			\
	$(srcdir)/ThumbnailCommand.cs		\
	$(srcdir)/QueryWidget.cs		\
	$(srcdir)/ZoomUtils.cs			\
	$(srcdir)/GPhotoCamera.cs		\
	$(srcdir)/CameraSelectionDialog.cs	\
	$(srcdir)/CameraFileSelectionDialog.cs	\
	$(srcdir)/TagSelectionDialog.cs		\
	$(srcdir)/Widgets/CompositeUtils.cs	\
	$(srcdir)/Widgets/Dissolve.cs		\
	$(srcdir)/Widgets/EditorPage.cs		\
	$(srcdir)/Widgets/Filmstrip.cs		\
	$(srcdir)/Widgets/FindBar.cs		\
	$(srcdir)/Widgets/IEffect.cs		\
	$(srcdir)/Widgets/ITransition.cs	\
	$(srcdir)/Widgets/IconView.cs		\
	$(srcdir)/Widgets/ImageDisplay.cs	\
	$(srcdir)/Widgets/ImageInfo.cs		\
	$(srcdir)/Widgets/InfoBox.cs		\
	$(srcdir)/Widgets/MetadataDisplay.cs		\
	$(srcdir)/Widgets/PanZoom.cs		\
	$(srcdir)/Widgets/PreviewPopup.cs 	\
	$(srcdir)/Widgets/Push.cs		\
	$(srcdir)/Widgets/QueryView.cs		\
	$(srcdir)/Widgets/RatingMenuItem.cs	\
	$(srcdir)/Widgets/Reveal.cs		\
	$(srcdir)/Widgets/ScalingIconView.cs	\
	$(srcdir)/Widgets/Sidebar.cs		\
	$(srcdir)/Widgets/TagEntry.cs		\
	$(srcdir)/Widgets/TagMenu.cs		\
	$(srcdir)/Widgets/TagView.cs		\
	$(srcdir)/Widgets/TrayView.cs		\
	$(srcdir)/Widgets/ViewContext.cs		\
	$(srcdir)/Widgets/Wipe.cs		\
	$(srcdir)/XmpTagsImporter.cs		\
	$(srcdir)/main.cs

if !NOGCONF
GCONF_PKG = -pkg:gconf-sharp-2.0
endif

CMS_ASSEMBLIES = 				\
	$(NUNIT_PKG)				\
	-pkg:gtk-sharp-2.0

CORE_ASSEMBLIES =				\
	-pkg:gtk-sharp-2.0			\
	-pkg:gnome-vfs-sharp-2.0		\
	-r:Cms.dll				\
	-r:Mono.Posix				\
	-r:FSpot.Utils.dll

QUERY_ASSEMBLIES =				\
	-r:FSpot.Core.dll			\
	-r:FSpot.Utils.dll

UTILS_ASSEMBLIES =				\
	-pkg:gtk-sharp-2.0			\
	-pkg:gnome-sharp-2.0			\
	-r:Mono.Posix				\
	-r:Mono.Cairo				\
	$(LINK_GLITZ)				\
	$(GCONF_PKG)				\
	$(LINK_DBUS)

JOBSCHEDULER_ASSEMBLIES =			\
	-r:FSpot.Utils.dll

WIDGETS_ASSEMBLIES =				\
	$(LINK_MONO_ADDINS)			\
	-pkg:gtk-sharp-2.0			\
	-pkg:gnome-sharp-2.0			\
	-r:Mono.Posix				\
	-r:FSpot.Core.dll			\
	-r:FSpot.Utils.dll

F_SPOT_ASSEMBLIES = 				\
	$(LINK_DBUS)				\
	$(LINK_GPHOTO2)				\
	$(LINK_MONO_ADDINS)			\
	$(LINK_FLICKR)				\
	$(LINK_KEYRING)				\
	$(LINK_SMUGMUG)				\
	$(LINK_TAO)				\
	$(LINK_SEMWEB)				\
	-r:System.Data				\
	-r:System.Web				\
	-r:Mono.Data.SqliteClient		\
	-r:Mono.Posix				\
	-r:Mono.Security			\
	-r:Mono.Cairo				\
	-r:ICSharpCode.SharpZipLib		\
	-r:FSpot.Core.dll			\
	-r:FSpot.Query.dll			\
	-r:FSpot.Utils.dll			\
	-r:FSpot.JobScheduler.dll		\
	-r:FSpot.Widgets.dll			\
	-r:Cms.dll				\
	$(NUNIT_PKG)				\
	$(GCONF_PKG)				\
	$(GTKHTML_SHARP_PKG)			\
	-pkg:glade-sharp-2.0			\
	-pkg:gnome-sharp-2.0			\
	-pkg:gnome-vfs-sharp-2.0

F_SPOT_DISTRESOURCES =			\
	$(srcdir)/dces.rdf		\
	$(srcdir)/f-spot.glade		\
	$(srcdir)/FSpot.addin.xml

F_SPOT_RESOURCES =					\
	$(F_SPOT_DISTRESOURCES)				\
	$(top_srcdir)/COPYING				\
	$(top_srcdir)/icons/f-spot-not.png		\
	$(top_srcdir)/icons/f-spot-logo.svg		\
	$(top_srcdir)/icons/flower.png

RESOURCES = $(foreach res,$(F_SPOT_RESOURCES), $(addprefix -resource:,$(res)),$(notdir $(res)))

fspotlibdir = $(libdir)/f-spot
fspotlib_DATA = f-spot.exe.config	\
		f-spot.exe		\
		Cms.dll.config		\
		Cms.dll			\
		FSpot.Utils.dll		\
		FSpot.Utils.dll.config	\
		FSpot.Query.dll		\
		FSpot.Core.dll		\
		FSpot.JobScheduler.dll	\
		FSpot.Widgets.dll

bin_SCRIPTS = f-spot

CMS_CSFILES = $(CMS_CSDISTFILES)

CORE_CSFILES = $(CORE_CSDISTFILES)	\
	Core/Defines.cs	

QUERY_CSFILES = $(QUERY_CSDISTFILES)

UTILS_CSFILES = $(UTILS_CSDISTFILES)

JOBSCHEDULER_CSFILES = $(JOBSCHEDULER_CSDISTFILES)

WIDGETS_CSFILES = $(WIDGETS_CSDISTFILES)

F_SPOT_CSFILES = $(F_SPOT_CSDISTFILES)	\
	AssemblyInfo.cs

Cms.dll: $(CMS_CSFILES)
	@echo -e "\n*** Compiling $@"
	$(CSC_LIB) -out:$@ $(EXTRAFLAGS) $(UNSAFE) $(CMS_CSFILES) $(CMS_ASSEMBLIES)

FSpot.Core.dll: $(CORE_CSFILES) FSpot.Utils.dll Cms.dll
	@echo -e "\n*** Compiling $@"
	$(CSC_LIB) -out:$@ $(EXTRAFLAGS) $(CORE_CSFILES) $(CORE_ASSEMBLIES)

FSpot.Query.dll: $(QUERY_CSFILES) FSpot.Utils.dll FSpot.Core.dll
	@echo -e "\n*** Compiling $@"
	$(CSC_LIB) -out:$@ $(EXTRAFLAGS) $(QUERY_CSFILES) $(QUERY_ASSEMBLIES)

FSpot.Utils.dll: $(UTILS_CSFILES)
	@echo -e "\n*** Compiling $@"
	$(CSC_LIB) -out:$@ $(EXTRAFLAGS) $(UTILS_CSFILES) $(UTILS_ASSEMBLIES)

FSpot.JobScheduler.dll: $(JOBSCHEDULER_CSFILES) FSpot.Utils.dll
	@echo -e "\n*** Compiling $@"
	$(CSC_LIB) -out:$@ $(EXTRAFLAGS) $(JOBSCHEDULER_CSFILES) $(JOBSCHEDULER_ASSEMBLIES)

FSpot.Widgets.dll: $(WIDGETS_CSFILES) FSpot.Utils.dll FSpot.Core.dll
	@echo -e "\n*** Compiling $@"
	$(CSC_LIB) -out:$@ $(EXTRAFLAGS) $(WIDGETS_CSFILES) $(WIDGETS_ASSEMBLIES)

f-spot.exe: $(F_SPOT_CSFILES) FSpot.Utils.dll FSpot.Core.dll FSpot.Query.dll FSpot.JobScheduler.dll FSpot.Widgets.dll Cms.dll $(F_SPOT_RESOURCES)
	@echo -e "\n*** Compiling $@"
	$(CSC) -target:winexe -out:$@ $(EXTRAFLAGS) $(UNSAFE) $(NOWARN) $(NUNIT_DEFINES) $(F_SPOT_CSFILES) $(F_SPOT_ASSEMBLIES) $(RESOURCES)

all: f-spot.exe

EXTRA_DIST =					\
	$(UTILS_CSDISTFILES)			\
	$(CMS_CSDISTFILES)			\
	$(CORE_CSDISTFILES)			\
	$(QUERY_CSDISTFILES)			\
	$(JOBSCHEDULER_CSDISTFILES)		\
	$(WIDGETS_CSDISTFILES)			\
	$(F_SPOT_CSDISTFILES)			\
	$(F_SPOT_DISTRESOURCES)			\
	f-spot.exe.config.in			\
	f-spot.in				\
	FSpot.Utils.dll.config			\
	Cms.dll.config.in

CLEANFILES =					\
	f-spot.exe.config			\
	f-spot.exe.mdb				\
	f-spot.exe				\
	f-spot					\
	Cms.dll.mdb				\
	Cms.dll					\
	FSpot.Utils.dll.mdb			\
	FSpot.Utils.dll				\
	FSpot.Query.dll				\
	FSpot.Query.dll.mdb			\
	FSpot.Core.dll.mdb			\
	FSpot.Core.dll				\
	FSpot.JobScheduler.dll			\
	FSpot.JobScheduler.dll.mdb		\
	FSpot.Widgets.dll			\
	FSpot.Widgets.dll.mdb			\
	Cms.dll.config


DISTCLEANFILES = 				\
	Makefile

