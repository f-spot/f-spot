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
	FSpot.Core/Delay.cs \
	FSpot.Core/FileBrowsableItem.cs \
	FSpot.Core/Global.cs \
	FSpot.Core/IBrowsableCollection.cs \
	FSpot.Core/IBrowsableItem.cs \
	FSpot.Core/IBrowsableItemChanges.cs \
	FSpot.Core/IBrowsableItemComparer.cs \
	FSpot.Core/IBrowsableItemExtensions.cs \
	FSpot.Core/IBrowsableItemVersion.cs \
	FSpot.Core/IBrowsableItemVersionable.cs \
	FSpot.Core/ILoadable.cs \
	FSpot.Core/PhotoChanges.cs \
	FSpot.Core/PhotosChanges.cs \
	FSpot.Core/Roll.cs \
	FSpot.Core/Tag.cs

RESOURCES = FSpot.Core.addins

module_DATA = FSpot.Core.addins

include $(top_srcdir)/build/build.mk

EXTRA_DIST += FSpot.Core.dll.config
module_SCRIPTS += FSpot.Core.dll.config
