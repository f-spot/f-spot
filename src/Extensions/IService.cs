/*
 * FSpot.Extensions.IService.cs
 *
 * Author(s):
 * 	Stephane Delcroix  <stephane@delcroix.org>
 *
 * This is free software. See COPYING for details.
 */

namespace FSpot.Extensions
{
	public interface IService
	{
		bool Start ();
		bool Stop ();
	}
}
