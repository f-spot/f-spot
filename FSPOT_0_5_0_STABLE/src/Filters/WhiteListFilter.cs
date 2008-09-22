/*
 * Filters/WhiteListFilter
 *
 * Author(s)
 *   Stephane Delcroix <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 *
 */

namespace FSpot.Filters {
	public class WhiteListFilter : IFilter 
	{	
		System.Collections.ArrayList valid_extensions;

		public WhiteListFilter (string [] valid_extensions)
		{
			this.valid_extensions = new System.Collections.ArrayList ();
			foreach (string extension in valid_extensions)
				this.valid_extensions.Add (extension.ToLower ());
		}

		public bool Convert (FilterRequest req)
		{
			if ( valid_extensions.Contains (System.IO.Path.GetExtension(req.Current.LocalPath).ToLower ()) )
				return false;

			if ( !valid_extensions.Contains (".jpg") && !valid_extensions.Contains (".jpeg"))
				throw new System.NotImplementedException ("can only save jpeg :(");

			return (new JpegFilter ()).Convert (req);
		}
	}
}
