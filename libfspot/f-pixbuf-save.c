/* FSpot -- a photo management program
 *
 * f-pixbuf-save.c based on code from gimp and the GdkPixbuf jpeg save
 * routines.
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

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <errno.h>
#include <setjmp.h>
#include <unistd.h>
#include <sys/types.h>
#include <sys/stat.h>

#include <jpeglib.h>
#include <jerror.h>

#include <libexif/exif-data.h>
#include <gdk-pixbuf/gdk-pixbuf.h>


typedef struct {
	int     type;
	char   *data;
	int     length;
} FJpegMarker;

struct f_error_mgr
{
	struct jpeg_error_mgr pub;            /* "public" fields */
	
#ifdef __ia64__
	/* Ugh, the jmp_buf field needs to be 16-byte aligned on ia64 and some
	 * glibc/icc combinations don't guarantee this. So we pad. See bug #138357
	 * for details.
	 */
	long double           alignment_padding;
#endif
	
	jmp_buf               setjmp_buffer;  /* for return to caller */
};


static void
f_error_exit (j_common_ptr cinfo)
{
	struct f_error_mgr *err = (struct f_error_mgr *) cinfo->err;
	
	/* Always display the message. */
	/* We could postpone this until after returning, if we chose. */
	(*cinfo->err->output_message) (cinfo);
	
	/* Return control to the setjmp point */
	longjmp (err->setjmp_buffer, 1);
}

int 
f_pixbuf_save_jpeg (GdkPixbuf *pixbuf,
		    gchar *path, 
		    int quality,
		    FJpegMarker *marker, 
		    int num_markers)
{
	struct jpeg_compress_struct cinfo;
	struct f_error_mgr jerr;
	FILE   * outfile;
	int i = 0;

	g_object_ref (pixbuf);
	cinfo.err = jpeg_std_error (&jerr.pub);
	jerr.pub.error_exit = f_error_exit;

	outfile = NULL;

	/* Establish the setjmp return context for f_error_exit to use. */
	if (setjmp (jerr.setjmp_buffer)) {
		g_warning ("Error while saving file...");

		jpeg_destroy_compress (&cinfo);
		if (outfile) {
			fclose (outfile);
			unlink (path);
		}
		if (pixbuf)
			g_object_unref (pixbuf);
		
		return FALSE;
	}

	jpeg_create_compress (&cinfo);
	
	if ((outfile = fopen (path, "wb")) == NULL) {
		g_message ("Could not open '%s' for writing: %s",
			   path, g_strerror (errno));
		g_object_unref (pixbuf);
		return FALSE;
	}

	jpeg_stdio_dest (&cinfo, outfile);
	
	cinfo.input_components = 3;
	cinfo.image_width = gdk_pixbuf_get_width (pixbuf);
	cinfo.image_height = gdk_pixbuf_get_height (pixbuf);
	cinfo.in_color_space = JCS_RGB;

	jpeg_set_defaults (&cinfo);
	jpeg_set_quality (&cinfo, quality, TRUE);

	cinfo.comp_info[0].h_samp_factor = 2;
	cinfo.comp_info[0].v_samp_factor = 2;
	cinfo.comp_info[1].h_samp_factor = 1;
	cinfo.comp_info[1].v_samp_factor = 1;
	cinfo.comp_info[2].h_samp_factor = 1;
	cinfo.comp_info[2].v_samp_factor = 1;
	
	cinfo.dct_method = JDCT_ISLOW;

	jpeg_start_compress (&cinfo, TRUE);
	
	/* Add all the markers that were passed in */
	while (i < num_markers) {
		g_warning ("adding marker: %d, %s", marker [i].type, marker [i].data);
		jpeg_write_marker (&cinfo, marker [i].type, marker [i].data, marker [i].length);
		i++;
	}

	if (gdk_pixbuf_get_has_alpha (pixbuf)) {
		// FIXME handle alpha case.
		g_object_unref (pixbuf);
		fclose (outfile);
		return FALSE;
	}

	while (cinfo.next_scanline < cinfo.image_height) {
		guchar *data = gdk_pixbuf_get_pixels (pixbuf) + (cinfo.next_scanline * gdk_pixbuf_get_rowstride (pixbuf));
		jpeg_write_scanlines (&cinfo, &data, 1);
	}
	
	jpeg_finish_compress (&cinfo);
	jpeg_destroy_compress (&cinfo);

	fclose (outfile);
	g_object_unref (pixbuf);

	return TRUE;
}
