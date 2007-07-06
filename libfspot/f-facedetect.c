#include "cv.h"
#include "highgui.h"
#include "f-image-view.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <assert.h>
#include <math.h>
#include <float.h>
#include <limits.h>
#include <time.h>
#include <ctype.h>

#ifdef _EiC
#define WIN32
#endif

static CvMemStorage* storage = 0;
static CvHaarClassifierCascade* cascade = 0;

GdkRegion * detect_and_draw( IplImage* image, FImageView *image_view );


const char* cascade_name =
    "haarcascade_frontalface_alt.xml";
/*    "haarcascade_profileface.xml";*/

int f_detect (char *cascadeName, 
		char *fileName,
		FImageView *image_view)
{
    CvCapture* capture = 0;
    IplImage *frame, *frame_copy = 0;
    int optlen = strlen("--cascade=");
    const char* input_name;
	printf("CASCADE %s\n FILENAME %s\n",cascadeName, fileName);
//    if( strncmp( cascadeName, "--cascade=", optlen ) == 0 )
//    {
//        cascade_name = cascadeName + optlen;
//        input_name = argc > 2 ? argv[2] : 0;
//    }
	if(strlen(cascadeName))
	{
	   cascade_name = cascadeName;
	   input_name	= fileName;
	}
    else
    {
        cascade_name = "/usr/share/opencv/haarcascades/haarcascade_frontalface_default.xml";
        input_name = fileName;
    }

    cascade = (CvHaarClassifierCascade*)cvLoad( cascade_name, 0, 0, 0 );
    
    if( !cascade )
    {
        fprintf( stderr, "ERROR: Could not load classifier cascade\n" );
        fprintf( stderr,
        "Usage: facedetect --cascade=\"<cascade_path>\" [filename|camera_index]\n" );
        return -1;
    }
    storage = cvCreateMemStorage(0);
    
    cvNamedWindow( "result", 1 );

        const char* filename = input_name ? input_name : (char*)"lena.jpg";
        //IplImage* image = cvLoadImage( filename, 1 );
	GdkPixbuf *pixbuf = image_view_get_pixbuf (IMAGE_VIEW(image_view));
	int width = gdk_pixbuf_get_width (pixbuf);
	int height = gdk_pixbuf_get_height (pixbuf);
	cairo_surface_t *surface = f_pixbuf_to_cairo_surface (pixbuf);

        IplImage* image = cvCreateImage( cvSize (width, height), 8, 4);
	guchar *src = f_image_surface_get_data (surface);
	guchar *dest = image->imageData;
	
	int i;
	for (i = 0; i < height; i++) {
		memcpy (dest, src, width * 4);
		dest += image->widthStep;
		src += 4;
	}
	
        if( image )
        {
            detect_and_draw( image, image_view );
            cvWaitKey(0);
            cvReleaseImage( &image );
        }

    
 //   cvDestroyWindow("result");

    return 0;
}

GdkRegion *
detect_and_draw( IplImage* img, FImageView *image_view)
{
	GdkRectangle zone, zone1;
	cairo_t *ctx;
	GdkRegion *selection;
	GdkRegion *other;
	GdkRectangle rect;

    static CvScalar colors[] = 
    {
        {{0,0,255}},
        {{0,128,255}},
        {{0,255,255}},
        {{0,255,0}},
        {{255,128,0}},
        {{255,255,0}},
        {{255,0,0}},
        {{255,0,255}}
    };

    double scale = 1.000001;
    IplImage* gray = cvCreateImage( cvSize(img->width,img->height), 8, 1 );
    IplImage* small_img = cvCreateImage( cvSize( cvRound (img->width/scale),
						 cvRound (img->height/scale)),
					 8, 1 );
    int i;

    cvCvtColor( img, gray, CV_BGR2GRAY );
    cvResize( gray, small_img, CV_INTER_LINEAR );
    cvEqualizeHist( small_img, small_img );
    cvClearMemStorage( storage );
    if( cascade )
    {
	other = gdk_region_new ();
        double t = (double)cvGetTickCount();
        CvSeq* faces = cvHaarDetectObjects( small_img, cascade, storage,
                                            1.1, 2, 0/*CV_HAAR_DO_CANNY_PRUNING*/,
                                            cvSize(30, 30) );
        t = (double)cvGetTickCount() - t;
        printf( "detection time = %gms\n", t/((double)cvGetTickFrequency()*1000.) );

	
        for( i = 0; i < (faces ? faces->total : 0); i++ )
        {
            CvRect* r = (CvRect*)cvGetSeqElem( faces, i );
	    printf("%d\n%d\n",i,faces->total);
	    printf("%d\n%d\n",r->x,r->y);
	    printf("%d\n%d\n",r->width, r->height);
	    image_coords_to_window(image_view, r->x, r->y, &zone.x, &zone.y);
	    int x2, y2;
	    image_coords_to_window(image_view, r->x + r->width, r->y + r->height, &x2, &y2);
	    zone.width = x2 - zone.x;
	    zone.height = y2 - zone.y;
	    printf("rect (%d %d %d %d) zone (%d %d %d %d) \n", r->x, r->y, r->width, r->height, zone.x, zone.y, zone.width, zone.height);
	    gdk_region_union_with_rect (other, &zone);

            CvPoint center;
            int radius;
            center.x = cvRound((r->x + r->width*0.5)*scale);
            center.y = cvRound((r->y + r->height*0.5)*scale);
            radius = cvRound((r->width + r->height)*0.25*scale);
            cvCircle( img, center, radius, colors[i%8], 3, 8, 0 );
        }
    }

    cvShowImage( "result", img );
	ctx = gdk_cairo_create (GTK_WIDGET (image_view)->window);
	cairo_set_source_rgba (ctx, .5, .2, .2, .7);
	gdk_cairo_region (ctx, other);
	cairo_fill (ctx);
	cairo_destroy (ctx);
    gdk_region_destroy (other);

    cvReleaseImage( &gray );
    cvReleaseImage( &small_img );
    
    return other;
}
