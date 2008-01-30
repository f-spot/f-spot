/*
 * FSpot.DbItem.cs
 *
 * Author(s):
 *	Larry Ewing
 *
 * This is fre software. See COPYING for details.
 */

public class DbItem {
	uint id;
	public uint Id {
		get { return id; }
	}

	protected DbItem (uint id) {
		this.id = id;
	}
}
