/* 
 *  Authors: 
 *    Larry Ewing <lewing@novell.com>
 *    Stephane Delcroix <stephane@delcroix.org>
 * 
 *  I don't like per file copyright notices.
 */
using System.Collections;

namespace FSpot.Filters {
	public class FilterSet : IFilter {
		public ArrayList list;
	
		public FilterSet () {
			list = new ArrayList ();
		}
		
		public void Add (IFilter filter)
		{
			list.Add (filter);
		}
		
		public bool Convert (string source, string dest)
		{
			bool changed = false;
			foreach (IFilter filter in list) {
				changed |= filter.Convert (changed ? dest : source, dest);
			}
			return changed;
		}
	}
}
