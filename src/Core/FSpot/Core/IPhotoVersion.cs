//
// IPhotoVersion.cs
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

namespace FSpot.Core
{
    public interface IPhotoVersion : ILoadable
    {
        /// <summary>
        ///   The name of the version. e.g. "Convert to Black and White"
        /// </summary>
		/// <remarks>
		///   This is not the name of the file.
		/// </remarks>
		string Name { get; }

		// TODO: add Comment
		bool Protected { get; }

		// TODO: add more metadata

		// TODO: BaseUri and Filename are just in the database scheme. Does it make sense to provide them
		//       to the outside?

		/// <summary>
		///   The base uri of the directory of this version. That is the whole uri without the
		///   filename.
		/// </summary>
		string BaseUri { get; }

		/// <summary>
		///    The filename of this version.
		/// </summary>
		string Filename { get; }

		// TODO: add Comment
		// TODO: not every item is also imported. So does it make sense to have that checksum here?
		//       (If a comment is added, include the reasons for having this here!)
		string ImportMd5 { get; }
	}
}
