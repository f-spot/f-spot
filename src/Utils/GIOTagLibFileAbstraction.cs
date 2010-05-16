using System;
using System.IO;
using GLib;

namespace FSpot.Utils
{
    public sealed class GIOTagLibFileAbstraction : TagLib.File.IFileAbstraction
    {
        private FileInputStream gio_stream;

        public string Name {
            get {
                return Uri.ToString ();
            }
            set {
                Uri = new Uri (value);
            }
        }

        public Uri Uri { get; set; }

        public Stream ReadStream {
            get {
                if (gio_stream == null) {
                    var file = FileFactory.NewForUri(Uri);
                    gio_stream = file.Read (null);
                }
                return new GioStream (gio_stream);
            }
        }

        public Stream WriteStream {
            get { throw new NotImplementedException (); }
        }

        public void CloseStream (Stream stream)
        {
            stream.Close ();
            gio_stream.Dispose ();
            gio_stream = null;
        }
    }
}
