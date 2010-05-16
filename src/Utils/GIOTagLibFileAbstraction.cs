using System;
using System.IO;
using GLib;

namespace FSpot.Utils
{
    public sealed class GIOTagLibFileAbstraction : TagLib.File.IFileAbstraction
    {
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
            get { return new GioStream (FileFactory.NewForUri(Uri).Read (null)); }
        }

        public Stream WriteStream {
            get { throw new NotImplementedException (); }
        }

        public void CloseStream (Stream stream)
        {
            stream.Close ();
        }
    }
}
