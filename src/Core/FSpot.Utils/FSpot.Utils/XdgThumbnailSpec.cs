using Gdk;
using System;
using System.Runtime.InteropServices;
using Hyena;

namespace FSpot.Utils
{
    public enum ThumbnailSize
    {
        Normal,
        Large
    }

    public static class XdgThumbnailSpec
    {
#region Public API
        public delegate Pixbuf PixbufLoader (SafeUri uri);
        public static PixbufLoader DefaultLoader { get; set; }

        public static Pixbuf LoadThumbnail (SafeUri uri, ThumbnailSize size)
        {
            return LoadThumbnail (uri, size, DefaultLoader);
        }

        public static Pixbuf LoadThumbnail (SafeUri uri, ThumbnailSize size, PixbufLoader loader)
        {
            var thumb_uri = ThumbUri (uri, size);
            var pixbuf = LoadFromUri (thumb_uri);
            if (!IsValid (uri, pixbuf)) {
                Log.DebugFormat ("Invalid thumbnail, reloading: {0}", uri);
                if (pixbuf != null)
                    pixbuf.Dispose ();

                if (loader == null)
                    return null;

                pixbuf = CreateFrom (uri, thumb_uri, size, loader);
            }
            return pixbuf;
        }

        public static void RemoveThumbnail (SafeUri uri)
        {
            var normal_uri = ThumbUri (uri, ThumbnailSize.Normal);
            var large_uri = ThumbUri (uri, ThumbnailSize.Large);

            var file = GLib.FileFactory.NewForUri (normal_uri);
            if (file.Exists)
                file.Delete (null);

            file = GLib.FileFactory.NewForUri (large_uri);
            if (file.Exists)
                file.Delete (null);
        }
#endregion

#region Private helpers
        const string ThumbMTimeOpt = "tEXt::Thumb::MTime";
        const string ThumbUriOpt = "tEXt::Thumb::URI";

        static SafeUri home_dir = new SafeUri (Environment.GetFolderPath (Environment.SpecialFolder.Personal));

        private static Pixbuf CreateFrom (SafeUri uri, SafeUri thumb_uri, ThumbnailSize size, PixbufLoader loader)
        {
            var pixels = size == ThumbnailSize.Normal ? 128 : 256;
            Pixbuf pixbuf;
            try {
                pixbuf = loader (uri);
            } catch (Exception e) {
                Log.DebugFormat ("Failed loading image for thumbnailing: {0}", uri);
                Log.DebugException (e);
                return null;
            }
            double scale_x = (double) pixbuf.Width / pixels;
            double scale_y = (double) pixbuf.Height / pixels;
            double scale = Math.Max (1.0, Math.Max (scale_x, scale_y));
            int target_x = (int) (pixbuf.Width / scale);
            int target_y = (int) (pixbuf.Height / scale);
            var thumb_pixbuf = pixbuf.ScaleSimple (target_x, target_y, InterpType.Bilinear);
            pixbuf.Dispose ();

            var file = GLib.FileFactory.NewForUri (uri);
            var info = file.QueryInfo ("time::modified", GLib.FileQueryInfoFlags.None, null);
            var mtime = info.GetAttributeULong ("time::modified").ToString ();

            thumb_pixbuf.Savev (thumb_uri.LocalPath, "png",
                                new string [] {ThumbUriOpt, ThumbMTimeOpt, null},
                                new string [] {uri, mtime});

            return thumb_pixbuf;
        }

        private static SafeUri ThumbUri (SafeUri uri, ThumbnailSize size)
        {
            var hash = CryptoUtil.Md5Encode (uri);
            return home_dir.Append (".thumbnails")
                           .Append (size == ThumbnailSize.Normal ? "normal" : "large")
                           .Append (hash + ".png");
        }

        private static Pixbuf LoadFromUri (SafeUri uri)
        {
            var file = GLib.FileFactory.NewForUri (uri);
            if (!file.Exists)
                return null;
            Pixbuf pixbuf;
            using (var stream = new GLib.GioStream (file.Read (null))) {
                try {
                    pixbuf = new Pixbuf (stream);
                } catch (Exception e) {
                    file.Delete ();
                    Log.DebugFormat ("Failed thumbnail: {0}", uri);
                    Log.DebugException (e);
                    return null;
                }
            }
            return pixbuf;
        }

        private static bool IsValid (SafeUri uri, Pixbuf pixbuf)
        {
            if (pixbuf == null) {
                return false;
            }

            if (pixbuf.GetOption (ThumbUriOpt) != uri.ToString ()) {
                return false;
            }

            var file = GLib.FileFactory.NewForUri (uri);
            if (!file.Exists)
                return false;

            var info = file.QueryInfo ("time::modified", GLib.FileQueryInfoFlags.None, null);

            if (pixbuf.GetOption (ThumbMTimeOpt) != info.GetAttributeULong ("time::modified").ToString ()) {
                return false;
            }

            return true;
        }
#endregion

    }
}
