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
#include <lcms2.h>
#include <lcms2_plugin.h>

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
int bchswSampler(register const cmsUInt16Number In[], register cmsUInt16Number Out[], register void* Cargo)
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

cmsHPROFILE CMSEXPORT f_cmsCreateBCHSWabstractProfile(int nLUTPoints,
						       double Exposure,
						       double Bright,
						       double Contrast,
						       double Hue,
						       double Saturation,
						       cmsCIExyY * current_wp,
						      cmsCIExyY * destination_wp,
						      cmsToneCurve * Curves [])
{
	cmsHPROFILE hICC;
	cmsPipeline* Pipeline;
	BCHSWADJUSTS bchsw;
	cmsCIExyY WhitePnt;
	cmsStage* CLUT, * gammaCorrection;
	cmsUInt32Number Dimensions[MAX_INPUT_DIMENSIONS];
	int i;

	bchsw.Brightness = Bright;
	bchsw.Contrast   = Contrast;
	bchsw.Hue        = Hue;
	bchsw.Saturation = Saturation;
	bchsw.Exposure   = Exposure;

	cmsxyY2XYZ(&bchsw.WPsrc, current_wp);
	cmsxyY2XYZ(&bchsw.WPdest, destination_wp);

	hICC = cmsCreateProfilePlaceholder(NULL);
	if (!hICC)                          // can't allocate
		return NULL;

	cmsSetDeviceClass(hICC,      cmsSigAbstractClass);
	cmsSetColorSpace(hICC,       cmsSigLabData);
	cmsSetPCS(hICC,              cmsSigLabData);

	cmsSetHeaderRenderingIntent(hICC,  INTENT_PERCEPTUAL);

	// Creates a Pipeline with 3D grid only
	Pipeline = cmsPipelineAlloc(NULL, 3, 3);
	if (Pipeline == NULL) {
		cmsCloseProfile(hICC);
		return NULL;
	}

	for (i=0; i < MAX_INPUT_DIMENSIONS; i++) Dimensions[i] = nLUTPoints;
	CLUT = cmsStageAllocCLut16bitGranular(NULL, Dimensions, 3, 3, NULL);
	if (CLUT == NULL) return NULL;

	if (Curves != NULL) {
	  gammaCorrection = cmsStageAllocToneCurves(NULL, 3, Curves);
	  cmsPipelineInsertStage(Pipeline, cmsAT_END, gammaCorrection);
	}

	if (!cmsStageSampleCLut16bit(CLUT, bchswSampler, (void*) &bchsw, 0)) {

	       // Shouldn't reach here
	       cmsPipelineFree(Pipeline);
	       cmsCloseProfile(hICC);
	       return NULL;
       }

	cmsPipelineInsertStage(Pipeline, cmsAT_END, CLUT);

       // Create tags
       cmsWriteTag(hICC, cmsSigDeviceMfgDescTag, (void*) "(f-spot internal)");
       cmsWriteTag(hICC, cmsSigProfileDescriptionTag, (void*) "f-spot BCHSW abstract profile");
       cmsWriteTag(hICC, cmsSigDeviceModelDescTag,    (void*) "BCHSW built-in");
       cmsWriteTag(hICC, cmsSigMediaWhitePointTag, (void*) cmsD50_XYZ());
       cmsWriteTag(hICC, cmsSigAToB0Tag, (void*) Pipeline);

       // Pipeline is already on virtual profile
       cmsPipelineFree(Pipeline);

       // Ok, done
       return hICC;
}
