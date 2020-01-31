using System.ComponentModel.DataAnnotations.Schema;

namespace FSpot.Models
{
	public partial class Export : BaseModel
	{
		[NotMapped]
		public long OldId { get; set; }
		public long ImageId { get; set; }
		public long ImageVersionId { get; set; }
		public string ExportType { get; set; }
		public string ExportToken { get; set; }
	}
}
