using SemWeb;
namespace FSpot {
	public class MetadataStore : MemoryStore
	{
		public static NamespaceManager Namespaces;
		
		static MetadataStore ()
		{
			Namespaces = new NamespaceManager ();
			
		}
		
		public void Dump ()
		{
			foreach (SemWeb.Statement stmt in this) {
				System.Console.WriteLine(stmt);
			}
		}
	       
	}	       
}
