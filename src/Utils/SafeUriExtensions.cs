using Hyena;
using System;

namespace FSpot
{
    public static class SafeUriExtensions
    {
        public static SafeUri Append (this SafeUri base_uri, string filename)
        {
            return new SafeUri (base_uri.AbsoluteUri + (base_uri.AbsoluteUri.EndsWith ("/") ? "" : "/") + filename, true);
        }

        public static SafeUri GetBaseUri (this SafeUri uri)
        {
            var abs_uri = uri.AbsoluteUri;
            return new SafeUri (abs_uri.Substring (0, abs_uri.LastIndexOf ('/')), true);
        }

        public static string GetFilename (this SafeUri uri)
        {
            var abs_uri = uri.AbsoluteUri;
            return abs_uri.Substring (abs_uri.LastIndexOf ('/') + 1);
        }

        public static string GetExtension (this SafeUri uri)
        {
            var abs_uri = uri.AbsoluteUri;
            var index = abs_uri.LastIndexOf ('.');
            return index == -1 ? String.Empty : abs_uri.Substring (index);
        }

        public static string GetFilenameWithoutExtension (this SafeUri uri)
        {
            var name = uri.GetFilename ();
            var index = name.LastIndexOf ('.');
            return index > -1 ? name.Substring (0, index) : name;
        }
    }
}
