namespace FSpot {
	public delegate void IBrowsableCollectionChangedHandler (IBrowsableCollection collection);
	public delegate void IBrowsableCollectionItemChangedHandler (IBrowsableCollection collection, int item);

	public interface IBrowsableCollection {
		IBrowsableItem [] Items {
			get;
		}
		
		event IBrowsableCollectionChangedHandler Changed;
		event IBrowsableCollectionItemChangedHandler ItemChanged;
	}

	public interface IBrowsableItem {
		System.DateTime Time {
			get;
		}
		
		Tag [] Tags {
			get;
		}

		System.Uri DefaultVersionUri {
			get;
		}

		string Description {
			get;
		}

		string Name {
			get; 
		}
	}	
}	
