namespace FSpot {
	public interface IBrowsableCollection {
		IBrowsableItem [] Items {
			get;
		}
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
