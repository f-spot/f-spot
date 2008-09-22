/* -*- Mode: C; indent-tabs-mode: t; c-basic-offset: 8; tab-width: 8 -*- */
/* f-pixbuf-utils.c
 *
 * Copyright (C) 2001, 2002, 2003 The Free Software Foundation, Inc.
 * Copyright (C) 2003 Ettore Perazzoli
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License as
 * published by the Free Software Foundation; either version 2 of the
 * License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public
 * License along with this program; if not, write to the
 * Free Software Foundation, Inc., 59 Temple Place - Suite 330,
 * Boston, MA 02111-1307, USA.
 *
 * Author: Paolo Bacchilega <paolo.bacch@tin.it>
 *
 * Adapted by Ettore Perazzoli <ettore@perazzoli.org>
 */

/* Some bits are based upon the GIMP source code, the original copyright
 * note follows:
 *
 * The GIMP -- an image manipulation program
 * Copyright (C) 1995 Spencer Kimball and Peter Mattis
 *
 */


#include <config.h>

#include "f-pixbuf-utils.h"
#include "f-utils.h"

#include <string.h>
#include <math.h>
#include <stdio.h>
#include <errno.h>
#include <gdk/gdkcairo.h>
#include "f-image-surface.h"


/* Helper functions.  */

static unsigned char
apply_brightness_and_contrast (unsigned char u_value,
			       float brightness,
			       float contrast)
{
	float  nvalue;
	double power;
	float  value;

	value = (float) u_value / 255.0;

	/* apply brightness */
	if (brightness < 0.0)
		value = value * (1.0 + brightness);
	else
		value = value + ((1.0 - value) * brightness);
	
	/* apply contrast */
	if (contrast < 0.0) {
		if (value > 0.5)
			nvalue = 1.0 - value;
		else
			nvalue = value;

		if (nvalue < 0.0)
			nvalue = 0.0;

		nvalue = 0.5 * pow (nvalue * 2.0 , (double) (1.0 + contrast));

		if (value > 0.5)
			value = 1.0 - nvalue;
		else
			value = nvalue;
	} else {
		if (value > 0.5)
			nvalue = 1.0 - value;
		else
			nvalue = value;
		
		if (nvalue < 0.0)
			nvalue = 0.0;
		
		power = (contrast == 1.0) ? 127 : 1.0 / (1.0 - contrast);
		nvalue = 0.5 * pow (2.0 * nvalue, power);
		
		if (value > 0.5)
			value = 1.0 - nvalue;
		else
			value = nvalue;
	}
	
	return (guchar) (value * 255);
}


/* Public functions.  */

int
f_pixbuf_get_image_size (GdkPixbuf *pixbuf)
{
	int width, height;

	width = gdk_pixbuf_get_width (pixbuf);
	height = gdk_pixbuf_get_height (pixbuf);

	return MAX (width, height);
}

int
f_pixbuf_get_scaled_width (GdkPixbuf *pixbuf,
			   int size)
{
	int orig_width, orig_height;

	orig_width = gdk_pixbuf_get_width (pixbuf);
	orig_height = gdk_pixbuf_get_height (pixbuf);

	if (orig_width > orig_height)
		return size;
	else
		return size * ((double) orig_width / orig_height);
}

int
f_pixbuf_get_scaled_height (GdkPixbuf *pixbuf,
			    int size)
{
	int orig_width, orig_height;

	orig_width = gdk_pixbuf_get_width (pixbuf);
	orig_height = gdk_pixbuf_get_height (pixbuf);

	if (orig_width > orig_height)
		return size * ((double) orig_height / orig_width);
	else
		return size;
}

/* Return a new GdkPixbuf enhancing/reducing brightness and contrast according
   to the specified values (from -1.0 to +1.0).  */
