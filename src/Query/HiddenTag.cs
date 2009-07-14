/*
 * HiddenTag.cs
 * 
 * Author(s):
 *	Mike Gemuende <mike@gemuende.de>
 *
 * This is free software. See COPYING for details.
 *
 */


using System;

using FSpot;


namespace FSpot.Query
{
	
	public class HiddenTag : IQueryCondition
	{
		private static HiddenTag show_hidden_tag;
		private static HiddenTag hide_hidden_tag;
		
		public static HiddenTag ShowHiddenTag {
			get {
				if (show_hidden_tag == null)
					show_hidden_tag = new HiddenTag (true);
				
				return show_hidden_tag;
			}
		}

		public static HiddenTag HideHiddenTag {
			get {
				if (hide_hidden_tag == null)
					hide_hidden_tag = new HiddenTag (false);
				
				return hide_hidden_tag;
			}
		}
		
		
		bool show_hidden;
		
		private HiddenTag (bool show_hidden)
		{
			this.show_hidden = show_hidden;
		}
		
		public string SqlClause ()
		{
			Tag hidden = Core.Database.Tags.Hidden;
			
			if ( ! show_hidden && hidden != null)
				return String.Format (" photos.id NOT IN (SELECT photo_id FROM photo_tags WHERE tag_id = {0}) ",
				                      hidden.Id);
			else
				return null;
		}
	}
}
