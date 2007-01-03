/*
 * f-pixbuf-unsharp.c 
 *  -- This is adapted from a plug-in for the GIMP 1.0
 *  http://www.stdout.org/~winston/gimp/unsharp.html
 *  (now out of date) by
 *
 * Copyright (C) 1999 Winston Chang
 *                    <winstonc@cs.wisc.edu>
 *                    <winston@stdout.org>
 *
 * Copyright 2004 Novell, Inc.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 */

#include <config.h>

#include <math.h>
#include <stdio.h>
#include <errno.h>
#include <stdlib.h>

#include <gtk/gtk.h>
#include <gdk-pixbuf/gdk-pixbuf.h>

#define ROUND(x) ((int) ((x) + 0.5))

/* ----------------------- gen_lookup_table ----------------------- */
/* generates a lookup table for every possible product of 0-255 and
   each value in the convolution matrix.  The returned array is
   indexed first by matrix position, then by input multiplicand (?)
   value.
*/
static gdouble *
gen_lookup_table (gdouble *cmatrix,
		  gint     cmatrix_length)
{
  int i, j;
  gdouble* lookup_table = g_new (gdouble, cmatrix_length * 256);
  gdouble* lookup_table_p = lookup_table;
  gdouble* cmatrix_p      = cmatrix;

  for (i=0; i<cmatrix_length; i++)
    {
      for (j=0; j<256; j++)
	{
	  *(lookup_table_p++) = *cmatrix_p * (gdouble)j;
	}
      cmatrix_p++;
    }

  return lookup_table;
}

/* generates a 1-D convolution matrix to be used for each pass of
 * a two-pass gaussian blur.  Returns the length of the matrix.
 */
static gint
gen_convolve_matrix (gdouble   radius,
		     gdouble **cmatrix_p)
{
  gint matrix_length;
  gint matrix_midpoint;
  gdouble* cmatrix;
  gint i,j;
  gdouble std_dev;
  gdouble sum;

  /* we want to generate a matrix that goes out a certain radius
   * from the center, so we have to go out ceil(rad-0.5) pixels,
   * inlcuding the center pixel.  Of course, that's only in one direction,
   * so we have to go the same amount in the other direction, but not count
   * the center pixel again.  So we double the previous result and subtract
   * one.
   * The radius parameter that is passed to this function is used as
   * the standard deviation, and the radius of effect is the
   * standard deviation * 2.  It's a little confusing.
   */
  radius = fabs(radius) + 1.0;

  std_dev = radius;
  radius = std_dev * 2;

  /* go out 'radius' in each direction */
  matrix_length = 2 * ceil(radius-0.5) + 1;
  if (matrix_length <= 0) matrix_length = 1;
  matrix_midpoint = matrix_length/2 + 1;
  *cmatrix_p = g_new (gdouble, matrix_length);
  cmatrix = *cmatrix_p;

  /*  Now we fill the matrix by doing a numeric integration approximation
   * from -2*std_dev to 2*std_dev, sampling 50 points per pixel.
   * We do the bottom half, mirror it to the top half, then compute the
   * center point.  Otherwise asymmetric quantization errors will occur.
   *  The formula to integrate is e^-(x^2/2s^2).
   */

  /* first we do the top (right) half of matrix */
  for (i = matrix_length/2 + 1; i < matrix_length; i++)
    {
      double base_x = i - floor(matrix_length/2) - 0.5;
      sum = 0;
      for (j = 1; j <= 50; j++)
	{
	  if ( base_x+0.02*j <= radius )
	    sum += exp (-(base_x+0.02*j)*(base_x+0.02*j) /
			(2*std_dev*std_dev));
	}
      cmatrix[i] = sum/50;
    }

  /* mirror the thing to the bottom half */
  for (i=0; i<=matrix_length/2; i++) {
    cmatrix[i] = cmatrix[matrix_length-1-i];
  }

  /* find center val -- calculate an odd number of quanta to make it symmetric,
   * even if the center point is weighted slightly higher than others. */
  sum = 0;
  for (j=0; j<=50; j++)
    {
      sum += exp (-(0.5+0.02*j)*(0.5+0.02*j) /
		  (2*std_dev*std_dev));
    }
  cmatrix[matrix_length/2] = sum/51;

  /* normalize the distribution by scaling the total sum to one */
  sum=0;
  for (i=0; i<matrix_length; i++) sum += cmatrix[i];
  for (i=0; i<matrix_length; i++) cmatrix[i] = cmatrix[i] / sum;

  return matrix_length;
}

