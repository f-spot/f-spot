/***************************************************************************
 *  IntervalHeap.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.Collections;
using System.Collections.Generic;

namespace Banshee.Kernel
{
    public class IntervalHeap<T> : ICollection<T>, ICollection, IEnumerable<T>, IEnumerable
    {
        private const int MIN_CAPACITY = 16;
    
        private int count;
        private int generation;
        
        private Interval [] heap;
        
        public IntervalHeap()
        {
            Clear();
        }
        
        public virtual T Pop()
        {
            if(count == 0) {
                throw new InvalidOperationException();
            }
            
            T item = heap[0].Item;
            MoveDown(0, heap[--count]);
            generation++;
            
            return item;
        }
        
        public virtual T Peek()
        {
            if(count == 0) {
                throw new InvalidOperationException();
            }
            
            return heap[0].Item;
        }

        public virtual void Push(T item, int priority)
        {
            if(item == null) {
                throw new ArgumentNullException("item");
            }
            
            if(count == heap.Length) {
                OptimalArrayResize(ref heap, 1);
            }
            
            MoveUp(++count - 1, new Interval(item, priority));
            generation++;
        }
        
        public virtual void Clear()
        {
            generation = 0;
            heap = new Interval[MIN_CAPACITY];
        }
        
        void ICollection.CopyTo(Array array, int index)
        {
            if(array == null) {
                throw new ArgumentNullException("array");
            }

            if(index < 0) {
                throw new ArgumentOutOfRangeException("index");
            }

            Array.Copy(heap, 0, array, index, count);
        }
        
        public virtual void CopyTo(T [] array, int index)
        {
            if(array == null) {
                throw new ArgumentNullException("array");
            }

            if(index < 0) {
                throw new ArgumentOutOfRangeException("index");
            }

            Array.Copy(heap, 0, array, index, count);
        }

        public virtual bool Contains(T item)
        {
            if(item == null) {
                throw new ArgumentNullException("item");
            }
            
            return FindItemHeapIndex(item) >= 0;
        }
        
        public virtual void Add(T item)
        {
            if(item == null) {
                throw new ArgumentNullException("item");
            }
            
            Push(item, 0);
        }
        
        public virtual bool Remove(T item)
        {
            if(item == null) {
                throw new ArgumentNullException("item");
            }
            
            int index = FindItemHeapIndex(item);
            
            if(index < 0) {
                return false;
            }
        
            MoveDown(index, heap[--count]);
            generation++;
            
            return true;
        }
        
        public virtual void TrimExcess()
        {
            if(count < heap.Length * 0.9) {
                Array.Resize(ref heap, count);
            }
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        public virtual IEnumerator<T> GetEnumerator()
        {
            return new IntervalHeapEnumerator(this);
        }
        
        public static IntervalHeap<T> Synchronized(IntervalHeap<T> heap)
        {
            if(heap == null) {
                throw new ArgumentNullException("heap");
            }
            
            return new SyncIntervalHeap(heap);
        }
        
        private int FindItemHeapIndex(T item)
        {
            for(int i = 0; i < count; i++) {
                if(item.Equals(heap[i].Item)) {
                    return i;
                }
            }
            
            return -1;
        }
        
        private static int GetLeftChildIndex(int index)
        {
            return index * 2 + 1;
        }
        
        private static int GetParentIndex(int index)
        {
            return (index - 1) / 2;
        }
        
        // grow array to nearest minimum power of two
        private static void OptimalArrayResize(ref Interval [] array, int grow)
        { 
            int new_capacity = array.Length == 0 ? 1 : array.Length;
            int min_capacity = array.Length == 0 ? MIN_CAPACITY : array.Length + grow;

            while(new_capacity < min_capacity) {
                new_capacity <<= 1;
            }

            Array.Resize(ref array, new_capacity);
        }

        private void MoveUp(int index, Interval node)
        {
            int parent_index = GetParentIndex(index);
            
            while(index > 0 && heap[parent_index].Priority < node.Priority) {
                heap[index] = heap[parent_index];
                index = parent_index;
                parent_index = GetParentIndex(index);
            }
            
            heap[index] = node;
        }
        
        private void MoveDown(int index, Interval node)
        {
            int child_index = GetLeftChildIndex(index);
            
            while(child_index < count) {
                if(child_index + 1 < count 
                    && heap[child_index].Priority < heap[child_index + 1].Priority) {
                    child_index++;
                }
                
                heap[index] = heap[child_index];
                index = child_index;
                child_index = GetLeftChildIndex(index);
            }
            
            MoveUp(index, node);
        }

        public virtual int Count {
            get { return count; }
        }
        
        public bool IsReadOnly {
            get { return false; }
        }
        
        public virtual object SyncRoot {
            get { return this; }
        }
        
        public virtual bool IsSynchronized {
            get { return false; }
        }
        
        private struct Interval
        {
            private T item;
            private int priority;
            
            public Interval(T item, int priority)
            {
                this.item = item;
                this.priority = priority;
            }
            
            public T Item {
                get { return item; }
            }
            
            public int Priority { 
                get { return priority; }
            }
	
	   public override int GetHashCode ()
	   {
		return priority.GetHashCode () ^ item.GetHashCode ();
	   }
        }
        
        private sealed class SyncIntervalHeap : IntervalHeap<T>
        {
            private IntervalHeap<T> heap;
            
            internal SyncIntervalHeap(IntervalHeap<T> heap)
            {
                this.heap = heap;
            }
            
            public override int Count {
                get { lock(heap) { return heap.Count; } }
            }
            
            public override bool IsSynchronized {
                get { return true; }
            }
            
            public override object SyncRoot {
                get { return heap.SyncRoot; }
            }
            
            public override void Clear()
            {
                lock(heap) { heap.Clear(); }
            }
            
            public override bool Contains(T item)
            {
                lock(heap) { return heap.Contains(item); }
            }
            
            public override T Pop()
            {
                lock(heap) { return heap.Pop(); }
            }
            
            public override T Peek()
            {
                lock(heap) { return heap.Peek(); }
            }
            
            public override void Push(T item, int priority)
            {
                lock(heap) { heap.Push(item, priority); }
            }
            
            public override void Add(T item)
            {
                lock(heap) { heap.Add(item); }
            }
            
            public override bool Remove(T item)
            {
                lock(heap) { return heap.Remove(item); }
            }
            
            public override void TrimExcess()
            {
                lock(heap) { heap.TrimExcess(); }
            }
            
            public override void CopyTo(T [] array, int index)
            {
                lock(heap) { heap.CopyTo(array, index); }
            }
            
            public override IEnumerator<T> GetEnumerator()
            {
                lock(heap) { return new IntervalHeapEnumerator(this); }
            }
        }
    
        private sealed class IntervalHeapEnumerator : IEnumerator<T>, IEnumerator
        {
            private IntervalHeap<T> heap;
            private int index;
            private int generation;
            
            public IntervalHeapEnumerator(IntervalHeap<T> heap)
            {
                this.heap = heap;
                Reset();
            }
            
            public void Reset()
            {
                generation = heap.generation;
                index = -1;
            }
            
            public void Dispose()
            {
                heap = null;
            }
 
            public bool MoveNext()
            {
                if(generation != heap.generation) {
                    throw new InvalidOperationException();
                }
                
                if(index + 1 == heap.count) {
                    return false;
                }
                
                index++;
                return true;
            }
            
            object IEnumerator.Current {
                get { return Current; }
            }
 
            public T Current {
                get {
                    if(generation != heap.generation) {
                        throw new InvalidOperationException();
                    }
                    
                    return heap.heap[index].Item;
                }
            }
        }
    }
}
 
