
namespace FSpot.Models.Old
{
	public partial class PhotoTag : IConvert
	{
		public long PhotoId { get; set; }
		public long TagId { get; set; }

		public object Convert ()
		{
			return new Models.PhotoTag {
				OldPhotoId = PhotoId,
				OldTagId = TagId
			};
		}
	}
}
