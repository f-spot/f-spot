/* -*- Mode: C; indent-tabs-mode: t; c-basic-offset: 8; tab-width: 8 -*- */
/* f-image-view.c
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

#include <config.h>

#include "f-image-view.h"

#include "f-marshal.h"
#include "f-utils.h"
#include "cairo.h"
#include "libeog/cursors.h"


#define PARENT_TYPE image_view_get_type ()
static ImageViewClass *parent_class = NULL;


/* Thickness of the rectangle drawn to show the current selection.  */
#define SELECTION_LINE_WIDTH  1

/* Minimum number of pixels to move the pointer on the screen before the
   is started.  */
#define SELECTION_THRESHOLD  3

/* Maximum distance in pixel required between the selection and the mouse for a
   drag operation to modify the current selection.  */
#define SELECTION_EDGE_DISTANCE 8


enum {
	SELECTION_CHANGED,
	NUM_SIGNALS
};
static unsigned int signals[NUM_SIGNALS] = { 0 };


enum _Mode {
	MODE_IDLE,
	MODE_DRAG_X1,
	MODE_DRAG_X2,
	MODE_DRAG_Y1,
	MODE_DRAG_Y2,
	MODE_DRAG_X1Y1,
	MODE_DRAG_X1Y2,
	MODE_DRAG_X2Y2,
	MODE_DRAG_X2Y1,
	MODE_MOVE
};
typedef enum _Mode Mode;

struct _Selection {
	int x1, y1;
	int x2, y2;
};
typedef struct _Selection Selection;

struct _FImageViewPrivate {
	Mode mode;

	GdkGC *selection_gc;

	FImageViewPointerMode pointer_mode;
	gdouble selection_xy_ratio;

	F_BOOLEAN_MEMBER (selection_active);
	F_BOOLEAN_MEMBER (is_new_selection);

	int button_press_x, button_press_y;
	int drag_x_offset, drag_y_offset;

	/* The current selection.  */
	Selection selection;

	/* The selection as it was before a drag operation was started (used
	   when in MODE_MOVE).  */
	Selection initial_selection;
};


/* Utility functions.  */
void
f_image_view_window_coords_to_image (FImageView *image_view,
				     int window_x, int window_y,
				     int *image_x_return, int *image_y_return)
{
	GdkPixbuf *pixbuf;
	int x_offset, y_offset;
	int scaled_width, scaled_height;

	pixbuf = image_view_get_pixbuf (IMAGE_VIEW (image_view));

	image_view_get_offsets_and_size (IMAGE_VIEW (image_view),
					 &x_offset, &y_offset,
					 &scaled_width, &scaled_height);

	window_x = CLAMP (window_x, x_offset, x_offset + scaled_width - 1);
	window_y = CLAMP (window_y, y_offset, y_offset + scaled_height - 1);

	if (image_x_return != NULL) {
		*image_x_return = floor ((window_x - x_offset)
					 * (double) (gdk_pixbuf_get_width (pixbuf) - 1)
					 / (double) (scaled_width - 1) + .5);
	}

	if (image_y_return != NULL) {
		*image_y_return = floor ((window_y - y_offset)
					 * (double) (gdk_pixbuf_get_height (pixbuf) - 1)
					 / (double) (scaled_height - 1) + .5);
	}

	g_object_unref (pixbuf);
}

static void
image_coords_to_window (FImageView *image_view,
			int image_x, int image_y,
			int *window_x_return, int *window_y_return)
{
	GtkAllocation *allocation;
	GdkPixbuf *pixbuf;
	int x_offset, y_offset;
	int scaled_width, scaled_height;

	pixbuf = image_view_get_pixbuf (IMAGE_VIEW (image_view));
	allocation = & GTK_WIDGET (image_view)->allocation;

	image_view_get_offsets_and_size (IMAGE_VIEW (image_view),
					 &x_offset, &y_offset,
					 &scaled_width, &scaled_height);

	if (window_x_return != NULL) {
		*window_x_return = floor (image_x
					  * (double) (scaled_width - 1)
					  / (gdk_pixbuf_get_width (pixbuf) - 1) + .5);
		*window_x_return += x_offset;
	}
	if (window_y_return != NULL) {
		*window_y_return = floor (image_y
					  * (double) (scaled_height - 1)
					  / (gdk_pixbuf_get_height (pixbuf) - 1) + .5);
		*window_y_return += y_offset;
	}

	g_object_unref (pixbuf);
}

