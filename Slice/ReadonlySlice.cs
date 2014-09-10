using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Katis.Data
{
    public struct ReadonlySlice<T> : ICollection<T>, IEnumerable<T>
    {
        private readonly T[] array;
        private readonly int offset;
        private readonly int len;

        public ReadonlySlice(int len, int cap = -1)
        {
            if (cap == -1) cap = len;
            if (cap < len) throw new IndexOutOfRangeException("ReadonlySlice capacity must not be longer than length.");
            this.array = new T[cap];
            this.offset = 0;
            this.len = len;
        }

        public ReadonlySlice(Slice<T> slice)
        {
            array = slice.array;
            offset = slice.offset;
            len = slice.len;
        }

        public ReadonlySlice(T[] array, int from, int to)
        {
            if (array == null) throw new ArgumentNullException("ReadonlySlice constructor argument array was null");
            if (array.Length < to) throw new ArgumentOutOfRangeException("ReadonlySlice to-index must not be larger than the length of the array.");
            this.array = array;
            this.offset = from;
            if (to < 0)
            {
                this.len = array.Length + to - from;
            }
            else
            {
                this.len = to - from;
            }
        }

        public T this[int index]
        {
            get { return array[offset + index]; }
        }

        public ReadonlySlice<T> this[int from, int to]
        {
            get
            {
                if (from < 0) throw new ArgumentOutOfRangeException("Slice from-argument must be larger than 0");
                if (from > Capacity) throw new ArgumentOutOfRangeException("Slice to-argument must be larger than the capacity of the slice");
                return new ReadonlySlice<T>(array, from + offset, offset + to);
            }
        }

        public ReadonlySlice<T> this[int from, SliceTo to]
        {
            get
            {
                if (from < 0) throw new ArgumentOutOfRangeException("Slice from-argument must not be below zero.");
                switch (to)
                {
                    case SliceTo.End:
                        return new ReadonlySlice<T>(array, from + this.offset, this.len + this.offset);

                    case SliceTo.Full:
                        return new ReadonlySlice<T>(array, from + this.offset, array.Length - from - this.offset);
                }
                throw new ArgumentException("Unknown SliceTo-value");
            }
        }

        public static bool operator ==(ReadonlySlice<T> a, ReadonlySlice<T> b)
        {
            if (a.Count != b.Count) return false;

            for (int i = 0; i < a.Count; i++)
            {
                if (!a[i].Equals(b[i])) return false;
            }
            return true;
        }

        public static bool operator !=(ReadonlySlice<T> a, ReadonlySlice<T> b)
        {
            return !(a == b);
        }

        public int Capacity
        {
            get { return array.Length - offset; }
        }

        #region ICollection<T>
        public bool Contains(T item)
        {
            foreach (var v in this)
            {
                if (item.Equals(v)) return true;
            }
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(this.array, offset, array, arrayIndex, Count);
        }

        public int Count
        {
            get { return len; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        void ICollection<T>.Add(T item)
        {
            throw new NotImplementedException();
        }

        bool ICollection<T>.Remove(T item)
        {
            throw new NotImplementedException();
        }

        void ICollection<T>.Clear()
        {
            throw new NotImplementedException();
        }

        #endregion ICollection<T>

        #region IEnumerator<T>

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion IEnumerator<T>

        #region object overrides

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is ReadonlySlice<T>)
            {
                return this == (ReadonlySlice<T>)obj;
            }
            return false;
        }

        public bool Equals(ReadonlySlice<T> s)
        {
            return this == s;
        }

        public override int GetHashCode()
        {
            int hc = 0;
            hc ^= this.offset;
            hc ^= this.len;
            foreach (var item in this)
            {
                hc ^= item.GetHashCode();
            }
            return hc;
        }

        public override string ToString()
        {
            var sb = new StringBuilder(len * 2);
            sb.Append(String.Format("ReadonlySlice<{0}>[", array.GetType().GetElementType().Name));
            for (int i = 0; i < Count; i++)
            {
                sb.Append(this[i]);
                if (i < Count - 1) sb.Append(", ");
            }
            sb.Append(']');
            return sb.ToString();
        }
        #endregion object overrides
    }
}
