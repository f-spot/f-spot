using Hyena;

namespace FSpot
{
    public interface IBrowsableItemVersion {
        string Name { get; }
        bool IsProtected { get; }
        SafeUri BaseUri { get; }
        string Filename { get; }

        // For convenience
        SafeUri Uri { get; set; }

		string ImportMD5 { get; }
    }
}
