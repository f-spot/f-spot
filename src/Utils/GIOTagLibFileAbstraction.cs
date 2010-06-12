using System;
using System.IO;
using GLib;
using Hyena;

namespace FSpot.Utils
{
    public sealed class GIOTagLibFileAbstraction : TagLib.File.IFileAbstraction
    {
        private GioStream stream;

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
                return stream;
            }
        }

        public Stream WriteStream {
            get {
                if (stream == null) {
                    var file = FileFactory.NewForUri (Uri);
                    stream = new GioStream (file.ReplaceReadwrite (null, true, FileCreateFlags.None, null));
                }
                if (!stream.CanWrite) {
                    throw new Exception ("Stream still open in reading mode!");
                }
                return stream;
            }
        }

        public void CloseStream (Stream stream)
        {
            stream.Close ();
            if (stream == this.stream)
                this.stream = null;
        }
    }
}
