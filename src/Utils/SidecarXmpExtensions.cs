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
        public static bool ParseXmpSidecar (this TagLib.Image.File file, TagLib.File.IFileAbstraction resource)
        {
            string xmp;

            try {
                using (var stream = resource.ReadStream) {
                    using (var reader = new StreamReader (stream)) {
                        xmp = reader.ReadToEnd ();
                    }
                }
            } catch (Exception e) {
                Hyena.Log.Exception (String.Format ("Sidecar cannot be read for file {0}", file.Name), e);
                return false;
            }

            XmpTag tag = null;
            try {
                tag = new XmpTag (xmp);
            } catch (Exception e) {
                Hyena.Log.Exception (String.Format ("Metadata of Sidecar cannot be parsed for file {0}", file.Name), e);
                return false;
            }

            var xmp_tag = file.GetTag (TagLib.TagTypes.XMP, true) as XmpTag;
            xmp_tag.ReplaceFrom (tag);
            return true;
        }

        public static bool SaveXmpSidecar (this TagLib.Image.File file, TagLib.File.IFileAbstraction resource)
        {
            var xmp_tag = file.GetTag (TagLib.TagTypes.XMP, false) as XmpTag;
            if (xmp_tag == null) {
                // TODO: Delete File
                return true;
            }

            var xmp = xmp_tag.Render ();

            try {
                using (var stream = resource.WriteStream) {
                    stream.SetLength (0);
                    using (var writer = new StreamWriter (stream)) {
                        writer.Write (xmp);
                    }
                    resource.CloseStream (stream);
                }
            } catch (Exception e) {
                Hyena.Log.Exception (String.Format ("Sidecar cannot be saved: {0}", resource.Name), e);
                return false;
            }

            return true;
        }
    }
}
