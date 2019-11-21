
namespace FSpot.Database
{
	public partial class Tags
	{
		public long Id { get; set; }
		public string Name { get; set; }
		public long? CategoryId { get; set; }
		public byte[] IsCategory { get; set; }
		public long? SortPriority { get; set; }
		public string Icon { get; set; }
	}
}
