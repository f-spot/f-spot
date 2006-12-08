CSC = gmcs

LIBSMUGMUGNET_CSFILES =				\
	$(srcdir)/SmugMugApi.cs 		\
	$(srcdir)/NoCheckCertificatePolicy.cs 		

	

LIBSMUGMUGNETlibdir = $(libdir)/f-spot
LIBSMUGMUGNETlib_DATA =		\
	SmugMugNet.dll


SmugMugNet.dll: $(LIBSMUGMUGNET_CSFILES)
	$(CSC) -r:System.Web.dll -out:$@ /target:library $(LIBSMUGMUGNET_CSFILES)

all: SmugMugNet.dll

EXTRA_DIST =							\
	$(LIBSMUGMUGNET_CSFILES)	

CLEANFILES =							\
	SmugMugNet.dll 