static inline void
blur_line (gdouble *ctable,
	   gdouble *cmatrix,
	   gint     cmatrix_length,
	   guchar  *cur_col,
	   guchar  *dest_col,
	   gint     y,
	   glong    bytes)
{
  gdouble scale;
  gdouble sum;
  gint i=0, j=0;
  gint row;
  gint cmatrix_middle = cmatrix_length/2;

  gdouble *cmatrix_p;
  guchar  *cur_col_p;
  guchar  *cur_col_p1;
  guchar  *dest_col_p;
  gdouble *ctable_p;

  /* this first block is the same as the non-optimized version --
   * it is only used for very small pictures, so speed isn't a
   * big concern.
   */
  if (cmatrix_length > y)
    {
      for (row = 0; row < y ; row++)
	{
	  scale=0;
	  /* find the scale factor */
	  for (j = 0; j < y ; j++)
	    {
	      /* if the index is in bounds, add it to the scale counter */
	      if ((j + cmatrix_length/2 - row >= 0) &&
		  (j + cmatrix_length/2 - row < cmatrix_length))
		scale += cmatrix[j + cmatrix_length/2 - row];
	    }
	  for (i = 0; i<bytes; i++)
	    {
	      sum = 0;
	      for (j = 0; j < y; j++)
		{
		  if ((j >= row - cmatrix_length/2) &&
		      (j <= row + cmatrix_length/2))
		    sum += cur_col[j*bytes + i] * cmatrix[j];
		}
	      dest_col[row*bytes + i] = (guchar) ROUND (sum / scale);
	    }
	}
    }
  else
    {
      /* for the edge condition, we only use available info and scale to one */
      for (row = 0; row < cmatrix_middle; row++)
	{
	  /* find scale factor */
	  scale=0;
	  for (j = cmatrix_middle - row; j<cmatrix_length; j++)
	    scale += cmatrix[j];
	  for (i = 0; i<bytes; i++)
	    {
	      sum = 0;
	      for (j = cmatrix_middle - row; j<cmatrix_length; j++)
		{
		  sum += cur_col[(row + j-cmatrix_middle)*bytes + i] * cmatrix[j];
		}
	      dest_col[row*bytes + i] = (guchar) ROUND (sum / scale);
	    }
	}
      /* go through each pixel in each col */
      dest_col_p = dest_col + row*bytes;
      for (; row < y-cmatrix_middle; row++)
	{
	  cur_col_p = (row - cmatrix_middle) * bytes + cur_col;
	  for (i = 0; i<bytes; i++)
	    {
	      sum = 0;
	      cmatrix_p = cmatrix;
	      cur_col_p1 = cur_col_p;
	      ctable_p = ctable;
	      for (j = cmatrix_length; j>0; j--)
		{
		  sum += *(ctable_p + *cur_col_p1);
		  cur_col_p1 += bytes;
		  ctable_p += 256;
		}
	      cur_col_p++;
	      *(dest_col_p++) = ROUND (sum);
	    }
	}

      /* for the edge condition , we only use available info, and scale to one */
      for (; row < y; row++)
	{
	  /* find scale factor */
	  scale=0;
	  for (j = 0; j< y-row + cmatrix_middle; j++)
	    scale += cmatrix[j];
	  for (i = 0; i<bytes; i++)
	    {
	      sum = 0;
	      for (j = 0; j<y-row + cmatrix_middle; j++)
		{
		  sum += cur_col[(row + j-cmatrix_middle)*bytes + i] * cmatrix[j];
		}
	      dest_col[row*bytes + i] = (guchar) ROUND (sum / scale);
	    }
	}
    }
}

static guchar *
pixbuf_get_row (GdkPixbuf *buf, int row)
{
  guchar *pixels = gdk_pixbuf_get_pixels (buf);
  int stride = gdk_pixbuf_get_rowstride (buf);
  
  return pixels + row * stride;
}

static void
pixbuf_get_column (GdkPixbuf *buf, guchar *dest, int col)
{
  guchar *pixels = gdk_pixbuf_get_pixels (buf);
  int stride  = gdk_pixbuf_get_rowstride (buf);
  int height = gdk_pixbuf_get_height (buf);
  int n = gdk_pixbuf_get_n_channels (buf);
  guchar *cur;

  cur = pixels + col * n;
  if (n == 3)
    while (height--) {
      dest[0] = cur[0];
      dest[1] = cur[1];
      dest[2] = cur[2];
      dest += 3;
      cur += stride;
    }
  else if (n == 4)
     while (height--) {
      dest[0] = cur[0];
      dest[1] = cur[1];
      dest[2] = cur[2];
      dest[3] = cur[3];
      dest += 4;
      cur += stride;   
    }
}

static void
pixbuf_set_column (GdkPixbuf *buf, guchar *src, int col)
{
  guchar *pixels = gdk_pixbuf_get_pixels (buf);
  int stride  = gdk_pixbuf_get_rowstride (buf);
  int height = gdk_pixbuf_get_height (buf);
  int n = gdk_pixbuf_get_n_channels (buf);
  guchar *dest;

  dest = pixels + col * n;
  if (n == 3)
    while (height--) {
      dest[0] = src[0];
      dest[1] = src[1];
      dest[2] = src[2];
      src += 3;
      dest += stride;
    }
  else if (n == 4)
    while (height--) {
      dest[0] = src[0];
      dest[1] = src[1];
      dest[2] = src[2];
      dest[3] = src[3];
      src += 4;
      dest += stride;
    }
}