static void 
draw_union (GtkWidget *widget, GdkRectangle *previous, GdkRectangle *current)
{
	gdk_rectangle_union (current, previous, current);
	gtk_widget_queue_draw_area (widget, current->x, current->y, current->width, current->height);
	//gtk_widget_queue_draw (widget);
}

static GdkCursor *
get_cursor_for_mode (FImageView *image_view, Mode mode)
{
	if (mode != MODE_IDLE && image_view->priv->is_new_selection)
		return gdk_cursor_new_for_display (gdk_display_get_default (), GDK_TOP_LEFT_ARROW);

	switch (mode) {
	case MODE_IDLE:
		return NULL;

	case MODE_DRAG_X1:
	case MODE_DRAG_X2:
		return gdk_cursor_new_for_display (gdk_display_get_default (), GDK_SB_H_DOUBLE_ARROW);
		
	case MODE_DRAG_Y1:
	case MODE_DRAG_Y2:
		return gdk_cursor_new_for_display (gdk_display_get_default (), GDK_SB_V_DOUBLE_ARROW);

	case MODE_DRAG_X1Y1:
		return gdk_cursor_new_for_display (gdk_display_get_default (), GDK_TOP_LEFT_CORNER);

	case MODE_DRAG_X1Y2:
		return gdk_cursor_new_for_display (gdk_display_get_default (), GDK_BOTTOM_LEFT_CORNER);

	case MODE_DRAG_X2Y2:
		return gdk_cursor_new_for_display (gdk_display_get_default (), GDK_BOTTOM_RIGHT_CORNER);

	case MODE_DRAG_X2Y1:
		return gdk_cursor_new_for_display (gdk_display_get_default (), GDK_TOP_RIGHT_CORNER);

	case MODE_MOVE:
		return cursor_get (GTK_WIDGET (image_view), CURSOR_HAND_OPEN);

	default:
		g_assert_not_reached ();
	}

	return NULL;
}

static void
set_cursor (FImageView *image_view)
{
	GdkCursor *cursor = NULL;

	if (!GDK_IS_WINDOW(GTK_WIDGET (image_view)->window)) 
		return;
	
	if (image_view->priv->mode != MODE_IDLE) {
		cursor = get_cursor_for_mode (image_view, image_view->priv->mode);
	} else {
		switch (image_view->priv->pointer_mode) {
		case F_IMAGE_VIEW_POINTER_MODE_NONE:
		case F_IMAGE_VIEW_POINTER_MODE_SELECT:
			cursor = gdk_cursor_new_for_display (gdk_display_get_default (), GDK_TOP_LEFT_ARROW);
			break;

		case F_IMAGE_VIEW_POINTER_MODE_SCROLL:
			cursor = cursor_get (GTK_WIDGET (image_view), CURSOR_HAND_OPEN);
			break;

		default:
			g_assert_not_reached ();
		}
	}

	gdk_window_set_cursor (GTK_WIDGET (image_view)->window, cursor);
	gdk_cursor_unref (cursor);
}




/* Utilty functions for selection handling.  */

/* This uses GDK_INVERT so if called a second time without the selection
   changing it will remove the selection rectangle from the window.  */
static void
draw_selection (FImageView *image_view,
		GdkRectangle *area)
{
	FImageViewPrivate *priv = image_view->priv;
	int x1, y1, x2, y2;
	GdkRectangle zone;

	if (! priv->selection_active
	    || (area != NULL && (area->width == 0 || area->height == 0)))
		return;

	image_coords_to_window (image_view, priv->selection.x1, priv->selection.y1, &x1, &y1);
	image_coords_to_window (image_view, priv->selection.x2, priv->selection.y2, &x2, &y2);

	zone.x = MIN (x1, x2);
	zone.y = MIN (y1, y2);

	zone.width = ABS (x1 - x2);
	zone.height = ABS (y1 - y2);

	gtk_widget_queue_draw_area (GTK_WIDGET (image_view), zone.x, zone.y, zone.width, zone.height);
}

