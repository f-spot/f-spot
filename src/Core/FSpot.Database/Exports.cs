
namespace FSpot.Database
{
	public partial class Exports
	{
		public long Id { get; set; }
		public long ImageId { get; set; }
		public long ImageVersionId { get; set; }
		public string ExportType { get; set; }
		public string ExportToken { get; set; }
	}
}
