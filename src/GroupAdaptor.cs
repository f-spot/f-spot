namespace FSpot {
	public abstract class GroupAdaptor {
		public abstract int Value (int item) ;
		public abstract int Count ();
		public abstract string TickLabel (int item);
		public abstract string GlassLabel (int item);

		public abstract void SetLimits (int min, int max);
		public abstract void SetGlass (int item);

		public delegate void GlassSetHandler (GroupAdaptor adaptor, int index);
		public virtual event GlassSetHandler GlassSet;
	}
}
