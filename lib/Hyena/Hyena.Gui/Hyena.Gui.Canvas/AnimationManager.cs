//
// AnimationManager.cs
//
// Author:
//       Aaron Bockover <abockover@novell.com>
//
// Copyright 2009 Aaron Bockover
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Hyena.Gui.Theatrics;

namespace Hyena.Gui.Canvas
{
    public class Animation
    {
        //private int iterations;

        internal protected Stage<Animation> Stage { get; set; }
        internal protected Actor<Animation> Actor { get; set; }

        public Easing Easing { get; set; }
        public CanvasItem Item { get; set; }
        public int Repeat { get; set; }
        public uint Duration { get; set; }
        public Action<Animation> Update { get; set; }
        public Action<Animation> Finished { get; set; }

        public double Percent { get; private set; }

        public Animation ()
        {
            Duration = 1000;
            Easing = Easing.Linear;
            AnimationManager.Instance.Animate (this);
        }

        public Animation (double from, double to, Action<double> set) : this ()
        {
            Update = animation => set (from + animation.Percent * (to - from));
        }

        public void Start ()
        {
            if (Stage.Contains (this)) {
                Actor.Reset ();
                return;
            }

            Actor = Stage.Add (this, Duration);
            Percent = 0;
            //iterations = 0;
            Actor.CanExpire = false;
        }

        internal bool Step (Actor<Animation> actor)
        {
            bool is_expired = actor.Percent == 1;
            //bool is_expired = false;
            /*if (Repeat > 0 && actor.Percent == 1) {
                if (++iterations >= Repeat) {
                    is_expired = true;
                }
            }*/

            Percent = Choreographer.Compose (actor.Percent, Easing);
            Update (this);

            if (is_expired && Finished != null) {
                Finished (this);
            }

            if (Item != null) {
                Item.InvalidateRender ();
                //Item.Invalidate ();
            }

            return !is_expired;
        }
    }

    public class AnimationManager
    {
        private static AnimationManager instance;
        public static AnimationManager Instance {
            get { return instance ?? (instance = new AnimationManager ()); }
        }

        private Stage<Animation> stage = new Stage<Animation> ();

        public AnimationManager ()
        {
            stage.Play ();
            stage.ActorStep += (actor) => actor.Target.Step (actor);
        }

        internal void Animate (Animation animation)
        {
            animation.Stage = stage;
        }
    }
}
