using System;
using Gtk;
using Tao.OpenGl;
using Cairo;
using System.Runtime.InteropServices;
using FSpot;
using FSpot.Widgets;

namespace FSpot {
	public class TransitionList {
		GlTransition [] transitions = new GlTransition []
			{
				new GlTransition.Dissolve (),
				new GlTransition.Flip (),
				new GlTransition.Push (),
				new GlTransition.Reveal (),
				new GlTransition.Cover ()
			};
		int current_transition = 0;

		public TransitionList ()
		{

		}

		public event EventHandler Changed;

		public GlTransition Transition {
			get { return transitions [current_transition]; }
		}

		public int Index {
			get { return current_transition; }
			set { 
				current_transition = value;
				if (Changed != null)
					Changed (this, EventArgs.Empty);
			}
		}

		public ComboBox GetCombo ()
		{
			ComboBox combo = ComboBox.NewText ();
			combo.HasFrame = false;

			foreach (GlTransition t in transitions) {
				combo.AppendText (t.Name);
			}

			combo.Active = current_transition;

			combo.Changed += HandleComboChanged;
			combo.Show ();

			return combo;
		}
		
		private void HandleComboChanged (object sender, EventArgs args)
		{
			ComboBox combo = sender as ComboBox;
			string name = null;

			if (combo == null)
				return;

			TreeIter iter;

			if (combo.GetActiveIter (out iter))
				name = (string) combo.Model.GetValue (iter, 0);
			
			for (int i = 0; i < transitions.Length; i++)
				if (name == transitions[i].Name)
					Index = i;
		}
	}

	//[Binding(Gdk.Key.Up, "Spin", 1)]
	//[Binding(Gdk.Key.Down, "Spin", -1)]
	[Binding(Gdk.Key.Left, "Scale", .05f)]
	[Binding(Gdk.Key.Right, "Scale", -.05f)]

	public class TextureDisplay : Gtk.DrawingArea {
		BrowsablePointer item;
		GdkGlx.Context glx;
		float angle = 0.0f;
		Animator flip;
		bool running = false;

		public TextureDisplay (BrowsablePointer item)
		{
			this.item = item;
			DoubleBuffered = false;
			AppPaintable = true;
			CanFocus = true;
			item.Changed += HandleItemChanged;

			flip = new Animator (6000, 6000, delegate { flip.Start (); item.MoveNext (true); }); 
			flip.RunWhenStarted = false;
		}

		public override void Dispose ()
		{
			if (glx != null)
				glx.Destroy ();
		}

		GlTransition [] transitions = new GlTransition []
			{
				new GlTransition.Dissolve (),
				new GlTransition.Flip (),
				new GlTransition.Push (),
				new GlTransition.Reveal (),
				new GlTransition.Cover ()
			};
		int current_transition = 0;

		public GlTransition Transition {
			get { return transitions [current_transition]; }
		}

		public bool Spin (int amount)
		{
			current_transition += amount;

			if (current_transition >= transitions.Length)
				current_transition = 0;

			if (current_transition < 0)
				current_transition = transitions.Length -1;

			QueueDraw ();
			return true;
		}

		public bool Scale (float amount)
		{
			if (Transition != null)
				Transition.Percent += amount;

			QueueDraw ();
			return true;
		}
		
		public void Start ()
		{
			running = true;
			flip.Start ();
		}

		public void Stop ()
		{
			running = false;
			flip.Stop ();	
		}

		private Texture CreateTexture ()
		{
			if (glx == null || GdkWindow == null)
			   return null;
		
			glx.MakeCurrent (GdkWindow);

			Texture tex;
			try {
				using (ImageFile img = ImageFile.Create (item.Current.DefaultVersionUri)) {
					using (Gdk.Pixbuf pixbuf = img.Load ()) {
						FSpot.ColorManagement.ApplyScreenProfile (pixbuf, img.GetProfile ());
						tex = new Texture (pixbuf);
					}
				}
			} catch (Exception) {
				tex = new Texture (PixbufUtils.ErrorPixbuf);
			}
			return tex;
		}

