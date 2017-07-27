//
// GIOTagLibFileAbstraction.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2010 Novell, Inc.
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

using GLib;

using Hyena;

namespace FSpot.Utils
{
    /// <summary>
    ///   Wraps GIO into a TagLib IFileAbstraction.
    /// </summary>
    /// <remarks>
    ///   Implements a safe writing pattern by first copying the file to a
    ///   temporary location. This temporary file is used for writing. When the
    ///   stream is closed, the temporary file is moved to the original
    ///   location.
    /// </remarks>
    public sealed class GIOTagLibFileAbstraction : TagLib.File.IFileAbstraction
    {
        GioStream stream;
        SafeUri tmp_write_uri;

        const string TMP_INFIX = ".tmpwrite";

        public string Name {
			get {
				return Uri.ToString ();
			}
            set {
                Uri = new SafeUri (value);
            }
        }

        public SafeUri Uri { get; set; }

        public Stream ReadStream {
            get {
                if (stream == null) {
                    var file = FileFactory.NewForUri (Uri);
                    stream = new GioStream (file.Read (null));
                }
                if (!stream.CanRead)
                    throw new Exception ("Can't read from this resource");
                return stream;
            }
        }

        public Stream WriteStream {
            get {
                if (stream == null) {
                    var file = FileFactory.NewForUri (Uri);
                    if (!file.Exists) {
                        stream = new GioStream (file.Create (GLib.FileCreateFlags.None, null));
                    } else {
                        CopyToTmp ();
                        file = FileFactory.NewForUri (tmp_write_uri);
                        stream = new GioStream (file.OpenReadwrite (null));
                    }
                }
                if (!stream.CanWrite) {
                    throw new Exception ("Stream still open in reading mode!");
                }
                return stream;
            }
        }

        void CopyToTmp ()
        {
            var file = FileFactory.NewForUri (Uri);
            tmp_write_uri = CreateTmpFile ();
            var tmp_file = FileFactory.NewForUri (tmp_write_uri);

            file.Copy (tmp_file, GLib.FileCopyFlags.AllMetadata | GLib.FileCopyFlags.Overwrite, null, null);
        }

        void CommitTmp ()
        {
            if (tmp_write_uri == null)
                return;

            var file = FileFactory.NewForUri (Uri);
            var tmp_file = FileFactory.NewForUri (tmp_write_uri);

            tmp_file.Move (file, GLib.FileCopyFlags.AllMetadata | GLib.FileCopyFlags.Overwrite, null, null);
        }

        SafeUri CreateTmpFile ()
        {
            var uri = Uri.GetBaseUri ().Append ("." + Uri.GetFilenameWithoutExtension ());
            var tmp_uri = uri + TMP_INFIX + Uri.GetExtension ();
            return new SafeUri (tmp_uri, true);
        }

        public void CloseStream (Stream stream)
        {
            stream.Close ();
            if (stream == this.stream) {
                if (stream.CanWrite) {
                    CommitTmp ();
                }
                this.stream = null;
            }
        }
    }
}
