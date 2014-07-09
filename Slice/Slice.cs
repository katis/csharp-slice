using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Katis.Data
{
    public enum SliceTo
    {
        End,
        Full,
    }

    public class Slice<T> : IList<T>
    {
        internal T[] array;
        internal int offset;
        internal int len;

        public int Capacity
        {
            get { return array.Length; }
        }

        public Slice(int len)
            : this(len, len)
        {
        }

        public Slice(int len, int cap)
        {
            Contract.Requires(cap >= len);
            this.array = new T[cap];
            this.offset = 0;
            this.len = len;
        }

        public Slice(T[] array, int from, int to)
        {
            Contract.Requires(array.Length >= to);
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

        public T this[int i]
        {
            get { return array[i + offset]; }
            set { array[i + offset] = value; }
        }

        public Slice<T> this[int from, int to]
        {
            get { return new Slice<T>(array, from + this.offset, this.offset + to); }
        }

        public Slice<T> this[int from, SliceTo to]
        {
            get
            {
                switch (to)
                {
                    case SliceTo.End:
                        return new Slice<T>(array, from + this.offset, this.len + this.offset);

                    case SliceTo.Full:
                        return new Slice<T>(array, from + this.offset, array.Length - from - this.offset);
                }
                throw new ArgumentException("Unknown SliceTo-value");
            }
        }

        public Slice<T> Append(Slice<T> slice)
        {
            int size = this.Count;
            int newsize = this.Count + slice.Count;
            var newarr = this.array;
            if (newsize > array.Length)
            {
                Array.Resize(ref newarr, newsize);
            }
            Array.Copy(slice.array, slice.offset, newarr, this.Count, slice.len);
            return new Slice<T>(newarr, 0, newsize);
        }

        public Slice<T> Append(params T[] items)
        {
            return this.Append(Slice.Make(items));
        }

        public int CopyTo(Slice<T> dst)
        {
            var len = Math.Min(Count, dst.Count);
            Array.Copy(array, offset, dst.array, dst.offset, len);
            return len;
        }

        #region IEnumerable<T>

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < len; i++)
            {
                yield return this[i];
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion IEnumerable<T>

        #region ICollection<T>

        public int Count
        {
            get { return len; }
        }

        public void Add(T item)
        {
            Insert(Count, item);
        }

        public void Clear()
        {
            ((System.Collections.IList)array).Clear();
            this.offset = 0;
            this.len = 0;
        }

        public bool Contains(T item)
        {
            foreach (T i in this)
            {
                if (i.Equals(item))
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(T[] arr, int arrayIndex)
        {
            var max = Math.Min(arr.Length - arrayIndex, this.len);
            Array.Copy(array, offset, arr, arrayIndex, max);
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            int foundi = IndexOf(item);
            if (foundi == -1) return false;

            RemoveAt(foundi);
            return true;
        }

        #endregion ICollection<T>

        #region IList<T>

        public int IndexOf(T item)
        {
            for (int i = 0; i < len; i++)
            {
                if (item.Equals(this[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public void Insert(int index, T item)
        {
            Contract.Requires<IndexOutOfRangeException>(index >= 0);
            Contract.Requires<IndexOutOfRangeException>(index <= Count);

            var newarr = new T[Count + 1];

            if (index != 0)
            {
                Array.Copy(array, offset, newarr, 0, index);
            }
            newarr[index] = item;
            if (index < Count)
            {
                Array.Copy(array, offset + index, newarr, index + 1, newarr.Length - 1 - index);
            }
            this.array = newarr;
            this.offset = 0;
            this.len = array.Length;
        }

        public void RemoveAt(int index)
        {
            Contract.Requires<IndexOutOfRangeException>(index >= 0);
            Contract.Requires<IndexOutOfRangeException>(index < Count);

            var newarr = new T[Count - 1];
            Array.Copy(array, offset, newarr, 0, index);
            Array.Copy(array, offset + index + 1, newarr, index, newarr.Length - index);
            this.array = newarr;
            this.offset = 0;
            this.len = newarr.Length;
        }

        #endregion IList<T>

        #region object overrides

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return Equals(obj as Slice<T>);
        }

        public bool Equals(Slice<T> s)
        {
            if (s == null) return false;
            if (s.Count != this.Count) return false;

            for (int i = 0; i < Count; i++)
            {
                if (!this[i].Equals(s[i])) return false;
            }
            return true;
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
            sb.Append(String.Format("Slice<{0}>[", array.GetType().GetElementType().Name));
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

    public static class Slice
    {
        public static readonly SliceTo Full = SliceTo.Full;
        public static readonly SliceTo End = SliceTo.End;

        public static Slice<T> Make<T>(params T[] items)
        {
            return new Slice<T>(items, 0, items.Length);
        }

        public static Slice<T> ToSlice<T>(this IEnumerable<T> e)
        {
            var arr = e.ToArray<T>();
            return new Slice<T>(arr, 0, arr.Length);
        }

        public static Task<int> ReadSlice(this Stream stream, Slice<byte> slice)
        {
            return stream.ReadAsync(slice.array, slice.offset, slice.len);
        }

        public static Task WriteSlice(this Stream stream, Slice<byte> slice)
        {
            return stream.WriteAsync(slice.array, slice.offset, slice.len);
        }
    }
}