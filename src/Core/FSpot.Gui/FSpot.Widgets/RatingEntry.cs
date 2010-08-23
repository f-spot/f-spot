/*
 * RatingEntry.cs
 *
 * Author(s)
 *  Mike Gemuende <mike@gemuende.de>
 *
 * This is free software. See COPYING for details.
 */

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
    }
}