static GdkRectangle
get_selection_box (FImageView *image_view)
{
	FImageViewPrivate *priv = image_view->priv;
	int x1, y1, x2, y2;
	GdkRectangle zone;
	GdkPixbuf *pixbuf = image_view_get_pixbuf (IMAGE_VIEW (image_view));

	if (! priv->selection_active) {
		//zone = (GdkRectangle) GTK_WIDGET (image_view)->allocation;
		image_coords_to_window (image_view, 0, 0, &x1, &y1);
		if (pixbuf != NULL)
			image_coords_to_window (image_view, gdk_pixbuf_get_width (pixbuf), gdk_pixbuf_get_height (pixbuf), &x2, &y2);
		else 
			image_coords_to_window (image_view, 0, 0, &x2, &y2);
	} else {
		image_coords_to_window (image_view, priv->selection.x1, priv->selection.y1, &x1, &y1);
		image_coords_to_window (image_view, priv->selection.x2, priv->selection.y2, &x2, &y2);
	}

	zone.x = MIN (x1, x2);
	zone.y = MIN (y1, y2);

	zone.width = ABS (x1 - x2);
	zone.height = ABS (y1 - y2);

	return zone;
}

static gboolean
adjust_height_for_constraints (FImageView *image_view)
{
	FImageViewPrivate *priv = image_view->priv;
	int offset = floor ((ABS (priv->selection.x1 - priv->selection.x2) / priv->selection_xy_ratio) + .5);
	int *py1, *py2;

	switch (priv->mode) {
	case MODE_DRAG_Y1:
	case MODE_DRAG_X1Y1:
	case MODE_DRAG_X2Y1:
		py1 = &priv->selection.y2;
		py2 = &priv->selection.y1;
		break;
	default:
		py1 = &priv->selection.y1;
		py2 = &priv->selection.y2;
		break;
	}

	if (*py2 < *py1) {
		if (*py1 - offset < 0) {
			*py2 = 0;
			return FALSE;
		}

		*py2 = *py1 - offset;
	} else {
		GdkPixbuf *pixbuf = image_view_get_pixbuf (IMAGE_VIEW (image_view));

		if (pixbuf == NULL)
			return FALSE;

		if (*py1 + offset >= gdk_pixbuf_get_height (pixbuf)) {
			*py2 = gdk_pixbuf_get_height (pixbuf) - 1;
			g_object_unref (pixbuf);
			return FALSE;
		}

		*py2 = *py1 + offset;

		g_object_unref (pixbuf);
	}

	return TRUE;
}

static gboolean
adjust_width_for_constraints (FImageView *image_view)
{
	FImageViewPrivate *priv = image_view->priv;
	int offset = floor ((ABS (priv->selection.y1 - priv->selection.y2) * priv->selection_xy_ratio) + .5);
	int *px1, *px2;

	switch (priv->mode) {
	case MODE_DRAG_X1:
	case MODE_DRAG_X1Y1:
	case MODE_DRAG_X1Y2:
		px1 = &priv->selection.x2;
		px2 = &priv->selection.x1;
		break;
	default:
		px1 = &priv->selection.x1;
		px2 = &priv->selection.x2;
		break;
	}

	if (*px2 < *px1) {
		if (*px1 - offset < 0) {
			*px2 = 0;
			return FALSE;
		}

		*px2 = *px1 - offset;
	} else {
		GdkPixbuf *pixbuf = image_view_get_pixbuf (IMAGE_VIEW (image_view));

		if (pixbuf == NULL)
			return FALSE;

		if (*px1 + offset >= gdk_pixbuf_get_width (pixbuf)) {
			*px2 = gdk_pixbuf_get_width (pixbuf) - 1;
			g_object_unref (pixbuf);
			return FALSE;
		}

		*px2 = *px1 + offset;

		g_object_unref (pixbuf);
	}

	return TRUE;
}

