
namespace FSpot.Models.Old
{
	public partial class PhotoVersion : IConvert
	{
		public long PhotoId { get; set; }
		public long VersionId { get; set; }
		public string Name { get; set; }
		public string BaseUri { get; set; }
		public string Filename { get; set; }
		public string ImportMd5 { get; set; }
		public bool Protected { get; set; }

		public object Convert ()
		{
			return new Models.PhotoVersion {
				OldPhotoId = PhotoId,
				VersionId = VersionId,
				Name = Name,
				BaseUri = BaseUri,
				Filename = Filename,
				ImportMd5 = ImportMd5,
				Protected = Protected
			};
		}
	}
}
