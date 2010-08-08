using Hyena;

namespace FSpot
{
	public class HashUtils {
		public static string GenerateMD5 (SafeUri uri)
		{
			var file = GLib.FileFactory.NewForUri (uri);
			var stream = new GLib.GioStream (file.Read (null));
			var hash = CryptoUtil.Md5EncodeStream (stream);
			stream.Close ();
			return hash;
		}
	}
}