static void
constrain_selection (FImageView *image_view)
{
	FImageViewPrivate *priv = image_view->priv;
	double ratio;

	if (F_DOUBLE_EQUAL (priv->selection_xy_ratio, 0.0))
		return;
	if ((ABS (priv->selection.x2 - priv->selection.x1) > ABS (priv->selection.y2 - priv->selection.y1) && priv->selection_xy_ratio < 1) ||
		(ABS (priv->selection.x2 - priv->selection.x1) < ABS (priv->selection.y2 - priv->selection.y1) && priv->selection_xy_ratio > 1)) 
		priv->selection_xy_ratio = (double)1.0 / priv->selection_xy_ratio;

	switch (priv->mode) {
	case MODE_DRAG_X1:
	case MODE_DRAG_X2:
		if (! adjust_height_for_constraints (image_view))
			adjust_width_for_constraints (image_view);
		break;

	case MODE_DRAG_Y1:
	case MODE_DRAG_Y2:
		if (! adjust_width_for_constraints (image_view))
			adjust_height_for_constraints (image_view);
		break;

	default:
		ratio = (double) ABS (priv->selection.x2 - priv->selection.x1) / ABS (priv->selection.y2 - priv->selection.y1);
		if (ratio > priv->selection_xy_ratio) {
			if (! adjust_height_for_constraints (image_view))
				adjust_width_for_constraints (image_view);
		} else {
			if (! adjust_width_for_constraints (image_view))
				adjust_height_for_constraints (image_view);
		}
	}
}

static gboolean
check_corner_for_drag (int x, int y,
		       int mouse_x, int mouse_y,
		       int *x_offset_return, int *y_offset_return)
{
	if (ABS (mouse_x - x) > SELECTION_EDGE_DISTANCE || ABS (mouse_y - y) > SELECTION_EDGE_DISTANCE)
		return FALSE;

	if (x_offset_return != NULL)
		*x_offset_return = mouse_x - x;
	if (y_offset_return != NULL)
		*y_offset_return = mouse_y - y;

	return TRUE;
}

static gboolean
check_side_for_drag (int coord,
		     int mouse_coord,
		     int *offset_return)
{
	if (ABS (mouse_coord - coord) > SELECTION_EDGE_DISTANCE)
		return FALSE;

	if (offset_return != NULL)
		*offset_return = mouse_coord - coord;

	return TRUE;
}

static Mode
get_drag_mode_for_mouse_position (FImageView *view,
				  int mouse_x, int mouse_y,
				  int *drag_x_offset_return, int *drag_y_offset_return)
{
	FImageViewPrivate *priv = view->priv;
	int x1, y1, x2, y2, swap;

	if (! priv->selection_active)
		return MODE_IDLE;

	image_coords_to_window (view, priv->selection.x1, priv->selection.y1, &x1, &y1);
	image_coords_to_window (view, priv->selection.x2, priv->selection.y2, &x2, &y2);

	//Swap coords so x1<x2 and y1<y2
	if (priv->selection.x1 > priv->selection.x2) {
		swap = priv->selection.x1 ;
		priv->selection.x1 = priv->selection.x2 ;
		priv->selection.x2 = swap ;
	}
	if (priv->selection.y1 > priv->selection.y2) {
		swap = priv->selection.y1 ;
		priv->selection.y1 = priv->selection.y2 ;
		priv->selection.y2 = swap ;
	}

	if (check_corner_for_drag (x1, y1, mouse_x, mouse_y,
				   drag_x_offset_return, drag_y_offset_return))
		return MODE_DRAG_X1Y1;

	if (check_corner_for_drag (x1, y2, mouse_x, mouse_y,
				   drag_x_offset_return, drag_y_offset_return))
		return MODE_DRAG_X1Y2;

	if (check_corner_for_drag (x2, y2, mouse_x, mouse_y,
				   drag_x_offset_return, drag_y_offset_return))
		return MODE_DRAG_X2Y2;

	if (check_corner_for_drag (x2, y1, mouse_x, mouse_y,
				   drag_x_offset_return, drag_y_offset_return))
		return MODE_DRAG_X2Y1;

	if (drag_x_offset_return != NULL)
		*drag_x_offset_return = 0;
	if (drag_y_offset_return != NULL)
		*drag_y_offset_return = 0;


	if (mouse_x >= MIN (x1, x2) - SELECTION_EDGE_DISTANCE && mouse_x <= MAX (x1, x2) + SELECTION_EDGE_DISTANCE) {
		if (check_side_for_drag (y1, mouse_y, drag_y_offset_return))
			return MODE_DRAG_Y1;

		if (check_side_for_drag (y2, mouse_y, drag_y_offset_return))
			return MODE_DRAG_Y2;
	}

	if (mouse_y >= MIN (y1, y2) - SELECTION_EDGE_DISTANCE && mouse_y <= MAX (y1, y2) + SELECTION_EDGE_DISTANCE) {
		if (check_side_for_drag (x1, mouse_x, drag_x_offset_return))
			return MODE_DRAG_X1;

		if (check_side_for_drag (x2, mouse_x, drag_x_offset_return))
			return MODE_DRAG_X2;
	}

	if (mouse_x >= MIN (x1, x2) && mouse_x <= MAX (x1, x2)
	    && mouse_y >= MIN (y1, y2) && mouse_y <= MAX (y1, y2))
		return MODE_MOVE;

	return MODE_IDLE;
}

