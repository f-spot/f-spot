using FSpot.Utils;

namespace FSpot {
	public interface ILimitable {
		void SetLimits (int min, int max);
	}
	
	public abstract class GroupAdaptor {
		protected PhotoQuery query;
		public PhotoQuery Query {
			get {
				return query;
			}
		}

		protected bool order_ascending = false;
		public bool OrderAscending {
			get {
				return order_ascending;
			}
			set {
				if (order_ascending != value) {
					order_ascending = value;
					Reload();
				}
			}
		}
		
		public abstract int Value (int item) ;
		public abstract int Count ();
		public abstract string TickLabel (int item);
		public abstract string GlassLabel (int item);

		protected abstract void Reload ();

		public abstract void SetGlass (int item);
		public abstract int IndexFromPhoto (FSpot.IBrowsableItem photo);
		public abstract FSpot.IBrowsableItem PhotoFromIndex (int item);

		public delegate void GlassSetHandler (GroupAdaptor adaptor, int index);
		public virtual event GlassSetHandler GlassSet;

		public delegate void ChangedHandler (GroupAdaptor adaptor);
		public virtual event ChangedHandler Changed;

		protected void HandleQueryChanged (IBrowsableCollection sender)
		{
			Reload ();
		}

		public void Dispose ()
		{
			this.query.Changed -= HandleQueryChanged;
		}

		protected GroupAdaptor (PhotoQuery query, bool order_ascending)
		{
			this.order_ascending = order_ascending;
			this.query = query;
			this.query.Changed += HandleQueryChanged;

			Reload (); 
		}
	}
}
