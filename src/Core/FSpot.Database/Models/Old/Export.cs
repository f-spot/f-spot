
namespace FSpot.Models.Old
{
	public class Export : IConvert
	{
		public long Id { get; set; }
		public long ImageId { get; set; }
		public long ImageVersionId { get; set; }
		public string ExportType { get; set; }
		public string ExportToken { get; set; }

		public object Convert ()
		{
			return new Models.Export {
				OldId = Id,
				ImageId = ImageId,
				ImageVersionId = ImageVersionId,
				ExportType = ExportType,
				ExportToken = ExportToken
			};
		}
	}
}