static void
emit_selection_changed (FImageView *image_view)
{
	g_signal_emit (image_view, signals[SELECTION_CHANGED], 0);
}


/* ImageView methods.  */

static void
impl_paint_extra (ImageView *iv,
		  GdkRectangle *area)
{
#if FALSE
	FImageView *image_view = (FImageView *) iv;
	FImageViewPrivate *priv = image_view->priv;
	int x1, y1, x2, y2;
	cairo_t *ctx;
	GdkRegion *selection;
	GdkRegion *other;
	GdkRectangle rect;

	if (! priv->selection_active)
		return; 

	image_coords_to_window (image_view, priv->selection.x1, priv->selection.y1, &x1, &y1);
	image_coords_to_window (image_view, priv->selection.x2, priv->selection.y2, &x2, &y2);

	rect.x = MIN (x1, x2);
	rect.y = MIN (y1, y2);

	rect.width = ABS (x1 - x2);
	rect.height = ABS (y1 - y2);
	
	other = gdk_region_new ();
	gdk_region_union_with_rect (other, area);
	selection = gdk_region_new ();
	gdk_region_union_with_rect (selection, &rect);
	gdk_region_subtract (other, selection);
	gdk_region_destroy (selection);

	ctx = gdk_cairo_create (GTK_WIDGET (image_view)->window);
	cairo_set_source_rgba (ctx, .5, .5, .5, .7);
	gdk_cairo_region (ctx, other);
	cairo_fill (ctx);
	cairo_destroy (ctx);
	gdk_region_destroy (other);
#endif
}


static gboolean
impl_expose_event (GtkWidget *widget, GdkEventExpose *event)
{
	FImageView *image_view = F_IMAGE_VIEW (widget);
	FImageViewPrivate *priv = image_view->priv;
	int x1, y1, x2, y2;
	cairo_t *ctx;
	GdkRegion *selection;
	GdkRectangle rect;

	(* GTK_WIDGET_CLASS (parent_class)->expose_event) (widget, event);

	if (! priv->selection_active)
		return FALSE; 

	image_coords_to_window (image_view, priv->selection.x1, priv->selection.y1, &x1, &y1);
	image_coords_to_window (image_view, priv->selection.x2, priv->selection.y2, &x2, &y2);

	rect.x = MIN (x1, x2);
	rect.y = MIN (y1, y2);

	rect.width = ABS (x1 - x2);
	rect.height = ABS (y1 - y2);
	
	selection = gdk_region_new ();
	gdk_region_union_with_rect (selection, &rect);
	gdk_region_subtract (event->region, selection);
	gdk_region_destroy (selection);

	ctx = gdk_cairo_create (GTK_WIDGET (widget)->window);
	cairo_set_source_rgba (ctx, .5, .5, .5, .7);
	gdk_cairo_region (ctx, event->region);
	cairo_fill (ctx);
	cairo_destroy (ctx);
	return TRUE;
}

