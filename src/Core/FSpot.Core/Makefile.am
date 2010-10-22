ASSEMBLY = FSpot.Core
TARGET = library
LINK = $(REF_FSPOT_CORE)

SOURCES =  \
	FSpot.Core/BrowsableCollectionProxy.cs \
	FSpot.Core/BrowsableEventArgs.cs \
	FSpot.Core/BrowsablePointer.cs \
	FSpot.Core/BrowsablePointerChangedEventArgs.cs \
	FSpot.Core/Category.cs \
	FSpot.Core/DbItem.cs \
	FSpot.Core/Defines.cs \
	FSpot.Core/FilePhoto.cs \
	FSpot.Core/Global.cs \
	FSpot.Core/IBrowsableCollection.cs \
	FSpot.Core/IBrowsableItemChanges.cs \
	FSpot.Core/IInvalidPhotoCheck.cs \
	FSpot.Core/ILoadable.cs \
	FSpot.Core/IPhoto.cs \
	FSpot.Core/IPhotoComparer.cs \
	FSpot.Core/IPhotoExtensions.cs \
	FSpot.Core/IPhotoVersion.cs \
	FSpot.Core/IPhotoVersionable.cs \
	FSpot.Core/PhotoChanges.cs \
	FSpot.Core/PhotoList.cs \
	FSpot.Core/PhotosChanges.cs \
	FSpot.Core/Roll.cs \
	FSpot.Core/Tag.cs

RESOURCES = FSpot.Core.addins

module_DATA = FSpot.Core.addins

include $(top_srcdir)/build/build.mk

EXTRA_DIST += FSpot.Core.dll.config
module_SCRIPTS += FSpot.Core.dll.config
