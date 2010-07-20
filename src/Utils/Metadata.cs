using Hyena;
using TagLib;
using System;
using GLib;

namespace FSpot.Utils
{
    public static class Metadata
    {
        public static TagLib.Image.File Parse (SafeUri uri)
        {
            // Detect mime-type
            var gfile = FileFactory.NewForUri (uri);
            var info = gfile.QueryInfo ("standard::content-type", FileQueryInfoFlags.None, null);
            var mime = info.ContentType;

            // Parse file
            var res = new GIOTagLibFileAbstraction () { Uri = uri };
            var sidecar_uri = uri.ReplaceExtension (".xmp");
            var sidecar_res = new GIOTagLibFileAbstraction () { Uri = sidecar_uri };

            TagLib.Image.File file = null;
            try {
                file = TagLib.File.Create (res, mime, ReadStyle.Average) as TagLib.Image.File;
            } catch (Exception e) {
                Hyena.Log.Exception (String.Format ("Loading of Metadata failed for file: {0}", uri.ToString ()), e);
                return null;
            }

            // Load XMP sidecar
            var sidecar_file = GLib.FileFactory.NewForUri (sidecar_uri);
            if (sidecar_file.Exists) {
                file.ParseXmpSidecar (sidecar_res);
            }

            return file;
        }
    }
}
