using System;

namespace FSpot
{
    public interface IBrowsableItemVersion {
        string Name { get; }
        bool IsProtected { get; }
        Uri Uri { get; }
    }
}
