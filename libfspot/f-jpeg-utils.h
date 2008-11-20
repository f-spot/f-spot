/* -*- Mode: C; indent-tabs-mode: t; c-basic-offset: 8; tab-width: 8 -*-
 *
 * f-jpeg-utils.c: Utility functions for JPEG files.
 * 
 * Copyright (C) 2001 Red Hat Inc.
 * Copyright (C) 2001 The Free Software Foundation, Inc.
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
 * Authors: Alexander Larsson <alexl@redhat.com>
 *          Ettore Perazzoli <ettore@perazzoli.org>
 *          Paolo Bacchilega <paolo.bacch@tin.it>
 */

#ifndef F_JPEG_UTILS_H
#define F_JPEG_UTILS_H

#include <gdk-pixbuf/gdk-pixbuf.h>

//GdkPixbuf *f_load_scaled_jpeg  (const char *path,
//				int         target_width,
//				int         target_heigh,
//				int        *original_width_return,
//				int        *original_height_return);
//
void  f_get_jpeg_size  (const char *path,
			int        *width_return,
			int        *height_return);


enum _FJpegTransform {
	F_JPEG_TRANSFORM_ROTATE_90,
	F_JPEG_TRANSFORM_ROTATE_180,
	F_JPEG_TRANSFORM_ROTATE_270,
	F_JPEG_TRANSFORM_FLIP_H,
	F_JPEG_TRANSFORM_FLIP_V
};
typedef enum _FJpegTransform FJpegTransform;

gboolean  f_transform_jpeg  (const char     *source_path,
			     const char     *destination_path,
			     FJpegTransform  transform,
			     char **error_message_return);

#endif /* F_JPEG_UTILS_H */
