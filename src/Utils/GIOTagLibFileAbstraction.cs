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
        private GioStream stream;
        private SafeUri tmp_write_uri;

        private const string TMP_INFIX = ".tmpwrite";

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

        private void CopyToTmp ()
        {
            var file = FileFactory.NewForUri (Uri);
            tmp_write_uri = CreateTmpFile ();
            var tmp_file = FileFactory.NewForUri (tmp_write_uri);

            file.Copy (tmp_file, GLib.FileCopyFlags.AllMetadata | GLib.FileCopyFlags.Overwrite, null, null);
        }

        private void CommitTmp ()
        {
            if (tmp_write_uri == null)
                return;

            var file = FileFactory.NewForUri (Uri);
            var tmp_file = FileFactory.NewForUri (tmp_write_uri);

            tmp_file.Move (file, GLib.FileCopyFlags.AllMetadata | GLib.FileCopyFlags.Overwrite, null, null);
        }

        private SafeUri CreateTmpFile ()
        {
            var uri = Uri.GetBaseUri ().Append ("." + Uri.GetFilenameWithoutExtension ());
            var tmp_uri = uri.ToString () + TMP_INFIX + Uri.GetExtension ();
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
