//
// Mono.Google.Picasa.PicasaPictureCollection
//
// Author:
//	Gonzalo Paniagua Javier (gonzalo@novell.com)
//

//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Specialized;

namespace Mono.Google.Picasa {
	public sealed class PicasaPictureCollection : NameObjectCollectionBase {
		internal PicasaPictureCollection ()
		{
		}

		internal void Add (PicasaPicture picture)
		{
			if (BaseGet (picture.UniqueID) != null)
				throw new Exception ("Duplicate picture?");

			BaseAdd (picture.UniqueID, picture);
		}

		internal void SetReadOnly ()
		{
			IsReadOnly = true;
		}

		public PicasaPicture GetByTitle (string title)
		{
			if (title == null)
				throw new ArgumentNullException ("id");

			int count = Count;
			for (int i = 0; i < count; i++) {
				if (this [i].Title == title)
					return this [i];
			}

			return null;
		}

		public PicasaPicture this [int index] {
			get { return (PicasaPicture) BaseGet (index); }
		}

		public PicasaPicture this [string uniqueID] {
			get { return (PicasaPicture) BaseGet (uniqueID); }
		}

		public string [] AllKeys {
			get {
				NameObjectCollectionBase.KeysCollection keys = Keys;
				int count = keys.Count;
				string [] result = new string [count];
				for (int i = 0; i < count; i++)
					result [i] = keys [i];

				return result;
			}
		}

		public PicasaPicture [] AllValues {
			get { return (PicasaPicture []) BaseGetAllValues (typeof (PicasaPicture)); }
		}

	}
}
