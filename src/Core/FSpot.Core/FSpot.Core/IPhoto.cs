//
// IPhoto.cs
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

using System.Collections.Generic;

namespace FSpot.Core
{
    public interface IPhoto
    {

        #region Metadata

        /// <summary>
        ///    The time the item was created.
        /// </summary>
        System.DateTime Time { get; }

        /// <summary>
        ///    The tags which are dedicated to this item.
        /// </summary>
        Tag[] Tags { get; }

        /// <summary>
        ///    The description of the item.
        /// </summary>
        string Description { get; }

        /// <summary>
        ///    The name of the item.
        /// </summary>
        string Name { get; }

        /// <summary>
        ///    The rating which is dedicted to this item. It should only range from 0 to 5.
        /// </summary>
        uint Rating { get; }

        #endregion


        #region Versioning

        /// <summary>
        ///    The default version of this item. Every item must have at least one version and this must not be
        ///    <see langref="null"/>
        /// </summary>
        IPhotoVersion DefaultVersion { get; }

        /// <summary>
        ///    All versions of this item. Since every item must have at least the default version, this enumeration
        ///    must not be empty.
        /// </summary>
        IEnumerable<IPhotoVersion> Versions { get; }
        
        #endregion
        
    }
}