/* GtkWidget methods.  */

static void
impl_realize (GtkWidget *widget)
{
	FImageViewPrivate *priv = F_IMAGE_VIEW (widget)->priv;

	(* GTK_WIDGET_CLASS (parent_class)->realize) (widget);

	set_cursor (F_IMAGE_VIEW (widget));

	g_assert (priv->selection_gc == NULL);

	priv->selection_gc = gdk_gc_new (widget->window);
	gdk_gc_copy (priv->selection_gc, widget->style->fg_gc[GTK_STATE_NORMAL]);
	gdk_gc_set_function (priv->selection_gc, GDK_INVERT);
	gdk_gc_set_line_attributes (priv->selection_gc, SELECTION_LINE_WIDTH,
				    GDK_LINE_SOLID, GDK_CAP_NOT_LAST, GDK_JOIN_MITER);
}

static void
impl_unrealize (GtkWidget *widget)
{
	(* GTK_WIDGET_CLASS (parent_class)->unrealize) (widget);

	F_UNREF (F_IMAGE_VIEW (widget)->priv->selection_gc);
}

static gboolean
impl_button_press_event (GtkWidget *widget,
			 GdkEventButton *button_event)
{
	FImageView *image_view = F_IMAGE_VIEW (widget);
	FImageViewPrivate *priv = image_view->priv;
	Mode mode;

	if (priv->pointer_mode == F_IMAGE_VIEW_POINTER_MODE_SCROLL)
		return (* GTK_WIDGET_CLASS (parent_class)->button_press_event) (widget, button_event);
	else if (priv->pointer_mode == F_IMAGE_VIEW_POINTER_MODE_NONE)
		return FALSE;

	if (button_event->type == GDK_2BUTTON_PRESS && button_event->button == 1) {
		priv->is_new_selection = FALSE;
		priv->mode = MODE_IDLE;
		return FALSE;
	}

	if (button_event->type != GDK_BUTTON_PRESS || button_event->button != 1 || priv->mode != MODE_IDLE)
		return FALSE;

	if (! GTK_WIDGET_HAS_FOCUS (widget))
		gtk_widget_grab_focus (widget);

	mode = get_drag_mode_for_mouse_position (image_view, button_event->x, button_event->y,
						 & priv->drag_x_offset, & priv->drag_y_offset);
	if (mode == MODE_IDLE) {
		priv->mode = MODE_DRAG_X2Y2;
		priv->is_new_selection = TRUE;
	} else {
		priv->mode = mode;
		priv->is_new_selection = FALSE;
	}

	priv->initial_selection = priv->selection;

	priv->button_press_x = button_event->x;
	priv->button_press_y = button_event->y;

	if (priv->is_new_selection) {
		/* Erase existing selection rectangle.  */
		gtk_widget_queue_draw (widget);

		f_image_view_window_coords_to_image (image_view,
						     button_event->x, button_event->y,
						     &priv->selection.x1, &priv->selection.y1);
		priv->selection_active = FALSE;
		priv->selection.x2 = priv->selection.x1;
		priv->selection.y2 = priv->selection.y1;

		emit_selection_changed (F_IMAGE_VIEW (image_view));
	}

	set_cursor (image_view);

	return TRUE;
}

