//
// SafeUriExtensions.cs
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

using Hyena;

namespace FSpot.Utils
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
            return index == -1 ? string.Empty : abs_uri.Substring (index);
        }

        public static string GetFilenameWithoutExtension (this SafeUri uri)
        {
            var name = uri.GetFilename ();
            var index = name.LastIndexOf ('.');
            return index > -1 ? name.Substring (0, index) : name;
        }

        public static SafeUri ReplaceExtension (this SafeUri uri, string extension)
        {
            return uri.GetBaseUri ().Append (uri.GetFilenameWithoutExtension () + extension);
        }
    }
}
