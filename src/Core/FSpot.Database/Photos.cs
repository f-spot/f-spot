
namespace FSpot.Database
{
	public partial class Photos
	{
		public long Id { get; set; }
		public long Time { get; set; }
		public byte[] BaseUri { get; set; }
		public byte[] Filename { get; set; }
		public string Description { get; set; }
		public long RollId { get; set; }
		public long DefaultVersionId { get; set; }
		public long? Rating { get; set; }
	}
}
