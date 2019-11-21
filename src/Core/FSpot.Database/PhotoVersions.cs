
namespace FSpot.Database
{
	public partial class PhotoVersions
	{
		public long? PhotoId { get; set; }
		public long? VersionId { get; set; }
		public byte[] Name { get; set; }
		public byte[] BaseUri { get; set; }
		public byte[] Filename { get; set; }
		public string ImportMd5 { get; set; }
		public byte[] Protected { get; set; }
	}
}