		Animator animator;
		private void HandleItemChanged (BrowsablePointer p, BrowsablePointerChangedArgs args)
		{
			Animator = new Animator (3000, 20, HandleTick);

			if (glx == null)
				return;

			if (!item.IsValid || item.Collection.Count < 0)
				return;

			//Next = null;
			PreloadNext ();

			if (running)
				Animator.Start ();
		}

		private Animator Animator {
			get { return animator; }
			set {
				if (animator != null)
					animator.Stop ();

				if (value == null)
					flip.Start ();
				else 
					flip.Stop ();

				animator = value;
			}
		}

		public ComboBox GetCombo ()
		{
			ComboBox combo = ComboBox.NewText ();
			combo.HasFrame = false;

			foreach (GlTransition t in transitions)
				combo.AppendText (t.Name);
		       
			combo.Active = current_transition;
			combo.Changed += HandleComboChanged;
			combo.Show ();
			return combo;
		}
		
		private void HandleComboChanged (object sender, EventArgs args)
		{
			ComboBox combo = sender as ComboBox;
			string name = null;

			if (combo == null)
				return;

			TreeIter iter;

			if (combo.GetActiveIter (out iter))
				name = (string) combo.Model.GetValue (iter, 0);
			
			for (int i = 0; i < transitions.Length; i++)
				if (name == transitions[i].Name)
					current_transition = i;

			QueueDraw ();
		}

		public void HandleTick (object sender, EventArgs args)
		{
			if (!IsRealized) {
				System.Console.WriteLine ("animation running without active window");
				return;
			}

			if (Animator.Percent >= 1.0) {
				Transition.Percent = 1.0f;
				Animator = null;
			} else {
				Transition.Percent = Animator.Percent;
			}

			QueueDraw (); 
			GdkWindow.ProcessUpdates (false);
		}

		protected override void OnRealized ()
		{
#if FALSE
			if (CompositeUtils.IsComposited (Screen) && CompositeUtils.SetRgbaColormap (this))
				Console.WriteLine ("Set Rba");
#endif

			if (glx != null)
				glx.Destroy ();

			int [] attr = new int [] {
				(int) GdkGlx.GlxAttribute.Rgba,
				(int) GdkGlx.GlxAttribute.DepthSize, 16,
				(int) GdkGlx.GlxAttribute.DoubleBuffer,
				(int) GdkGlx.GlxAttribute.None
			};
			
			glx = new GdkGlx.Context (Screen, attr);
			Colormap = glx.GetColormap ();

			base.OnRealized ();
		}

		protected override void OnMapped ()
		{
			base.OnMapped ();

			if (Animator != null)
				Animator.Start ();
		}

		protected override void OnUnrealized ()
		{
			Animator = null;
			flip.Stop ();

			base.OnUnrealized ();

			if (glx != null)
				glx.Destroy ();
			
			glx = null;
		}

		protected override void OnDestroyed ()
		{
			item.Changed -= HandleItemChanged;
			base.OnDestroyed ();
		}

		Texture previous;
		public Texture Previous {
			get {
				if (previous == null)
					previous = Next;
				
				return previous;
			}
			set {
				if (previous != next && previous != null)
					previous.Dispose ();

				previous = value;
			}
		}

		Texture next;
		public Texture Next {
			get {
				if (next == null)
					PreloadNext ();
				return next;
			}
			set {
				Texture tmp = next;
				next = value;
				Previous = tmp;
			}
		}

		private void PreloadNext ()
		{
			Next = CreateTexture ();
		}

		GlTransition transition;
		private void DrawTransition ()
		{
			Transition.Draw (Allocation, Next, Previous);
		}

		private void DrawPixels ()
		{
			Gl.glClear (Gl.GL_COLOR_BUFFER_BIT);
			Gl.glPixelStorei (Gl.GL_UNPACK_ALIGNMENT, 1);
			Gl.glRasterPos2i (0, Allocation.Height);
			Gl.glPixelZoom (Allocation.Width / (float) next.Width, 
					- Allocation.Height / (float) next.Height);
			/*
			Gl.glDrawPixels (texture.Width, texture.Height, 
					 Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, 
					 texture.Pixels);
			*/
		}


		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			QueueDraw ();
		}

