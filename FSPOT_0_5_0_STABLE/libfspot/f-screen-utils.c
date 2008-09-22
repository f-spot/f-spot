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

typedef struct {
	double Exposure;
	double Brightness;
	double Contrast;
	double Hue;
	double Saturation;
	cmsCIEXYZ WPsrc, WPdest;
	
} BCHSWADJUSTS, *LPBCHSWADJUSTS;


static
int bchswSampler(register WORD In[], register WORD Out[], register LPVOID Cargo)
{
    cmsCIELab LabIn, LabOut;
    cmsCIELCh LChIn, LChOut;
    cmsCIEXYZ XYZ;
    double l;
    double power;
    gboolean shift;

    LPBCHSWADJUSTS bchsw = (LPBCHSWADJUSTS) Cargo;    

    cmsLabEncoded2Float(&LabIn, In);
         // Move white point in Lab

    cmsLab2XYZ(&bchsw ->WPsrc,  &XYZ, &LabIn);
    cmsXYZ2Lab(&bchsw ->WPdest, &LabIn, &XYZ);

    shift = (LabIn.L > 0.5);
    l = LabIn.L / 100;
    if (shift)
	    l = 1.0 - l;

    if (l < 0.0)
	    l = 0.0;

    if (bchsw->Contrast < 0)
	    power = 1.0 + bchsw->Contrast;
    else
	    power = (bchsw->Contrast == 1.0) ? 127 : 1.0 / (1.0 - bchsw->Contrast);
	    
    l = 0.5 * pow (l * 2.0 , power);

    if (shift)
	    l = 1.0 - l;

    LabIn.L = l * 100;

    cmsLab2LCh(&LChIn, &LabIn);

    // Do some adjusts on LCh
    
    LChOut.L = LChIn.L * bchsw ->Exposure + bchsw ->Brightness;

    LChOut.C = MAX (0, LChIn.C + bchsw ->Saturation);
    LChOut.h = LChIn.h + bchsw ->Hue;
    
    cmsLCh2Lab(&LabOut, &LChOut);

    // Back to encoded

    cmsFloat2LabEncoded(Out, &LabOut);
    

    return TRUE;
}


// Creates an abstract profile operating in Lab space for Brightness,
// contrast, Saturation and white point displacement

cmsHPROFILE LCMSEXPORT f_cmsCreateBCHSWabstractProfile(int nLUTPoints,
						       double Exposure,
						       double Bright, 
						       double Contrast,
						       double Hue,
						       double Saturation,
						       LPcmsCIExyY current_wp,
						       LPcmsCIExyY destination_wp,
						       LPGAMMATABLE Tables [])
{
	cmsHPROFILE hICC;
	LPLUT Lut;
	BCHSWADJUSTS bchsw;
	cmsCIExyY WhitePnt;
	
	bchsw.Exposure   = Exposure;
	bchsw.Brightness = Bright;
	bchsw.Contrast   = Contrast;
	bchsw.Hue        = Hue;
	bchsw.Saturation = Saturation;
	
	cmsxyY2XYZ(&bchsw.WPsrc, current_wp);
	cmsxyY2XYZ(&bchsw.WPdest, destination_wp);
	
	hICC = _cmsCreateProfilePlaceholder();
	if (!hICC)                          // can't allocate
		return NULL;
	
	cmsSetDeviceClass(hICC,      icSigAbstractClass);
	cmsSetColorSpace(hICC,       icSigLabData);
	cmsSetPCS(hICC,              icSigLabData);
	
	cmsSetRenderingIntent(hICC,  INTENT_PERCEPTUAL); 
	
	// Creates a LUT with 3D grid only
	Lut = cmsAllocLUT();
	
	cmsAlloc3DGrid(Lut, nLUTPoints, 3, 3);
	
	if (Tables != NULL)
	       cmsAllocLinearTable (Lut, Tables, 1);
	
       if (!cmsSample3DGrid(Lut, bchswSampler, (LPVOID) &bchsw, 0)) {
	       
	       // Shouldn't reach here
	       cmsFreeLUT(Lut);
	       cmsCloseProfile(hICC);
	       return NULL;
       }    
       
       // Create tags
       
       cmsAddTag(hICC, icSigDeviceMfgDescTag,      (LPVOID) "(f-spot internal)"); 
       cmsAddTag(hICC, icSigProfileDescriptionTag, (LPVOID) "f-spot BCHSW abstract profile");  
       cmsAddTag(hICC, icSigDeviceModelDescTag,    (LPVOID) "BCHSW built-in");      
       
       cmsAddTag(hICC, icSigMediaWhitePointTag, (LPVOID) cmsD50_XYZ());
       
       cmsAddTag(hICC, icSigAToB0Tag, (LPVOID) Lut);
       
       // LUT is already on virtual profile
       cmsFreeLUT(Lut);

       // Ok, done
       return hICC;
}

LPGAMMATABLE
f_cms_gamma_table_new (unsigned short data[], int start, int length)
{
	LPGAMMATABLE table = cmsAllocGamma (length);
	int i;
	if (!table)
		return NULL;

	data += start;

	for (i = 0; i < length; i++)
		table->GammaTable [i] = data [i];

	g_warning ("table %p, count = %d v[0] = %d", table, table->nEntries, table->GammaTable [0]);
	
	return table;
}

ushort *
f_cms_gamma_table_get_values (LPGAMMATABLE table)
{
	return &(table->GammaTable [0]);
}

int
f_cms_gamma_table_get_count (LPGAMMATABLE table)
{
	return table->nEntries;
}
