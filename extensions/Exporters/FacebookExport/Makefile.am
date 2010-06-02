ASSEMBLY = FSpot.Exporters.FacebookExport
TARGET = library
LINK = $(REF_FSPOT_EXTENSION_FACEBOOKEXPORT)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES = \
	FacebookExport.cs \
	FacebookExportDialog.cs \
	FacebookTagPopup.cs \
	FindNullableClashes.cs \
	Mono.Facebook/facebook.cs \
	Mono.Facebook/Album.cs \
	Mono.Facebook/Error.cs \
	Mono.Facebook/Event.cs \
	Mono.Facebook/FacebookException.cs \
	Mono.Facebook/FacebookParam.cs \
	Mono.Facebook/FacebookSession.cs \
	Mono.Facebook/Friend.cs \
	Mono.Facebook/FriendInfo.cs \
	Mono.Facebook/Group.cs \
	Mono.Facebook/Location.cs \
	Mono.Facebook/Notification.cs \
	Mono.Facebook/PeopleList.cs \
	Mono.Facebook/Photo.cs \
	Mono.Facebook/Responses.cs \
	Mono.Facebook/SessionInfo.cs \
	Mono.Facebook/SessionWrapper.cs \
	Mono.Facebook/Tag.cs \
	Mono.Facebook/User.cs \
	Mono.Facebook/Util.cs
RESOURCES = \
	FacebookExport.addin.xml \
	FacebookExport.ui

ASSEMBLY_INFO_SOURCE = Mono.Facebook/AssemblyInfo.cs

include $(top_srcdir)/build/build.mk
