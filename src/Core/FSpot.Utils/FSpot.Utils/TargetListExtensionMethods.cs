// FIXME: Missing license file?
using Gtk;

namespace FSpot.Utils
{
    public static class TargetListExtensionMethods
    {
        public static void AddTargetEntry(this TargetList targetList, TargetEntry entry)
        {
            targetList.Add(entry.Target, (uint)entry.Flags, (uint)entry.Info);
        }
    }
}

