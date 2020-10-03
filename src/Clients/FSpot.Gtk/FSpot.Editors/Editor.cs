//
// Editor.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2008-2010 Novell, Inc.
// Copyright (C) 2008, 2010 Ruben Vermeersch
//
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;

using Hyena;

using FSpot.Core;
using FSpot.Widgets;
using FSpot.Imaging;

using Gdk;
using Gtk;
using System.Linq;

namespace FSpot.Editors
{
	// This is the base class from which all editors inherit.
	public abstract class Editor
	{
		public delegate void ProcessingStartedHandler (string name, int count);
		public delegate void ProcessingStepHandler (int done);
		public delegate void ProcessingFinishedHandler ();

		public event ProcessingStartedHandler ProcessingStarted;
		public event ProcessingStepHandler ProcessingStep;
		public event ProcessingFinishedHandler ProcessingFinished;

		// Contains the current selection, the items being edited, ...
		EditorState state;
		public EditorState State {
			get {
				if (!StateInitialized)
					throw new ApplicationException ("Editor has not been initialized yet!");

				return state;
			}
			private set { state = value; }
		}

		public bool StateInitialized {
			get { return state != null; }
		}


		// Whether the user needs to select a part of the image before it can be applied.
		public bool NeedsSelection { get; protected set; }

		// A tool can be applied if it doesn't need a selection, or if it has one.
		public bool CanBeApplied {
			get {
				Log.Debug ($"{this} can be applied? {!NeedsSelection || (NeedsSelection && State.HasSelection)}");
				return !NeedsSelection || (NeedsSelection && State.HasSelection);
			}
		}

		public bool CanHandleMultiple { get; protected set; }

		protected void LoadPhoto (Photo photo, out Pixbuf photoPixbuf, out Cms.Profile photoProfile)
		{
			// FIXME: We might get this value from the PhotoImageView.
			using var img = App.Instance.Container.Resolve<IImageFileFactory> ().Create (photo.DefaultVersion.Uri);
			photoPixbuf = img.Load ();
			photoProfile = img.GetProfile ();
		}

		// The human readable name for this action.
		public readonly string Label;

		// The label on the apply button (usually shorter than the label).
		string applyLabel = "";
		public string ApplyLabel {
			get { return string.IsNullOrEmpty (applyLabel) ? Label : applyLabel; }
			protected set { applyLabel = value; }
		}


		// The icon name for this action (will be loaded from the theme).
		public readonly string IconName;

		protected Editor (string label, string iconName)
		{
			Label = label;
			IconName = iconName;
		}

		// Apply the editor's action to a photo.
		public void Apply ()
		{
			try {
				ProcessingStarted?.Invoke (Label, State.Items.Count ());
				TryApply ();
			} finally {
				ProcessingFinished?.Invoke ();
			}
		}

		void TryApply ()
		{
			if (NeedsSelection && !State.HasSelection) {
				throw new Exception ("Cannot apply without selection!");
			}

			int done = 0;
			foreach (Photo photo in State.Items) {
				LoadPhoto (photo, out var input, out var input_profile);

				Pixbuf edited = Process (input, input_profile);
				input.Dispose ();

				bool create_version = photo.DefaultVersion.IsProtected;
				photo.SaveVersion (edited, create_version);
				photo.Changes.DataChanged = true;
				App.Instance.Database.Photos.Commit (photo);

				done++;
				if (ProcessingStep != null) {
					ProcessingStep (done);
				}
			}

			Reset ();
		}

		protected abstract Pixbuf Process (Pixbuf input, Cms.Profile input_profile);

		protected virtual Pixbuf ProcessFast (Pixbuf input, Cms.Profile input_profile)
		{
			return Process (input, input_profile);
		}

		public bool HasSettings { get; protected set; }

		Pixbuf original;
		Pixbuf preview;
		protected void UpdatePreview ()
		{
			if (State.InBrowseMode) {
				throw new Exception ("Previews cannot be made in browse mode!");
			}

			if (State.Items.Count () > 1) {
				throw new Exception ("We should have one item selected when this happened, otherwise something is terribly wrong.");
			}

			if (original == null) {
				original = State.PhotoImageView.Pixbuf;
			}

			Pixbuf old_preview = null;
			if (preview == null) {
				CalcPreviewSize (original, out var width, out var height);
				preview = original.ScaleSimple (width, height, InterpType.Nearest);
			} else {
				// We're updating a previous preview
				old_preview = State.PhotoImageView.Pixbuf;
			}

			Pixbuf previewed = ProcessFast (preview, null);
			State.PhotoImageView.Pixbuf = previewed;
			State.PhotoImageView.ZoomFit (false);
			App.Instance.Organizer.InfoBox.UpdateHistogram (previewed);

			if (old_preview != null) {
				old_preview.Dispose ();
			}
		}

		void CalcPreviewSize (Pixbuf input, out int width, out int height)
		{
			int awidth = State.PhotoImageView.Allocation.Width;
			int aheight = State.PhotoImageView.Allocation.Height;
			int iwidth = input.Width;
			int iheight = input.Height;

			if (iwidth <= awidth && iheight <= aheight) {
				// Do not upscale
				width = iwidth;
				height = iheight;
			} else {
				double wratio = (double)iwidth / awidth;
				double hratio = (double)iheight / aheight;

				double ratio = Math.Max (wratio, hratio);
				width = (int)(iwidth / ratio);
				height = (int)(iheight / ratio);
			}
			//Log.Debug ("Preview size: Allocation: {0}x{1}, Input: {2}x{3}, Result: {4}x{5}", awidth, aheight, iwidth, iheight, width, height);
		}

		public void Restore ()
		{
			if (original != null && State.PhotoImageView != null) {
				State.PhotoImageView.Pixbuf = original;
				State.PhotoImageView.ZoomFit (false);

				App.Instance.Organizer.InfoBox.UpdateHistogram (null);
			}

			Reset ();
		}

		void Reset ()
		{
			preview?.Dispose ();

			preview = null;
			original = null;
			State = null;
		}

		// Can be overriden to provide a specific configuration widget.
		// Returning null means no configuration widget.
		public virtual Widget ConfigurationWidget ()
		{
			return null;
		}


		public virtual EditorState CreateState ()
		{
			return new EditorState ();
		}

		public delegate void InitializedHandler ();
		public event InitializedHandler Initialized;

		public void Initialize (EditorState state)
		{
			State = state;
			Initialized?.Invoke ();
		}
	}
}