static gboolean
impl_motion_notify_event (GtkWidget *widget,
			  GdkEventMotion *motion_event)
{
	FImageView *image_view = F_IMAGE_VIEW (widget);
	FImageViewPrivate *priv = image_view->priv;
	GdkModifierType mods;
	int x, y;
	int image_x, image_y;
	GdkRectangle previous;
	GdkRectangle current;
	
	if (priv->pointer_mode == F_IMAGE_VIEW_POINTER_MODE_SCROLL)
		return (* GTK_WIDGET_CLASS (parent_class)->motion_notify_event) (widget, motion_event);

	if (motion_event->is_hint)
		gdk_window_get_pointer (widget->window, &x, &y, &mods);
	else {
		x = motion_event->x;
		y = motion_event->y;
	}

	if (priv->mode == MODE_IDLE) {
		Mode drag_mode = get_drag_mode_for_mouse_position (image_view, x, y, NULL, NULL);
		GdkCursor *cursor;

		cursor = get_cursor_for_mode (image_view, drag_mode);

		if (cursor != NULL) {
			gdk_window_set_cursor (widget->window, cursor);
			gdk_cursor_unref (cursor);
		} else {
			set_cursor (image_view);
		}

		return TRUE;
	}

	previous = get_selection_box (image_view);
	if (! priv->selection_active) {
		/* Start the drag selection if the pointer has moved enough from the
		   initial clicking position.  */
		if (ABS (x - priv->button_press_x) < SELECTION_THRESHOLD
		    && ABS (y - priv->button_press_y) < SELECTION_THRESHOLD)
			return TRUE;
	
		priv->selection_active = TRUE;
	}

	f_image_view_window_coords_to_image (image_view, x, y, &image_x, &image_y);

	switch (priv->mode) {
	case MODE_DRAG_X1:
		priv->selection.x1 = image_x;
		break;

	case MODE_DRAG_X2:
		priv->selection.x2 = image_x;
		break;

	case MODE_DRAG_Y1:
		priv->selection.y1 = image_y;
		break;

	case MODE_DRAG_Y2:
		priv->selection.y2 = image_y;
		break;

	case MODE_DRAG_X1Y1:
		priv->selection.x1 = image_x;
		priv->selection.y1 = image_y;
		break;

	case MODE_DRAG_X1Y2:
		priv->selection.x1 = image_x;
		priv->selection.y2 = image_y;
		break;

	case MODE_DRAG_X2Y2:
		priv->selection.x2 = image_x;
		priv->selection.y2 = image_y;
		break;

	case MODE_DRAG_X2Y1:
		priv->selection.x2 = image_x;
		priv->selection.y1 = image_y;
		break;

	case MODE_MOVE: {
		double x_zoom, y_zoom;
		int x_offset, y_offset;
		int x1, x2, y1, y2;
		GdkPixbuf *pixbuf = image_view_get_pixbuf (IMAGE_VIEW (image_view));

		image_view_get_zoom (IMAGE_VIEW (image_view), &x_zoom, &y_zoom);

		x_offset = (x - priv->button_press_x) / x_zoom;
		y_offset = (y - priv->button_press_y) / y_zoom;

		x1 = MIN (priv->initial_selection.x1, priv->initial_selection.x2);
		x2 = MAX (priv->initial_selection.x1, priv->initial_selection.x2);
		y1 = MIN (priv->initial_selection.y1, priv->initial_selection.y2);
		y2 = MAX (priv->initial_selection.y1, priv->initial_selection.y2);

		x_offset = CLAMP (x_offset, -x1, gdk_pixbuf_get_width (pixbuf) - 1 - x2);
		y_offset = CLAMP (y_offset, -y1, gdk_pixbuf_get_height (pixbuf) - 1 - y2);

		priv->selection.x1 = priv->initial_selection.x1 + x_offset;
		priv->selection.y1 = priv->initial_selection.y1 + y_offset;
		priv->selection.x2 = priv->initial_selection.x2 + x_offset;
		priv->selection.y2 = priv->initial_selection.y2 + y_offset;

		g_object_unref (pixbuf);
		break;
	}

	default:
		g_assert_not_reached ();
	}

	constrain_selection (image_view);
	current = get_selection_box (image_view);

	draw_union (widget, &current, &previous);

	emit_selection_changed (image_view);
	return TRUE;
}

static gboolean
impl_button_release_event (GtkWidget *widget,
			   GdkEventButton *button_event)
{
	FImageViewPrivate *priv = F_IMAGE_VIEW (widget)->priv;

	if (priv->pointer_mode == F_IMAGE_VIEW_POINTER_MODE_SCROLL)
		return (* GTK_WIDGET_CLASS (parent_class)->button_release_event) (widget, button_event);

	priv->is_new_selection = FALSE;
	priv->mode = MODE_IDLE;

	set_cursor (F_IMAGE_VIEW (widget));

	return TRUE;
}


