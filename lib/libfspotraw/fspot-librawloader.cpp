//
// fspot-librawloader.cpp
//
// Author(s)
//	Ruben Vermeersch  <ruben@savanne.be>
//
// This is free software. See COPYING for details
//

#include "fspot-librawloader.h"

#include <libraw/libraw.h>

G_DEFINE_TYPE (FSpotLibrawLoader, fspot_librawloader, G_TYPE_OBJECT);

static void
fspot_librawloader_set_property (GObject	  *object,
								 guint		   property_id,
								 const GValue *value,
								 GParamSpec   *pspec);
static void
fspot_librawloader_get_property (GObject	  *object,
								 guint		   property_id,
								 GValue		  *value,
								 GParamSpec   *pspec);
static void fspot_librawloader_dispose (GObject *object);
static void fspot_librawloader_finalize (GObject *object);

static void open_if_needed (FSpotLibrawLoader *self);

#define FSPOT_LIBRAWLOADER_GET_PRIVATE(obj) (G_TYPE_INSTANCE_GET_PRIVATE ((obj), FSPOT_TYPE_LIBRAWLOADER, FSpotLibrawLoaderPriv))

struct _FSpotLibrawLoaderPriv
{
	LibRaw *raw_proc;
	gchar *filename;
	double progress;

	gboolean opened;
};

static void
fspot_librawloader_class_init (FSpotLibrawLoaderClass *klass)
{
	GObjectClass *gobject_class = G_OBJECT_CLASS (klass);
	GParamSpec *pspec;

	gobject_class->set_property = fspot_librawloader_set_property;
	gobject_class->get_property = fspot_librawloader_get_property;
	gobject_class->dispose      = fspot_librawloader_dispose;
	gobject_class->finalize     = fspot_librawloader_finalize;

	pspec = g_param_spec_string ("filename",
								 "The full path of the RAW files.",
								 "Set filename",
								 "",
							     (GParamFlags) (G_PARAM_READWRITE | G_PARAM_CONSTRUCT_ONLY));
	g_object_class_install_property (gobject_class,
									 PROP_FILENAME,
									 pspec);

	pspec = g_param_spec_double ("progress",
								 "The progress of loading the full size.",
								 "Loading progress",
								 0.0,
								 1.0,
								 0.0,
								 G_PARAM_READABLE);
	g_object_class_install_property (gobject_class,
									 PROP_PROGRESS,
									 pspec);

	g_type_class_add_private (klass, sizeof (FSpotLibrawLoaderPriv));
}

static void
fspot_librawloader_init (FSpotLibrawLoader *self)
{
	self->priv = FSPOT_LIBRAWLOADER_GET_PRIVATE (self);

	self->priv->raw_proc = new LibRaw;
	self->priv->opened = false;
	self->priv->progress = 0;
}

static void
fspot_librawloader_set_property (GObject	  *object,
								 guint		   property_id,
								 const GValue *value,
								 GParamSpec   *pspec)
{
	FSpotLibrawLoader *self = FSPOT_LIBRAWLOADER (object);

	switch (property_id)
	{
		case PROP_FILENAME:
			g_free (self->priv->filename);
			self->priv->filename = g_value_dup_string (value);
			break;

		default:
			G_OBJECT_WARN_INVALID_PROPERTY_ID (object, property_id, pspec);
			break;
	}
}

static void
fspot_librawloader_get_property (GObject	  *object,
								 guint		   property_id,
								 GValue		  *value,
								 GParamSpec   *pspec)
{
	FSpotLibrawLoader *self = FSPOT_LIBRAWLOADER (object);

	switch (property_id)
	{
		case PROP_FILENAME:
			g_value_set_string (value, self->priv->filename);
			break;

		default:
			G_OBJECT_WARN_INVALID_PROPERTY_ID (object, property_id, pspec);
			break;
	}
}

static void
fspot_librawloader_dispose (GObject *object)
{
	FSpotLibrawLoader *self = FSPOT_LIBRAWLOADER (object);

	self->priv->raw_proc->recycle ();

	G_OBJECT_CLASS (fspot_librawloader_parent_class)->dispose (object);
}

static void
fspot_librawloader_finalize (GObject *object)
{
	FSpotLibrawLoader *self = FSPOT_LIBRAWLOADER (object);

	delete self->priv->raw_proc;
	g_free (self->priv->filename);

	G_OBJECT_CLASS (fspot_librawloader_parent_class)->finalize (object);
}

GdkPixbuf *
fspot_librawloader_load_thumbnail (FSpotLibrawLoader *self)
{
	int result;
	libraw_processed_image_t *image = NULL;
	GdkPixbufLoader *loader = NULL;
	GdkPixbuf *pixbuf = NULL;
	GError *error = NULL;

	open_if_needed (self);

	self->priv->raw_proc->unpack_thumb ();
	image = self->priv->raw_proc->dcraw_make_mem_thumb (&result);
	g_assert (result == 0 && image != NULL);
	g_assert (image->type == LIBRAW_IMAGE_JPEG);

	loader = gdk_pixbuf_loader_new ();
	gdk_pixbuf_loader_write (loader, image->data, image->data_size, NULL);
	gdk_pixbuf_loader_close (loader, &error);
	g_assert (error == NULL);

	pixbuf = gdk_pixbuf_copy (gdk_pixbuf_loader_get_pixbuf (loader));

	return pixbuf;
}

GdkPixbuf *
fspot_librawloader_load_full (FSpotLibrawLoader *self)
{
	int result;
	libraw_processed_image_t *image = NULL;
	GdkPixbuf *pixbuf = NULL;

	open_if_needed (self);

	self->priv->raw_proc->unpack ();
	self->priv->raw_proc->dcraw_process ();
	image = self->priv->raw_proc->dcraw_make_mem_image (&result);
	g_assert (result == 0 && image != NULL);
	g_assert (image->type == LIBRAW_IMAGE_BITMAP);

	pixbuf = gdk_pixbuf_new_from_data (image->data,
									   GDK_COLORSPACE_RGB,
									   false,
									   image->bits,
									   image->width,
									   image->height,
									   image->width * 3, /* rowstride */
									   (GdkPixbufDestroyNotify) g_free,
									   NULL);

	return pixbuf;
}

static void
open_if_needed (FSpotLibrawLoader *self)
{
	if (!self->priv->opened) {
		int result = self->priv->raw_proc->open_file (self->priv->filename);
		g_assert (result == 0);

		self->priv->opened = true;
	}
}