GdkPixbuf *
f_pixbuf_copy_apply_brightness_and_contrast (GdkPixbuf *src,
					     float brightness,
					     float contrast)
{
	GdkPixbuf *result_pixbuf;
	char *sp, *dp;
	int width, height;
	int line;
	int result_rowstride, src_rowstride;
	int bytes_per_pixel;

	g_return_val_if_fail ((brightness > -1.0 || F_DOUBLE_EQUAL (brightness, -1.0))
			       && (brightness < 1.0 || F_DOUBLE_EQUAL (brightness, 1.0)),
			      NULL);
	g_return_val_if_fail ((contrast > -1.0 || F_DOUBLE_EQUAL (contrast, -1.0))
			       && (contrast < 1.0 || F_DOUBLE_EQUAL (contrast, 1.0)),
			      NULL);

	if (F_DOUBLE_EQUAL (brightness, 0.0) && F_DOUBLE_EQUAL (contrast, 0.0))
		return gdk_pixbuf_copy (src);

	result_pixbuf = gdk_pixbuf_new (gdk_pixbuf_get_colorspace (src),
					gdk_pixbuf_get_has_alpha (src),
					gdk_pixbuf_get_bits_per_sample (src),
					gdk_pixbuf_get_width (src),
					gdk_pixbuf_get_height (src));

	width = gdk_pixbuf_get_width (result_pixbuf);
	height = gdk_pixbuf_get_height (result_pixbuf);

	result_rowstride = gdk_pixbuf_get_rowstride (result_pixbuf);
	src_rowstride = gdk_pixbuf_get_rowstride (src);

	bytes_per_pixel = gdk_pixbuf_get_has_alpha (result_pixbuf) ? 4 : 3;

	sp = gdk_pixbuf_get_pixels (src);
	dp = gdk_pixbuf_get_pixels (result_pixbuf);

	for (line = 0; line < height; line ++) {
		char *sq = sp;
		char *dq = dp;
		int i;

		for (i = 0; i < width; i ++) {
			dq[0] = apply_brightness_and_contrast (sq[0], brightness, contrast);
			dq[1] = apply_brightness_and_contrast (sq[1], brightness, contrast);
			dq[2] = apply_brightness_and_contrast (sq[2], brightness, contrast);

			dq += bytes_per_pixel;
			sq += bytes_per_pixel;
		}

		sp += src_rowstride;
		dp += result_rowstride;
	}

	return result_pixbuf;
}

cairo_surface_t *
f_pixbuf_to_cairo_surface (GdkPixbuf *pixbuf)
{
  gint width = gdk_pixbuf_get_width (pixbuf);
  gint height = gdk_pixbuf_get_height (pixbuf);
  guchar *gdk_pixels = gdk_pixbuf_get_pixels (pixbuf);
  int gdk_rowstride = gdk_pixbuf_get_rowstride (pixbuf);
  int n_channels = gdk_pixbuf_get_n_channels (pixbuf);
  guchar *cairo_pixels;
  cairo_format_t format;
  cairo_surface_t *surface;
  int j;

  if (n_channels == 3)
    format = CAIRO_FORMAT_RGB24;
  else
    format = CAIRO_FORMAT_ARGB32;

  surface = f_image_surface_create (format, width, height);
  cairo_pixels = (guchar *)f_image_surface_get_data (surface);

  for (j = height; j; j--)
    {
      guchar *p = gdk_pixels;
      guchar *q = cairo_pixels;

      if (n_channels == 3)
	{
	  guchar *end = p + 3 * width;
	  
	  while (p < end)
	    {
#if G_BYTE_ORDER == G_LITTLE_ENDIAN
	      q[0] = p[2];
	      q[1] = p[1];
	      q[2] = p[0];
#else	  
	      q[1] = p[0];
	      q[2] = p[1];
	      q[3] = p[2];
#endif
	      p += 3;
	      q += 4;
	    }
	}
      else
	{
	  guchar *end = p + 4 * width;
	  guint t1,t2,t3;
	    
#define MULT(d,c,a,t) G_STMT_START { t = c * a + 0x7f; d = ((t >> 8) + t) >> 8; } G_STMT_END

	  while (p < end)
	    {
#if G_BYTE_ORDER == G_LITTLE_ENDIAN
	      MULT(q[0], p[2], p[3], t1);
	      MULT(q[1], p[1], p[3], t2);
	      MULT(q[2], p[0], p[3], t3);
	      q[3] = p[3];
#else	  
	      q[0] = p[3];
	      MULT(q[1], p[0], p[3], t1);
	      MULT(q[2], p[1], p[3], t2);
	      MULT(q[3], p[2], p[3], t3);
#endif
	      
	      p += 4;
	      q += 4;
	    }
	  
#undef MULT
	}

      gdk_pixels += gdk_rowstride;
      cairo_pixels += 4 * width;
    }

  return surface;
}

