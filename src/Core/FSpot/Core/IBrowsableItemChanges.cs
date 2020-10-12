//
// IBrowsableItemChanges.cs
//
// Author:
//   Stephane Delcroix <sdelcroix@novell.com>
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008 Stephane Delcroix
// Copyright (C) 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace FSpot.Core
{
	public interface IBrowsableItemChanges
	{
		bool DataChanged {get;}
		bool MetadataChanged {get;}
	}

	public class FullInvalidate : IBrowsableItemChanges
	{
		static FullInvalidate instance = new FullInvalidate ();
		public static FullInvalidate Instance {
			get { return instance; }
		}

		public bool DataChanged {
			get { return true; }
		}
		public bool MetadataChanged {
			get { return true; }
		}
	}

	public class InvalidateData : IBrowsableItemChanges
	{
		static InvalidateData instance = new InvalidateData ();
		public static InvalidateData Instance {
			get { return instance; }
		}

		public bool DataChanged { get { return true; } }
		public bool MetadataChanged { get { return false; } }
	}
}
