//
// Mono.Google.Picasa.PicasaPictureCollection
//
// Author:
//	Gonzalo Paniagua Javier (gonzalo@novell.com)
//

//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