GdkPixbuf *
f_pixbuf_from_cairo_surface (cairo_surface_t *source)
{
  gint width = cairo_image_surface_get_width (source);
  gint height = cairo_image_surface_get_height (source);
  GdkPixbuf *pixbuf = gdk_pixbuf_new (GDK_COLORSPACE_RGB,
				      TRUE,
				      8,
				      width,
				      height);

  guchar *gdk_pixels = gdk_pixbuf_get_pixels (pixbuf);
  int gdk_rowstride = gdk_pixbuf_get_rowstride (pixbuf);
  int n_channels = gdk_pixbuf_get_n_channels (pixbuf);
  cairo_format_t format;
  cairo_surface_t *surface;
  cairo_t *ctx;
  static const cairo_user_data_key_t key;
  int j;

  format = f_image_surface_get_format (source);
  surface = cairo_image_surface_create_for_data (gdk_pixels,
						 format,
						 width, height, gdk_rowstride);
  ctx = cairo_create (surface);
  cairo_set_source_surface (ctx, source, 0, 0);
  if (format == CAIRO_FORMAT_ARGB32)
	  cairo_mask_surface (ctx, source, 0, 0);
  else
	  cairo_paint (ctx);

  for (j = height; j; j--)
    {
      guchar *p = gdk_pixels;
      guchar *end = p + 4 * width;
      guchar tmp;

      while (p < end)
	{
	  tmp = p[0];
#if G_BYTE_ORDER == G_LITTLE_ENDIAN
	  p[0] = p[2];
	  p[2] = tmp;
#else	  
	  p[0] = p[1];
	  p[1] = p[2];
	  p[2] = p[3];
	  p[3] = tmp;
#endif
	  p += 4;
	}

      gdk_pixels += gdk_rowstride;
    }

  cairo_destroy (ctx);
  cairo_surface_destroy (surface);
  return pixbuf;
}


/**
 *  This alorithm is based on the redeye algorithm in flphoto 
 *  Copyright 2002-2003 by Michael Sweet
 *
 *  FIXME this is a very simplist algorithm, something more intelligent needs to be used.
 *
 *  Note: this is no longer used. A better implementation was written in C# in PixbufUtils.cs
 */

//void
//f_pixbuf_remove_redeye (GdkPixbuf *src)
//{
//	int width = gdk_pixbuf_get_width (src);
//	int height = gdk_pixbuf_get_height (src);
//	int i, j;
//
//	int r, g, b;
//	int channels = gdk_pixbuf_get_n_channels (src);
//
//	guchar *row = gdk_pixbuf_get_pixels (src);
//
//	for (i = 0; i < height; i++) {
//		guchar *col = row;
//
//		for (j = 0; j < width; j++) {
//			r = *col;
//			g = *(col + 1);
//			b = *(col + 2);
//			
//			if ((r > (3 * g / 2) && r > (3 * b / 2)) || (g > r && b > r)) {
//				memset(col, (r * 31 + g * 61 + b * 8) / 100, 3);
//			}
//			
//			col += channels;
//		}
//		row += gdk_pixbuf_get_rowstride (src);
//	}
//}

gboolean
f_pixbuf_save_jpeg_atomic  (GdkPixbuf   *pixbuf,
			    const char  *file_name,
			    int          quality,
			    GError     **error)
{
	char *tmp_file_name = g_strconcat (file_name, ".tmp", NULL);
	char *quality_string = g_strdup_printf ("%d", quality);
	gboolean success;

	if (! gdk_pixbuf_save (pixbuf, tmp_file_name, "jpeg", error,
			       "quality", quality_string, NULL)) {
		success = FALSE;
		goto end;
	}

	if (rename (tmp_file_name, file_name) != 0) {
		char *error_message = g_strdup_printf ("Atomic rename failed: %s", g_strerror (errno));
		g_set_error (error, GDK_PIXBUF_ERROR, GDK_PIXBUF_ERROR_FAILED, error_message);
		g_free (error_message);

		success = FALSE;
		goto end;
	}

	success = TRUE;

 end:
	g_free (quality_string);
	g_free (tmp_file_name);
	return TRUE;
}

