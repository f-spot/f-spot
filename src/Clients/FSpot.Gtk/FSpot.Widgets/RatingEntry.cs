//
// RatingEntry.cs
//
// Author:
//   Mike Gemünde <mike@gemuende.de>
//
// Copyright (C) 2010 Novell, Inc.
// Copyright (C) 2010 Mike Gemünde
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

namespace FSpot.Widgets
{
	public class RatingEntry : Hyena.Widgets.RatingEntry
	{
		public RatingEntry (int rating) : base (rating, new RatingRenderer ())
		{
			MaxRating = 5;
			MinRating = 0;
		}

		public RatingEntry () : this (0)
		{
		}

		public RatingEntry (IntPtr raw) : base (raw)
		{
		}
	}
}
