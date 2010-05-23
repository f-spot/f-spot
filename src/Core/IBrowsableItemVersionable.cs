using System.Collections.Generic;

namespace FSpot
{
	public interface IBrowsableItemVersionable {
		IEnumerable<IBrowsableItemVersion> Versions { get; }
	}
}