		public void PrintVersions ()
		{
			IntPtr version = Gl.glGetString (Gl.GL_VERSION);
			System.Console.WriteLine ("Version = {0}", Marshal.PtrToStringAnsi (version));
			
			IntPtr vendor = Gl.glGetString (Gl.GL_VENDOR);
			System.Console.WriteLine ("Vendor = {0}", Marshal.PtrToStringAnsi (vendor));
			
			IntPtr ext = Gl.glGetString (Gl.GL_EXTENSIONS);
			System.Console.WriteLine ("Extensions = {0}", Marshal.PtrToStringAnsi (ext));
		}

		void CheckError (string msg)
		{
			int error = Gl.glGetError ();
			if (error != Gl.GL_NO_ERROR)
				Console.WriteLine ("OpenGL error {0}: {1}", msg, Glu.gluErrorString (error));
		}

		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			glx.MakeCurrent (GdkWindow);

			CheckError ("entering expose");

			Gdk.Color c = Style.Background (State);
			Gl.glClearColor (c.Red / (float) ushort.MaxValue,
					 c.Blue / (float) ushort.MaxValue, 
					 c.Green / (float) ushort.MaxValue, 
					 1.0f);

			Gl.glShadeModel(Gl.GL_FLAT);
			
			//Gl.glMatrixMode(Gl.GL_MODELVIEW);
			//Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);
			Gl.glColor3f(1.0f, 1.0f, 1.0f);
			
			Gl.glEnable (Gl.GL_DEPTH_TEST);
			Gl.glEnable (Gl.GL_NORMALIZE);
			//Gl.glEnable (Gl.GL_BLEND);
			//Gl.glBlendFunc (Gl.GL_ONE, Gl.GL_ONE);
			Gl.glShadeModel (Gl.GL_FLAT);
			Gl.glEnable (Gl.GL_TEXTURE_RECTANGLE_ARB);
			//Gl.glPolygonMode (Gl.GL_FRONT_AND_BACK, Gl.GL_FILL);
			//Gl.glDisable (Gl.GL_CULL_FACE);
			//Gl.glLightModelf (Gl.GL_LIGHT_MODEL_TWO_SIDE, 0);
			//Gl.glEnable (Gl.GL_LIGHTING);
			//Gl.glEnable (Gl.GL_LIGHT0);
			//Gl.glLightfv (Gl.GL_LIGHT0, Gl.GL_DIFFUSE, new float [] { 1.0f, 1.0f, 0.0f, 1.0f });
			
			//Gl.glTexEnvi (Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_MODULATE);
			//Gl.glTexParameteri (Gl.GL_TEXTURE_RECTANGLE_ARB, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR);
			//Gl.glTexParameteri (Gl.GL_TEXTURE_RECTANGLE_ARB, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
			float [] black = new float [] { 0f, 0f, 0f, 1f};			       
			Gl.glTexParameterfv (Gl.GL_TEXTURE_RECTANGLE_ARB, Gl.GL_TEXTURE_BORDER_COLOR, black);
			Gl.glTexParameteri (Gl.GL_TEXTURE_RECTANGLE_ARB, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP_TO_BORDER_ARB);
			Gl.glTexParameteri (Gl.GL_TEXTURE_RECTANGLE_ARB, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP_TO_BORDER_ARB);
			
			// Viewing transformation
			Gl.glClear (Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
			//Gl.glMaterialfv (Gl.GL_FRONT, Gl.GL_DIFFUSE, new float [] { 1f, 1f, 1f, 1f});
			//Gl.glMaterialfv (Gl.GL_BACK, Gl.GL_DIFFUSE, new float [] { 1f, 1f, 1f, 1f});

			//Gl.glRotatef (35, 0, 1, 0);
			
			DrawTransition ();
			//DrawTextureSplit ();
			
			Gl.glFlush ();
			glx.SwapBuffers (GdkWindow);
			
			CheckError ("leaving expose");

			return true;
		}
	}
}
