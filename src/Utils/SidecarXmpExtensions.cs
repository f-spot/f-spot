using System;
using System.IO;
using GLib;
using Hyena;
using TagLib.Image;
using TagLib.Xmp;

namespace FSpot.Utils
{
    public static class SidecarXmpExtensions
    {
        /// <summary>
        ///    Parses the XMP file identified by resource and replaces the XMP
        ///    tag of file by the parsed data.
        /// </summary>
        public static void ParseXmpSidecar (this TagLib.Image.File file, TagLib.File.IFileAbstraction resource)
        {
            string xmp;
            using (var stream = resource.ReadStream) {
                using (var reader = new StreamReader (stream)) {
                    xmp = reader.ReadToEnd ();
                }
            }

            var tag = new XmpTag (xmp);
            var xmp_tag = file.GetTag (TagLib.TagTypes.XMP, true) as XmpTag;
            xmp_tag.ReplaceFrom (tag);
        }

        public static void SaveXmpSidecar (this TagLib.Image.File file, TagLib.File.IFileAbstraction resource)
        {
            var xmp_tag = file.GetTag (TagLib.TagTypes.XMP, false) as XmpTag;
            if (xmp_tag == null)
                return;

            var xmp = xmp_tag.Render ();

            using (var stream = resource.WriteStream) {
                stream.SetLength (0);
                using (var writer = new StreamWriter (stream)) {
                    writer.Write (xmp);
                }
                resource.CloseStream (stream);
            }
        }
    }
}
