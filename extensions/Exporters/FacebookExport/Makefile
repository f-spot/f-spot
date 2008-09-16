NAME = FacebookExport

SOURCES = \
	FacebookExport.cs

PACKAGES = \
	-pkg:glade-sharp-2.0 \
	-pkg:gtk-sharp-2.0 \
	-pkg:f-spot

ASSEMBLIES = \
	-r:Mono.Posix			\
	-r:Mono.Facebook.dll

RESOURCES = \
	-resource:$(NAME).glade \
	-resource:$(NAME).addin.xml

ASSEMBLY = $(NAME).dll

FACEBOOK_SOURCES =				\
	Mono.Facebook/Album.cs			\
	Mono.Facebook/AssemblyInfo.cs		\
	Mono.Facebook/Error.cs			\
	Mono.Facebook/Event.cs			\
	Mono.Facebook/FacebookException.cs	\
	Mono.Facebook/FacebookParam.cs		\
	Mono.Facebook/FacebookSession.cs	\
	Mono.Facebook/Friend.cs			\
	Mono.Facebook/FriendInfo.cs		\
	Mono.Facebook/Group.cs			\
	Mono.Facebook/Location.cs		\
	Mono.Facebook/Notification.cs		\
	Mono.Facebook/PeopleList.cs		\
	Mono.Facebook/Photo.cs			\
	Mono.Facebook/Responses.cs		\
	Mono.Facebook/SessionWrapper.cs		\
	Mono.Facebook/Tag.cs			\
	Mono.Facebook/User.cs			\
	Mono.Facebook/Util.cs

all: $(ASSEMBLY)

Mono.Facebook.dll: $(FACEBOOK_SOURCES)
	gmcs -target:library -out:$@ $(FACEBOOK_SOURCES)

install: all
	cp *.dll ~/.gnome2/f-spot/addins/

mpack: $(ASSEMBLY)
	mautil p $(ASSEMBLY)

$(ASSEMBLY): $(SOURCES) $(NAME).addin.xml Mono.Facebook.dll
	gmcs -target:library -out:$@ $(SOURCES) $(PACKAGES) $(ASSEMBLIES) $(RESOURCES)

clean:
	rm -f $(ASSEMBLY) *~ *.bak *.mpack *.gladep *.dll
