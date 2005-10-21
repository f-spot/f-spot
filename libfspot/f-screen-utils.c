/* this is taken from recent versions of libeog based on Ross Burton's patch */
/* 
 * right now we return the parsed profile rather than the data to avoid wrapping the XFree
 * as well.
 */
#include <config.h>
#include <gtk/gtk.h>
#include <X11/Xlib.h>
#include <X11/Xatom.h>
#include <gdk/gdkx.h>
#include <lcms.h>

cmsHPROFILE *
f_screen_get_profile (GdkScreen *screen)
{
	Display *dpy;
	Atom icc_atom, type;
	int format;
	gulong nitems;
	gulong bytes_after;
	guchar *str;
	int result;
	cmsHPROFILE *profile;
	
	dpy = GDK_DISPLAY_XDISPLAY (gdk_screen_get_display (screen));
	icc_atom = gdk_x11_get_xatom_by_name_for_display (gdk_screen_get_display (screen), "_ICC_PROFILE");
	
	result = XGetWindowProperty (dpy, GDK_WINDOW_XID (gdk_screen_get_root_window (screen)),
				     icc_atom, 0, G_MAXLONG,
				     False, XA_CARDINAL, &type, &format, &nitems,
				     &bytes_after, (guchar **)&str);
	/* TODO: handle bytes_after != 0 */
	
	if (nitems) {
		profile = cmsOpenProfileFromMem(str, nitems);
		XFree (str);
		return profile;
	} else
		return NULL;
}
