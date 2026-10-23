// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Specialized;

using FSpot.Core;
using FSpot.Query;

namespace FSpot.Avalonia.ViewModels;

// The grid binds this list. A PhotoQuery counts in SQL and loads a page when
// the virtualizing panel asks for a row. Tests append items when no query is attached.
public sealed class LibraryItems : IList, IReadOnlyList<PhotoItemViewModel>, INotifyCollectionChanged
{
	readonly List<PhotoItemViewModel> manual = new ();
	readonly Dictionary<int, PhotoItemViewModel> realized = new ();
	PhotoQuery? query;

	public event NotifyCollectionChangedEventHandler? CollectionChanged;

	// The grid asks for a row. The view model says whether that index is selected
	// without this list walking the catalog to find out.
	public Func<int, bool>? IsIndexSelected { get; set; }

	// A row the person selected, or the current photo, stays built when the window moves.
	public Func<PhotoItemViewModel, bool>? KeepRealized { get; set; }

	// The grid dropped this row. The view model disposes the bitmap the cell was showing.
	public Action<PhotoItemViewModel>? Discarded { get; set; }

	public bool IsPaged => query != null;

	public IEnumerable<PhotoItemViewModel> Realized
		=> query == null ? manual : realized.Values;

	// Indexes the grid has already built. A paged library does not walk the rows it has not shown.
	public List<(int Index, PhotoItemViewModel Item)> Indexed ()
	{
		if (query == null) {
			var items = new List<(int, PhotoItemViewModel)> (manual.Count);
			for (int i = 0; i < manual.Count; i++)
				items.Add ((i, manual[i]));
			return items;
		}

		var paged = new List<(int, PhotoItemViewModel)> (realized.Count);
		foreach (var pair in realized)
			paged.Add ((pair.Key, pair.Value));
		return paged;
	}

	public void Show (PhotoQuery source)
	{
		query = source ?? throw new ArgumentNullException (nameof (source));
		manual.Clear ();
		DropRealized ();
		RaiseReset ();
	}

	public void Show (IReadOnlyList<PhotoItemViewModel> items)
	{
		query = null;
		DropRealized ();
		manual.Clear ();
		manual.AddRange (items);
		RaiseReset ();
	}

	public void Add (PhotoItemViewModel item)
	{
		if (query != null)
			throw new InvalidOperationException ("A paged library is filled from the catalog.");
		manual.Add (item);
		CollectionChanged?.Invoke (this, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Add, item, manual.Count - 1));
	}

	public int Count => query?.Count ?? manual.Count;

	public PhotoItemViewModel this[int index] {
		get {
			if (query == null)
				return manual[index];
			if (!realized.TryGetValue (index, out PhotoItemViewModel? item)) {
				item = new PhotoItemViewModel ((Photo)query[index]);
				if (IsIndexSelected != null)
					item.Selected = IsIndexSelected (index);
				realized[index] = item;
			}
			return item;
		}
	}

	// The panel shows a slice wider than one page. Trimming on the last index of that
	// slice would drop the first cells while their containers still hold them.
	public void TrimOutside (int first, int last)
	{
		if (query == null)
			return;
		DropOutside (first - PhotoQuery.PageSize, last + PhotoQuery.PageSize);
	}

	public void TrimAround (int index)
	{
		if (query == null)
			return;
		int page = index - index % PhotoQuery.PageSize;
		DropOutside (page - PhotoQuery.PageSize, page + PhotoQuery.PageSize);
	}

	void DropOutside (int low, int high)
	{
		var drop = new List<int> ();
		foreach (int key in realized.Keys) {
			if (key >= low && key <= high)
				continue;
			if (KeepRealized != null && KeepRealized (realized[key]))
				continue;
			drop.Add (key);
		}
		foreach (int key in drop) {
			PhotoItemViewModel item = realized[key];
			realized.Remove (key);
			Discarded?.Invoke (item);
		}
	}

	void DropRealized ()
	{
		if (realized.Count == 0)
			return;
		var gone = new List<PhotoItemViewModel> (realized.Values);
		realized.Clear ();
		if (Discarded is null)
			return;
		foreach (PhotoItemViewModel item in gone)
			Discarded (item);
	}

	public int IndexOf (PhotoItemViewModel item)
	{
		if (item == null)
			return -1;
		if (query == null)
			return manual.IndexOf (item);
		if (item.Photo is Photo photo)
			return query.IndexOf (photo);
		return -1;
	}

	public bool Contains (PhotoItemViewModel item)
		=> IndexOf (item) >= 0;

	public IEnumerator<PhotoItemViewModel> GetEnumerator ()
	{
		for (int i = 0; i < Count; i++)
			yield return this[i];
	}

	public void Clear ()
	{
		query = null;
		manual.Clear ();
		DropRealized ();
		RaiseReset ();
	}

	void RaiseReset ()
		=> CollectionChanged?.Invoke (this, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset));

	int IList.Add (object? value)
	{
		Add ((PhotoItemViewModel)value!);
		return Count - 1;
	}

	bool IList.Contains (object? value) => value is PhotoItemViewModel item && Contains (item);
	int IList.IndexOf (object? value) => value is PhotoItemViewModel item ? IndexOf (item) : -1;
	void IList.Insert (int index, object? value) => throw new NotSupportedException ();
	void IList.Remove (object? value) => throw new NotSupportedException ();
	void IList.RemoveAt (int index) => throw new NotSupportedException ();
	void IList.Clear () => Clear ();
	bool IList.IsFixedSize => false;
	bool IList.IsReadOnly => false;
	object? IList.this[int index] {
		get => this[index];
		set => throw new NotSupportedException ();
	}
	void ICollection.CopyTo (Array array, int index) => throw new NotSupportedException ();
	bool ICollection.IsSynchronized => false;
	object ICollection.SyncRoot => this;
	IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();
}
