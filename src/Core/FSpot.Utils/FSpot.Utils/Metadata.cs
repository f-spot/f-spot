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

            if (mime.StartsWith ("application/x-extension-")) {
                // Works around broken metadata detection - https://bugzilla.gnome.org/show_bug.cgi?id=624781
                mime = String.Format ("taglib/{0}", mime.Substring (24));
            }

            // Parse file
            var res = new GIOTagLibFileAbstraction () { Uri = uri };
            var sidecar_uri = uri.ReplaceExtension (".xmp");
            var sidecar_res = new GIOTagLibFileAbstraction () { Uri = sidecar_uri };

            TagLib.Image.File file = null;
            try {
                file = TagLib.File.Create (res, mime, ReadStyle.Average) as TagLib.Image.File;
            } catch (Exception) {
                Hyena.Log.DebugFormat ("Loading of metadata failed for file: {0}, trying extension fallback", uri);
                
                try {
                    file = TagLib.File.Create (res, ReadStyle.Average) as TagLib.Image.File;
                } catch (Exception e) {
                    Hyena.Log.DebugFormat ("Loading of metadata failed for file: {0}", uri);
                    Hyena.Log.DebugException (e);
                    return null;
                }
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