static void 
rotate_line (guchar *sbuf, guchar *dstart, int length, int stride, int channels, gboolean mirror)
{
	guchar *dbuf = dstart;
	int doffset = stride - channels;
	int soffset = 0;

	if (mirror) {
		sbuf += (length - 1) * channels;
		doffset = stride - channels;
		soffset = - (2 * channels);
	}   

	if (channels == 3)
		while (length--) {
			*(dbuf++) = *(sbuf++);
			*(dbuf++) = *(sbuf++);
			*(dbuf++) = *(sbuf++);
			dbuf += doffset;
			sbuf += soffset;
		}
	else
		while (length--) {
			*(dbuf++) = *(sbuf++);
			*(dbuf++) = *(sbuf++);
			*(dbuf++) = *(sbuf++);
			*(dbuf++) = *(sbuf++);
			dbuf += doffset;
			sbuf += soffset;
		}
}

static void
copy_line (guchar *sbuf, guchar *dbuf, int length, int channels, gboolean mirror)
{
	if (!mirror) {
		memcpy (dbuf, sbuf, length * channels);
	} else {
		dbuf += (length - 1) * channels;
		if (channels == 3)
			while (length --) {
				*(dbuf++) = *(sbuf++);
				*(dbuf++) = *(sbuf++);
				*(dbuf++) = *(sbuf++);	
				dbuf -= 6;
			}
		else 
			while (length --) {
				*(dbuf++) = *(sbuf++);
				*(dbuf++) = *(sbuf++);
				*(dbuf++) = *(sbuf++);	
				*(dbuf++) = *(sbuf++);	
				dbuf -= 8;
			}				
	}	
}

void
f_pixbuf_copy_with_orientation (GdkPixbuf *src, GdkPixbuf *dest, int orientation)
{
	gboolean rotate = FALSE;
	gboolean flip = FALSE;
	gboolean mirror = FALSE;
	
	int sw = gdk_pixbuf_get_width (src);
	int sh = gdk_pixbuf_get_height (src);
	int dw = gdk_pixbuf_get_width (dest);
	int dh = gdk_pixbuf_get_height (dest);
	

	int channels = gdk_pixbuf_get_n_channels (src);
	
	int dstride = gdk_pixbuf_get_rowstride (dest);
	int sstride = gdk_pixbuf_get_rowstride (src);
	
	guchar *sp = gdk_pixbuf_get_pixels (src);
	guchar *dp = gdk_pixbuf_get_pixels (dest);		
	int offset = sstride;

	if (channels != gdk_pixbuf_get_n_channels (dest)) {
		g_warning ("source and dest channels do no match");
		return;
	}
	
	switch (orientation) {
	case 1: // TopLeft
		break;
	case 2: // TopRight
		flip = TRUE;
		break;
	case 3: // BottomRight
		mirror = TRUE;
		flip = TRUE;
		break;
	case 4: // BottomLeft
		mirror = TRUE;
		break;
	case 5: // LeftTop
		rotate = TRUE;
		break;
	case 6: // RightTop
		mirror = TRUE;
		rotate = TRUE;
		break;
	case 7: // RightBottom
		flip = TRUE;
		mirror = TRUE;
		rotate = TRUE;
		break;
	case 8: // LeftBottom
		flip = TRUE;
		rotate = TRUE;
		break;
	}

	if (rotate && (dh != sw || dw != sh)) {
		g_warning ("source and destination sizes do not match orientation");
		return;
	}
	
	//g_warning ("rotate = %d, flip = %d, mirror = %d", rotate, flip, mirror);
	if (mirror) {
		offset = -sstride;
		sp = sp + (sh - 1) * sstride;
	}
	
	while (sh --) {
		if (rotate) {
			rotate_line (sp, dp, sw, dstride, channels, flip);
			dp += channels;
		} else {
				
			copy_line (sp, dp, sw, channels, flip);
			dp += dstride;
		}
		sp += offset;
	}
}
