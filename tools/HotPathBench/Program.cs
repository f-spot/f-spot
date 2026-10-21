using System.Diagnostics;
using System.Reflection;
using FSpot.Database;
using Hyena;

const int Photos = 256;
const int Repeats = 31;
string root = Path.Combine(Path.GetTempPath(), "fspot-bench-" + Guid.NewGuid().ToString("n"));
Directory.CreateDirectory(root);
string dbPath = Path.Combine(root, "photos.db");
var db = new Db(null, null, null);
db.Init(dbPath, true);
for (int i = 0; i < Photos; i++) {
    db.Database.Execute(
        "INSERT INTO photos (time, base_uri, filename, description, roll_id, default_version_id, rating) VALUES (?, ?, ?, ?, ?, ?, ?)",
        1_700_000_000L + i,
        "file:///library/",
        "p" + i + ".jpg",
        "",
        1,
        1,
        0);
    db.Database.Execute(
        "INSERT INTO photo_versions (photo_id, version_id, name, base_uri, filename, import_md5, protected) VALUES (?, ?, ?, ?, ?, ?, ?)",
        i + 1,
        1,
        "Original",
        "file:///library/",
        "p" + i + ".jpg",
        "",
        0);
}

string raf = Path.Combine(root, "sample.raf");
var bytes = new byte[4096];
bytes[0x57] = 200;
bytes[0x5B] = 64;
File.WriteAllBytes(raf, bytes);
var rafType = typeof(Db).Assembly.GetType("FSpot.Imaging.RafImageFile")!;
object rafFile = Activator.CreateInstance(
    rafType,
    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
    binder: null,
    args: [SafeUri.FromPath(raf)],
    culture: null)!;
var openImage = rafType.GetMethod("OpenImageStream")!;

var querySamples = Measure(() => {
    var found = db.Photos.Query("SELECT * FROM photos");
    if (found.Length != Photos)
        throw new InvalidOperationException("query returned " + found.Length);
});
var headerSamples = Measure(() => {
    using var image = (Stream)openImage.Invoke(rafFile, null)!;
    if (image.Length != 64)
        throw new InvalidOperationException("jpeg length " + image.Length);
});

Console.WriteLine("operation=PhotoStore.Query");
Console.WriteLine("fixture_photos=" + Photos);
Console.WriteLine("fixture_bytes=" + new FileInfo(dbPath).Length);
Console.WriteLine("allocated_bytes=" + Median(querySamples.alloc));
Console.WriteLine("median_elapsed_us=" + (Median(querySamples.ticks) / 10.0).ToString(System.Globalization.CultureInfo.InvariantCulture));
Console.WriteLine("operation=RafImageFile.OpenImageStream");
Console.WriteLine("fixture_bytes=" + bytes.Length);
Console.WriteLine("allocated_bytes=" + Median(headerSamples.alloc));
Console.WriteLine("median_elapsed_us=" + (Median(headerSamples.ticks) / 10.0).ToString(System.Globalization.CultureInfo.InvariantCulture));
Console.WriteLine("no hot-path edit was adopted");

db.Dispose();
Directory.Delete(root, true);

static (long[] alloc, long[] ticks) Measure(Action action)
{
    for (int i = 0; i < 5; i++)
        action();
    var alloc = new long[Repeats];
    var ticks = new long[Repeats];
    for (int i = 0; i < Repeats; i++) {
        long before = GC.GetAllocatedBytesForCurrentThread();
        var watch = Stopwatch.StartNew();
        action();
        watch.Stop();
        alloc[i] = GC.GetAllocatedBytesForCurrentThread() - before;
        ticks[i] = watch.ElapsedTicks;
    }
    return (alloc, ticks);
}

static long Median(long[] values)
{
    var copy = (long[])values.Clone();
    Array.Sort(copy);
    return copy[copy.Length / 2];
}
