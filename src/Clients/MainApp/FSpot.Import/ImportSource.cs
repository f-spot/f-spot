using Hyena;
using System;

using FSpot.Core;


namespace FSpot.Import
{
    public interface ImportSource {
        string Name { get; }
        string IconName { get; }

        void StartPhotoScan (ImportController controller, PhotoList photo_list);
        void Deactivate ();
    }
}
