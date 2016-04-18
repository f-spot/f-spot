//
// ScreensaverConfig.cs
//
// Author:
//   Stephane Delcroix <stephane@delcroix.org>
//
// Copyright (C) 2009 Novell, Inc.
// Copyright (C) 2009 Stephane Delcroix
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Reflection;

using Gtk;

using FSpot;
using FSpot.Core;
using FSpot.UI.Dialog;
using FSpot.Extensions;
using FSpot.Settings;
using FSpot.Widgets;

namespace FSpot.Tools.ScreensaverConfig
{
	public class ScreensaverConfig : ICommand
	{
		const string SaverCommand = "screensavers-f-spot-screensaver";
		const string SaverMode = "single";
		const string GNOME_SCREENSAVER_THEME = "/apps/gnome-screensaver/themes";
		const string GNOME_SCREENSAVER_MODE = "/apps/gnome-screensaver/mode";
		const string SCREENSAVER_TAG = Preferences.SCREENSAVER_TAG;
		const string SCREENSAVER_DELAY = Preferences.SCREENSAVER_DELAY;
		const double default_delay = 2.0;

		public void Run (object o, EventArgs e)
		{
			var d = new ScreensaverConfigDialog ();
			d.Run ();
			d.Destroy ();
		}

		class ScreensaverConfigDialog : BuilderDialog
		{
			[GtkBeans.Builder.Object] Range delay_scale;
			[GtkBeans.Builder.Object] Table table;
			[GtkBeans.Builder.Object] RadioButton all_radio;
			[GtkBeans.Builder.Object] RadioButton tagged_radio;
			[GtkBeans.Builder.Object] Button do_button;
			Adjustment delay_adjustment;
			MenuButton tag_button;

			public ScreensaverConfigDialog () : base (Assembly.GetExecutingAssembly (), "ScreensaverConfigDialog.ui", "screensaver-config-dialog")
			{
				delay_adjustment = new Adjustment (default_delay, 1.0, 20.0, .5, 5, 0);
				LoadPreference (SCREENSAVER_DELAY);
				delay_scale.Adjustment = delay_adjustment;
				delay_adjustment.ValueChanged += HandleDelayChanged;
				
				tag_button = new MenuButton ();
				tag_button.SizeRequested += delegate (object sender, SizeRequestedArgs args) {
					var req = args.Requisition;
					req.Width += 100;
					args.Requisition = req;
				};
				TagMenu menu = new TagMenu (null, App.Instance.Database.Tags);
				menu.Populate (false);
				menu.TagSelected += HandleTagSelected;
				tag_button.Menu = menu;
				tag_button.ShowAll ();
				table.Attach (tag_button, 2, 3, 1, 2);
				LoadPreference (SCREENSAVER_TAG);
				all_radio.Toggled += HandleTagRadioToggled;

				do_button.Clicked += HandleUseFSpot;
			}

			void LoadPreference (string key)
			{
				switch (key) {
				case SCREENSAVER_DELAY:
					var delay = Preferences.Get<double> (key);
					if (delay == 0.0)
						delay = default_delay;
					if (delay < delay_adjustment.Lower)
						delay = delay_adjustment.Lower;
					if (delay > delay_adjustment.Upper)
						delay = delay_adjustment.Upper;
					delay_adjustment.Value = delay;
					break;
				case SCREENSAVER_TAG:
					var screensaver_tag = Preferences.Get<int> (key);
					Tag t = App.Instance.Database.Tags.GetTagById (screensaver_tag);
					if (screensaver_tag == 0 || t == null) {
						all_radio.Active = true;
						tag_button.Sensitive = false;
					} else {
						tagged_radio.Active = true;
						tag_button.Label = t.Name;
					}
					break;
				}
			}

			void HandleDelayChanged (object sender, EventArgs e)
			{
				Preferences.Set (SCREENSAVER_DELAY, delay_adjustment.Value);
			}

			void HandleTagRadioToggled (object sender, EventArgs e)
			{
				tag_button.Sensitive = tagged_radio.Active;
				if (all_radio.Active)
					Preferences.Set (SCREENSAVER_TAG, 0);
				else
					HandleTagSelected (((tag_button.Menu as Menu).Active as TagMenu.TagMenuItem).Value);
			}

			void HandleTagSelected (Tag t)
			{
				tag_button.Label = t.Name;
				Preferences.Set (SCREENSAVER_TAG, (int) t.Id);
			}

			void HandleUseFSpot (object sender, EventArgs e)
			{
				Preferences.Set (GNOME_SCREENSAVER_MODE, SaverMode);
				Preferences.Set (GNOME_SCREENSAVER_THEME, new string [] { SaverCommand });
			}
		}
	}
}
