//
// PhotoVersion.cs
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

using Hyena;
using FSpot.Core;
using FSpot.Utils;

namespace FSpot
{
    public class PhotoVersion : IPhotoVersion
    {
        public string Name { get; set; }
        public IPhoto Photo { get; private set; }
        public SafeUri BaseUri { get; set; }
        public string Filename { get; set; }

        public SafeUri Uri {
            get { return BaseUri.Append (Filename); }
            set {
                BaseUri = value.GetBaseUri ();
                Filename = value.GetFilename ();
            }
        }

        public string ImportMD5 { get; set; }
        public uint VersionId { get; private set; }
        public bool IsProtected { get; private set; }

        public PhotoVersion (IPhoto photo, uint version_id, SafeUri base_uri, string filename, string md5_sum, string name, bool is_protected)
        {
            Photo = photo;
            VersionId = version_id;
            BaseUri = base_uri;
            Filename = filename;
            ImportMD5 = md5_sum;
            Name = name;
            IsProtected = is_protected;
        }
    }
}
