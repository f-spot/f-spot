/* -*- Mode: C; indent-tabs-mode: t; c-basic-offset: 8; tab-width: 8 -*- */
/* f-image-view.h
 *
 * Copyright (C) 2003  Ettore Perazzoli
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
 * Author: Ettore Perazzoli <ettore@ximian.com>
 */

#ifndef _F_IMAGE_VIEW_H_
#define _F_IMAGE_VIEW_H_

#include "libeog/image-view.h"

#define F_TYPE_IMAGE_VIEW			(f_image_view_get_type ())
#define F_IMAGE_VIEW(obj)			(G_TYPE_CHECK_INSTANCE_CAST ((obj), F_TYPE_IMAGE_VIEW, FImageView))
#define F_IMAGE_VIEW_CLASS(klass)		(G_TYPE_CHECK_CLASS_CAST ((klass), F_TYPE_IMAGE_VIEW, FImageViewClass))
#define F_IS_IMAGE_VIEW(obj)			(G_TYPE_CHECK_INSTANCE_TYPE ((obj), F_TYPE_IMAGE_VIEW))
#define F_IS_IMAGE_VIEW_CLASS(klass)		(G_TYPE_CHECK_CLASS_TYPE ((obj), F_TYPE_IMAGE_VIEW))


enum _FImageViewPointerMode {
	F_IMAGE_VIEW_POINTER_MODE_NONE,
	F_IMAGE_VIEW_POINTER_MODE_SELECT,
	F_IMAGE_VIEW_POINTER_MODE_SCROLL
};
typedef enum _FImageViewPointerMode FImageViewPointerMode;


typedef struct _FImageView        FImageView;
typedef struct _FImageViewPrivate FImageViewPrivate;
typedef struct _FImageViewClass   FImageViewClass;

struct _FImageView {
	ImageView parent;

	FImageViewPrivate *priv;
};

struct _FImageViewClass {
	ImageViewClass parent_class;

	void (* selection_changed) (FImageView *image_view);
};


GType  f_image_view_get_type  (void);

GtkWidget *f_image_view_new  (void);

void                   f_image_view_set_pointer_mode  (FImageView            *image_view,
						       FImageViewPointerMode  mode);
FImageViewPointerMode  f_image_view_get_pointer_mode  (FImageView            *image_view);

void     f_image_view_set_selection_xy_ratio  (FImageView *image_view,
					       gdouble     selection_xy_ratio);
gdouble  f_image_view_get_selection_xy_ratio  (FImageView *image_view);

gboolean  f_image_view_get_selection  (FImageView *image_view,
				       int        *x_return,
				       int        *y_return,
				       int        *width_return,
				       int        *height_return);

void  f_image_view_unset_selection  (FImageView *image_view);

#endif /* _F_IMAGE_VIEW_H_ */
