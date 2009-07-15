//
// fspot-librawloader.h
//
// Author(s)
//	Ruben Vermeersch  <ruben@savanne.be>
//
// This is free software. See COPYING for details
//

#ifndef __FSPOT_LIBRAWLOADER_H__
#define __FSPOT_LIBRAWLOADER_H__

#include <glib-object.h>
#include <gdk/gdk.h>

G_BEGIN_DECLS

#define FSPOT_TYPE_LIBRAWLOADER				(fspot_librawloader_get_type ())
#define FSPOT_LIBRAWLOADER(obj)				(G_TYPE_CHECK_INSTANCE_CAST ((obj), FSPOT_TYPE_LIBRAWLOADER, FSpotLibrawLoader))
#define FSPOT_IS_LIBRAWLOADER(obj)			(G_TYPE_CHECK_INSTANCE_TYPE ((obj), FSPOT_TYPE_LIBRAWLOADER))
#define FSPOT_LIBRAWLOADER_CLASS(klass)		(G_TYPE_CHECK_CLASS_CAST ((klass), FSPOT_TYPE_LIBRAWLOADER, FSpotLibrawLoaderClass))
#define FSPOT_IS_LIBRAWLOADER_CLASS(klass)	(G_TYPE_CHECK_CLASS_TYPE ((klass), FSPOT_TYPE_LIBRAWLOADER))
#define FSPOT_LIBRAWLOADER_GET_CLASS(obj)	(G_TYPE_INSTANCE_GET_CLASS ((obj), FSPOT_TYPE_LIBRAWLOADER, FSpotLibrawLoaderClass))

enum {
	PROP_0,

	PROP_FILENAME,
	PROP_PROGRESS
};

typedef struct _FSpotLibrawLoader		FSpotLibrawLoader;
typedef struct _FSpotLibrawLoaderClass	FSpotLibrawLoaderClass;
typedef struct _FSpotLibrawLoaderPriv	FSpotLibrawLoaderPriv;

struct _FSpotLibrawLoader
{
	GObject parent_instance;

	/*< private >*/
	FSpotLibrawLoaderPriv *priv;
};

struct _FSpotLibrawLoaderClass
{
	GObjectClass parent_class;

};

GType fspot_librawloader_get_type (void);
GdkPixbuf * fspot_librawloader_load_thumbnail (FSpotLibrawLoader *self);
GdkPixbuf * fspot_librawloader_load_full (FSpotLibrawLoader *self);

G_END_DECLS

#endif /* __FSPOT_LIBRAWLOADER_H__ */
