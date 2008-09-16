#if ENABLE_BEAGLE
using Beagle;

namespace FSpot {
	public static class BeagleNotifier {
		public static void SendUpdate (IBrowsableItem item)
		{
			Indexable indexable = new Indexable (item.DefaultVersionUri);
			indexable.Type = IndexableType.PropertyChange;
			Beagle.Property prop;

			// Clear the existing tags
			prop = Beagle.Property.NewKeyword ("fspot:Tag", "");
			prop.IsMutable = true;
			prop.IsPersistent = true;
			indexable.AddProperty (prop);
			prop = Beagle.Property.NewKeyword ("image:Tag", "");
			prop.IsMutable = true;
			prop.IsPersistent = true;
			indexable.AddProperty (prop);

			foreach (Tag t in item.Tags) {
				prop = Beagle.Property.NewKeyword ("fspot:Tag", t.Name);
				prop.IsMutable = true;
				prop.IsPersistent = true;
				indexable.AddProperty (prop);
				prop = Beagle.Property.NewKeyword ("image:Tag", t.Name);
				prop.IsMutable = true;
				prop.IsPersistent = true;
				indexable.AddProperty (prop);
			}

			prop = Beagle.Property.New ("fspot:Description", item.Description);
			prop.IsMutable = true;
			prop.IsPersistent = true;
			indexable.AddProperty (prop);

			// Create a message to send to the daemon with this information.
			// The source tells it what index the existing "/home/joe/test.txt" document lives.
			IndexingServiceRequest req = new IndexingServiceRequest ();
			req.Keepalive = false;
			req.Source = "Files";
			req.Add (indexable);

			req.SendAsync ();
		}
	}
}
#endif
