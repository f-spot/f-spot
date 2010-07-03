using Hyena;

namespace FSpot
{
	/// <summary>
	///    This is the contract that needs to be implemented before the image
	///    data of the object can be loaded.
	/// </summary>
	public interface ILoadable
	{
		SafeUri Uri { get; set; }
	}
}
