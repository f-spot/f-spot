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


/* Returns a copy of pixbuf src rotated 90 degrees clockwise or 90
   counterclockwise.  */
GdkPixbuf *
f_pixbuf_copy_rotate_90 (GdkPixbuf *src, 
			 gboolean counter_clockwise)
{
	GdkPixbuf *dest;
	int        has_alpha;
	int        sw, sh, srs;
	int        dw, dh, drs;
	guchar    *s_pix;
        guchar    *d_pix;
	guchar    *sp;
        guchar    *dp;
	int        i, j;
	int        a;

	if (!src) return NULL;

	sw = gdk_pixbuf_get_width (src);
	sh = gdk_pixbuf_get_height (src);
	has_alpha = gdk_pixbuf_get_has_alpha (src);
	srs = gdk_pixbuf_get_rowstride (src);
	s_pix = gdk_pixbuf_get_pixels (src);

	dw = sh;
	dh = sw;
	dest = gdk_pixbuf_new (GDK_COLORSPACE_RGB, has_alpha, 8, dw, dh);
	drs = gdk_pixbuf_get_rowstride (dest);
	d_pix = gdk_pixbuf_get_pixels (dest);

	a = (has_alpha ? 4 : 3);

	for (i = 0; i < sh; i++) {
		sp = s_pix + (i * srs);
		for (j = 0; j < sw; j++) {
			if (counter_clockwise)
				dp = d_pix + ((dh - j - 1) * drs) + (i * a);
			else
				dp = d_pix + (j * drs) + ((dw - i - 1) * a);

			*(dp++) = *(sp++);	/* r */
			*(dp++) = *(sp++);	/* g */
			*(dp++) = *(sp++);	/* b */
			if (has_alpha) *(dp) = *(sp++);	/* a */
		}
	}

	return dest;
}

/* Returns a copy of pixbuf mirrored and or flipped.  TO do a 180 degree
   rotations set both mirror and flipped TRUE if mirror and flip are FALSE,
   result is a simple copy.  */
GdkPixbuf *
f_pixbuf_copy_mirror (GdkPixbuf *src, 
		      gboolean mirror, 
		      gboolean flip)
{
	GdkPixbuf *dest;
	int        has_alpha;
	int        w, h, srs;
	int        drs;
	guchar    *s_pix;
        guchar    *d_pix;
	guchar    *sp;
        guchar    *dp;
	int        i, j;
	int        a;

	if (!src) return NULL;

	w = gdk_pixbuf_get_width (src);
	h = gdk_pixbuf_get_height (src);
	has_alpha = gdk_pixbuf_get_has_alpha (src);
	srs = gdk_pixbuf_get_rowstride (src);
	s_pix = gdk_pixbuf_get_pixels (src);

	dest = gdk_pixbuf_new (GDK_COLORSPACE_RGB, has_alpha, 8, w, h);
	drs = gdk_pixbuf_get_rowstride (dest);
	d_pix = gdk_pixbuf_get_pixels (dest);

	a = has_alpha ? 4 : 3;

	for (i = 0; i < h; i++)	{
		sp = s_pix + (i * srs);
		if (flip)
			dp = d_pix + ((h - i - 1) * drs);
		else
			dp = d_pix + (i * drs);

		if (mirror) {
			dp += (w - 1) * a;
			for (j = 0; j < w; j++) {
				*(dp++) = *(sp++);	/* r */
				*(dp++) = *(sp++);	/* g */
				*(dp++) = *(sp++);	/* b */
				if (has_alpha) *(dp) = *(sp++);	/* a */
				dp -= (a + 3);
			}
		} else {
			for (j = 0; j < w; j++) {
				*(dp++) = *(sp++);	/* r */
				*(dp++) = *(sp++);	/* g */
				*(dp++) = *(sp++);	/* b */
				if (has_alpha) *(dp++) = *(sp++);	/* a */
			}
		}
	}
	
	return dest;
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
