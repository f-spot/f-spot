//
// Mono.Google.Picasa.PicasaAlbumCollection
//
// Author:
//	Gonzalo Paniagua Javier (gonzalo@novell.com)
//

//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Specialized;

namespace Mono.Google.Picasa {
	public sealed class PicasaAlbumCollection : NameObjectCollectionBase {
		internal PicasaAlbumCollection ()
		{
		}

		internal void Add (PicasaAlbum album)
		{
			if (BaseGet (album.UniqueID) != null)
				throw new Exception ("Duplicate album?");

			BaseAdd (album.UniqueID, album);
		}

		internal void SetReadOnly ()
		{
			IsReadOnly = true;
		}

		public PicasaAlbum GetByTitle (string title)
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

		public PicasaAlbum this [int index] {
			get { return (PicasaAlbum) BaseGet (index); }
		}

		public PicasaAlbum this [string uniqueID] {
			get { return (PicasaAlbum) BaseGet (uniqueID); }
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

		public PicasaAlbum [] AllValues {
			get { return (PicasaAlbum []) BaseGetAllValues (typeof (PicasaAlbum)); }
		}
	}
}
