//
// SidecarXmpExtensions.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
// Copyright (C) 2010 Ruben Vermeersch
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;

using Hyena;

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
                Log.DebugFormat ($"Sidecar cannot be read for file {file.Name}");
                Log.DebugException (e);
                return false;
            }

            XmpTag tag = null;
            try {
                tag = new XmpTag (xmp, file);
            } catch (Exception e) {
				Log.DebugFormat ($"Metadata of Sidecar cannot be parsed for file {file.Name}");
                Log.DebugException (e);
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
				Log.DebugFormat ($"Sidecar cannot be saved: {resource.Name}");
                Log.DebugException (e);
                return false;
            }

            return true;
        }
    }
}