GdkPixbuf * f_pixbuf_unsharp_l_mask (GdkPixbuf *src_buf, 
				     double radius, 
				     double amount, 
				     double threshold)
{
  GdkPixbuf *dest_buf;
  int width = gdk_pixbuf_get_width (src_buf);
  int height = gdk_pixbuf_get_height (src_buf);
  int channels = gdk_pixbuf_get_n_channels (src_buf);
  int i;
  int row;
  gdouble *cmatrix = NULL;
  gint     cmatrix_length;
  gdouble *ctable;
  guchar *src;
  guchar *dest;
  int span = channels * width;
  int diff;
  int value;

  dest_buf = gdk_pixbuf_new (GDK_COLORSPACE_RGB,
			     gdk_pixbuf_get_has_alpha (src_buf),
			     8,
			     width,
			     height);

  cmatrix_length = gen_convolve_matrix (radius, &cmatrix);
  ctable = gen_lookup_table (cmatrix, cmatrix_length);
  
  /* walk the columns */
  for (i = 0; i < height; i++) {
    src  = pixbuf_get_row (src_buf, i);
    dest = pixbuf_get_row (dest_buf, i);
    blur_line (ctable, cmatrix, cmatrix_length, src, dest, width, channels);
  }
  g_free (src);
  g_free (dest);

  /* walk the rows */
  src  = g_new (guchar, height * channels);
  dest = g_new (guchar, height * channels);
  for (i = 0; i < width; i++) {
    pixbuf_get_column (src_buf, src, i);
    pixbuf_get_column (dest_buf, dest, i);

    blur_line (ctable, cmatrix, cmatrix_length, src, dest, height, channels);
    pixbuf_set_column (dest_buf, dest, i);
  }
  g_free (src);
  g_free (dest);
  
  /* threshold the values */
  for (row = 0; row < height; row++) {
    src  = pixbuf_get_row (src_buf, row);
    dest = pixbuf_get_row (dest_buf, row);
    
    for (i = 0; i < span; i += channels) {
      diff = src[i] - dest[i];
      if (abs (2 * diff) < threshold)
	diff = 0;

      value = src[i] + amount * diff;
      dest[i] = (guchar)CLAMP (value, 0x00, 0xff);
    }
  }

  return dest_buf;
}

GdkPixbuf *
f_pixbuf_blur (GdkPixbuf *src_buf, double radius)
{
  GdkPixbuf *dest_buf;
  int width = gdk_pixbuf_get_width (src_buf);
  int height = gdk_pixbuf_get_height (src_buf);
  int channels = gdk_pixbuf_get_n_channels (src_buf);
  int i;
  int row;
  gdouble *cmatrix = NULL;
  gint     cmatrix_length;
  gdouble *ctable;
  guchar *src;
  guchar *dest;
  int span = channels * width;

  dest_buf = gdk_pixbuf_new (GDK_COLORSPACE_RGB,
			     gdk_pixbuf_get_has_alpha (src_buf),
			     8,
			     width,
			     height);

  cmatrix_length = gen_convolve_matrix (radius, &cmatrix);
  ctable = gen_lookup_table (cmatrix, cmatrix_length);
  
  gdk_pixbuf_fill (dest_buf, 0);

  /* walk the columns */
  for (i = 0; i < height; i++) {
    src  = pixbuf_get_row (src_buf, i);
    dest = pixbuf_get_row (dest_buf, i);
    blur_line (ctable, cmatrix, cmatrix_length, src, dest, width, channels);
  }
  
  /* walk the rows */
  src  = g_new (guchar, height * channels);
  dest = g_new (guchar, height * channels);
  for (i = 0; i < width; i++) {
    pixbuf_get_column (src_buf, src, i);
    pixbuf_get_column (dest_buf, dest, i);

    blur_line (ctable, cmatrix, cmatrix_length, src, dest, height, channels);
    pixbuf_set_column (dest_buf, dest, i);
  }
  g_free (src);
  g_free (dest);

  return dest_buf;
}

GdkPixbuf *
f_pixbuf_unsharp_mask (GdkPixbuf *src_buf, 
		       double radius, 
		       double amount, 
		       double threshold)
{
  GdkPixbuf *dest_buf;
  int width = gdk_pixbuf_get_width (src_buf);
  int height = gdk_pixbuf_get_height (src_buf);
  int channels = gdk_pixbuf_get_n_channels (src_buf);
  guchar *src;
  guchar *dest;
  int i;
  int row;
  int span = channels * width;
  int diff;
  int value;

  dest_buf = f_pixbuf_blur (src_buf, radius);
  
  /* threshold the values */
  for (row = 0; row < height; row++) {
    src  = pixbuf_get_row (src_buf, row);
    dest = pixbuf_get_row (dest_buf, row);
    
    for (i = 0; i < span; i++) {
      diff = src[i] - dest[i];
      if (abs (2 * diff) < threshold)
	diff = 0;

      value = src[i] + amount * diff;
      dest[i] = (guchar)CLAMP (value, 0x00, 0xff);
    }
  }

  return dest_buf;
}
