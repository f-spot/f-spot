
namespace FSpot.Database
{
	public partial class Jobs
	{
		public long Id { get; set; }
		public string JobType { get; set; }
		public string JobOptions { get; set; }
		public long? RunAt { get; set; }
		public long JobPriority { get; set; }
	}
}
