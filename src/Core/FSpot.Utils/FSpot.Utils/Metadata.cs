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
            string mime;
            try {
                var gfile = FileFactory.NewForUri (uri);
                var info = gfile.QueryInfo ("standard::content-type", FileQueryInfoFlags.None, null);
                mime = info.ContentType;
            } catch (Exception e) {
                Hyena.Log.DebugException (e);
                return null;
            }

            if (mime.StartsWith ("application/x-extension-")) {
                // Works around broken metadata detection - https://bugzilla.gnome.org/show_bug.cgi?id=624781
                mime = String.Format ("taglib/{0}", mime.Substring (24));
            }

            // Parse file
            var res = new GIOTagLibFileAbstraction () { Uri = uri };
            var sidecar_uri = GetSidecarUri (uri);
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

        public static void SaveSafely (this TagLib.Image.File metadata, SafeUri photo_uri, bool always_sidecar)
        {
            if (always_sidecar || !metadata.Writeable || metadata.PossiblyCorrupt) {
                if (!always_sidecar && metadata.PossiblyCorrupt) {
                    Hyena.Log.WarningFormat ("Metadata of file {0} may be corrupt, refusing to write to it, falling back to XMP sidecar.", photo_uri);
                }

                var sidecar_res = new GIOTagLibFileAbstraction () { Uri = GetSidecarUri (photo_uri) };

                metadata.SaveXmpSidecar (sidecar_res);
            } else {
                metadata.Save ();
            }
        }

        private delegate SafeUri GenerateSideCarName (SafeUri photo_uri);
        private static GenerateSideCarName[] SidecarNameGenerators = {
            (p) => new SafeUri (p.AbsoluteUri + ".xmp"),
            (p) => p.ReplaceExtension (".xmp"),
        };

        public static SafeUri GetSidecarUri (SafeUri photo_uri)
        {
            // First probe for existing sidecar files, use the one that's found.
            foreach (var generator in SidecarNameGenerators) {
                var name = generator (photo_uri);
                var file = GLib.FileFactory.NewForUri (name);
                if (file.Exists) {
                    return name;
                }
            }
            

            // Fall back to the default strategy.
            return SidecarNameGenerators[0] (photo_uri);
        }
    }
}
