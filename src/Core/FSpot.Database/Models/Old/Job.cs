namespace FSpot.Models.Old
{
	public partial class Job : IConvert
	{
		public long Id { get; set; }
		public string JobType { get; set; }
		public string JobOptions { get; set; }
		public long? RunAt { get; set; }
		public long JobPriority { get; set; }

		public object Convert ()
		{
			return new Models.Job {
				OldId = Id,
				JobType = JobType,
				JobOptions = JobOptions,
				RunAt = RunAt,
				JobPriority = JobPriority
			};
		}
	}
}