/* GObject methods.  */

static void
impl_finalize (GObject *object)
{
	FImageViewPrivate *priv = F_IMAGE_VIEW (object)->priv;

	g_free (priv);

	(* G_OBJECT_CLASS (parent_class)->finalize) (object);
}


/* Initialization.  */

static void
class_init (FImageViewClass *class)
{
	ImageViewClass *image_view_class = IMAGE_VIEW_CLASS (class);
	GtkWidgetClass *widget_class = GTK_WIDGET_CLASS (class);
	GObjectClass *object_class = G_OBJECT_CLASS (class);

	image_view_class->paint_extra = impl_paint_extra;

	widget_class->realize              = impl_realize;
	widget_class->unrealize            = impl_unrealize;
	widget_class->button_press_event   = impl_button_press_event;
	widget_class->motion_notify_event  = impl_motion_notify_event;
	widget_class->button_release_event = impl_button_release_event;
	widget_class->expose_event         = impl_expose_event;

	object_class->finalize = impl_finalize;

	parent_class = g_type_class_peek_parent (class);

	signals[SELECTION_CHANGED] = g_signal_new ("selection_changed",
						   G_TYPE_FROM_CLASS (class),
						   G_SIGNAL_RUN_LAST,
						   G_STRUCT_OFFSET (FImageViewClass, selection_changed),
						   NULL, NULL,
						   f_marshal_VOID__VOID,
						   G_TYPE_NONE, 0);
}

static void
init (FImageView *image_view)
{
	FImageViewPrivate *priv;

	priv = g_new0 (FImageViewPrivate, 1);
	priv->mode         = MODE_IDLE;
	priv->pointer_mode = F_IMAGE_VIEW_POINTER_MODE_SELECT;

	image_view->priv = priv;
}


/* Instantiation.  */

GtkWidget *
f_image_view_new (void)
{
	return g_object_new (f_image_view_get_type (), NULL);
}


/* Accessor functions.  */

void
f_image_view_set_pointer_mode (FImageView *image_view,
			       FImageViewPointerMode mode)
{
	image_view->priv->pointer_mode = mode;
    	set_cursor (image_view);
}

FImageViewPointerMode
f_image_view_get_pointer_mode (FImageView *image_view)
{
	return image_view->priv->pointer_mode;
}

void
f_image_view_set_selection_xy_ratio (FImageView *image_view,
				     gdouble selection_xy_ratio)
{
	FImageViewPrivate *priv = image_view->priv;
	GdkRectangle previous;
	GdkRectangle current;

	priv->selection_xy_ratio = selection_xy_ratio;

	previous = get_selection_box (image_view);
	constrain_selection (image_view);
	current = get_selection_box (image_view);
	draw_union (GTK_WIDGET (image_view), &current, &previous);

	emit_selection_changed (image_view);
}

gdouble
f_image_view_get_selection_xy_ratio (FImageView *image_view)
{
	return image_view->priv->selection_xy_ratio;
}


gboolean
f_image_view_get_selection (FImageView *image_view,
			    int *x_return, int *y_return,
			    int *width_return, int *height_return)
{
	FImageViewPrivate *priv = image_view->priv;

	if (! priv->selection_active) {
		*x_return = *y_return = 0;
		*width_return = *height_return = 0;
		return FALSE;
	}

	*x_return = MIN (priv->selection.x1, priv->selection.x2);
	*y_return = MIN (priv->selection.y1, priv->selection.y2);
	*width_return = ABS (priv->selection.x1 - priv->selection.x2);
	*height_return = ABS (priv->selection.y1 - priv->selection.y2);

	return TRUE;
}


void
f_image_view_unset_selection (FImageView *image_view)
{
	if (image_view->priv->selection_active) {
		image_view->priv->selection_active = FALSE;
		emit_selection_changed (image_view);
	}
}


F_MAKE_TYPE (FImageView, f_image_view)
