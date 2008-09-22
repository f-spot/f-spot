#include <gdk/gdk.h>
#include <cairo.h>

const cairo_user_data_key_t pixel_key;
const cairo_user_data_key_t format_key;

cairo_surface_t *
f_image_surface_create (cairo_format_t format, int width, int height)
{
	int size;
	cairo_surface_t *surface;
	unsigned char *pixels;

	switch (format) {
	case CAIRO_FORMAT_ARGB32:
	case CAIRO_FORMAT_RGB24:
		size = 4;
		break;
	case CAIRO_FORMAT_A8:
		size = 8;
		break;
	case CAIRO_FORMAT_A1:
		size = 1;
		break;
	}

	pixels = g_malloc (width * height * size);
	surface = cairo_image_surface_create_for_data (pixels,
						       format,
						       width,
						       height,
						       width * size);

	cairo_surface_set_user_data (surface, &pixel_key, pixels, g_free);
	cairo_surface_set_user_data (surface, &format_key, GINT_TO_POINTER (format), NULL);

	return surface;
}

void  *
f_image_surface_get_data (cairo_surface_t *surface)
{
	return cairo_surface_get_user_data (surface, &pixel_key);
}

cairo_format_t
f_image_surface_get_format (cairo_surface_t *surface)
{
	return GPOINTER_TO_INT (cairo_surface_get_user_data (surface, &format_key));
}

int
f_image_surface_get_width (cairo_surface_t *surface)
{
	return cairo_image_surface_get_width (surface);
}

int
f_image_surface_get_height (cairo_surface_t *surface)
{
	return cairo_image_surface_get_height (surface);	
}

