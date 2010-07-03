using Hyena;

namespace FSpot
{
    public interface IBrowsableItemVersion : ILoadable {
        string Name { get; }
        bool IsProtected { get; }
        SafeUri BaseUri { get; }
        string Filename { get; }

		string ImportMD5 { get; }
    }
}
