namespace FSpot {
	public abstract class GroupAdaptor {
		public abstract int Value (int item) ;
		public abstract int Count ();
		public abstract string Label (int item);
		
		public abstract void SetLimits (int min, int max);
		public abstract void SetGlass (int item);
	}
}
