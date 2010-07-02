using Hyena;
using TagLib;
using System;

namespace FSpot.Utils
{
    public static class Metadata
    {
        public static TagLib.Image.File Parse (SafeUri uri)
		{
			var res = new GIOTagLibFileAbstraction () { Uri = uri };
            var sidecar_uri = uri.ReplaceExtension (".xmp");
            var sidecar_res = new GIOTagLibFileAbstraction () { Uri = sidecar_uri };
            var file = File.Create (res) as TagLib.Image.File;

            var sidecar_file = GLib.FileFactory.NewForUri (sidecar_uri);
			if (sidecar_file.Exists) {
				file.ParseXmpSidecar (sidecar_res);
			}

			return file;
		}
	}
}
