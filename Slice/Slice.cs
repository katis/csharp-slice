using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Slice is a wrapper around Array that can point to a part of it and can be resliced
    /// to create a new slice which points to a part of the same data as the original slice.
    /// </summary>
    public struct Slice<T> : IList<T>
    {
        internal readonly T[] array;
        internal readonly int offset;
        internal readonly int len;

        /// <summary>
        /// Returns the maximum capacity of the slice.
        /// </summary>
        public int Capacity
        {
            get { return array.Length - offset; }
        }

        /// <summary>
        /// Creates a new slice with the length and capacity of len.
        /// </summary>
        /// <param name="len">Length and capacity of the slice</param>
        public Slice(int len)
            : this(len, len)
        {
        }

        /// <summary>
        /// Creates a new slice with the provided length and capacity.
        /// </summary>
        /// <param name="len">Length of the slice</param>
        /// <param name="cap">Maximum capacity of the slice</param>
        public Slice(int len, int cap)
        {
            if (cap < len) throw new IndexOutOfRangeException("Slice capacity must not be longer than length.");
            this.array = new T[cap];
            this.offset = 0;
            this.len = len;
        }

        /// <summary>
        /// Creates a new slice with the provided array as data, from as offset from the array
        /// and to as the stop index.
        /// </summary>
        public Slice(T[] array, int from, int to)
        {
            if (array == null) throw new ArgumentNullException("Slice method argument array was null");
            if (array.Length < to) throw new ArgumentOutOfRangeException("Slice to-index must not be larger than the length of the array.");
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

        public static bool operator ==(Slice<T> a, Slice<T> b)
        {
            if (a.Count != b.Count) return false;

            for (int i = 0; i < a.Count; i++)
            {
                if (!a[i].Equals(b[i])) return false;
            }
            return true;
        }

        public static bool operator !=(Slice<T> a, Slice<T> b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Returns an item in the provided index.
        /// </summary>
        /// <returns>Item at index i</returns>
        public T this[int i]
        {
            get { return array[i + offset]; }
            set { array[i + offset] = value; }
        }

        /// <summary>
        /// Creates a new slice referencing the data in this slice from a new index.
        /// </summary>
        /// <param name="from">Start index of the new slice.</param>
        /// <param name="to">End index of the new slice.</param>
        public Slice<T> this[int from, int to]
        {
            get
            {
                if (from < 0) throw new ArgumentOutOfRangeException("Slice from-argument must be larger than 0");
                if (from > Capacity) throw new ArgumentOutOfRangeException("Slice to-argument must be larger than the capacity of the slice");
                return new Slice<T>(array, from + this.offset, this.offset + to);
            }
        }

        /// <summary>
        /// Creates a new slice referencing the data in this slice from a new index.
        /// </summary>
        /// <param name="from">Start index of the new slice</param>
        /// <param name="to">
        ///     Slice.End is the end of the slice index. (slice[0, Slice.End] == slice[0, slice.Count])
        ///     Slice.Full is the full capacity index of the slice. (slice[0, Slice.Full] == slice[0, slice.Capacity])
        /// </param>
        public Slice<T> this[int from, SliceTo to]
        {
            get
            {
                if (from < 0) throw new ArgumentOutOfRangeException("Slice from-argument must not be below zero.");
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

        /// <summary>
        /// Creates a new slice with the provided slice appended to this slice.
        /// </summary>
        /// <param name="slice">Slice to append to this slice.</param>
        public Slice<T> Append(Slice<T> slice)
        {
            int size = this.Count;
            int newlen = this.Count + slice.Count;
            var newarr = this.array;
            if (newlen > Capacity)
            {
                int newcap = newlen * 2;
                Array.Resize(ref newarr, newcap);
            }
            Array.Copy(slice.array, slice.offset, newarr, this.Count, slice.len);
            return new Slice<T>(newarr, 0, newlen);
        }

        /// <summary>
        /// Creates a new slice with the provided items appended to it.
        /// </summary>
        public Slice<T> Append(params T[] items)
        {
            if (items == null) throw new ArgumentNullException("Slice method argument items was null");
            return this.Append(Slice.Make(items));
        }

        /// <summary>
        /// Copies slice to the provided slice,
        /// limiting the copying to the length of the smaller slice.
        /// </summary>
        /// <returns>The items copied</returns>
        public int CopyTo(Slice<T> dst)
        {
            var len = Math.Min(Count, dst.Count);
            Array.Copy(array, offset, dst.array, dst.offset, len);
            return len;
        }

        /// <summary>
        /// Creates a copy of the slice.
        /// Does not produce an identical slice as only the capacity will be the
        /// current Count of the slice.
        /// </summary>
        public Slice<T> Clone()
        {
            var slice = new Slice<T>(Count);
            CopyTo(slice);
            return slice;
        }

        /// <summary>
        /// Returns the slice contents as an array.
        /// Only copies if the slice points only to a part of the array.
        /// </summary>
        public T[] ToArray()
        {
            if (offset == 0 && len == array.Length) return array;
            var a = new T[Count];
            Array.Copy(array, offset, a, 0, a.Length);
            return a;
        }

        /// <summary>
        /// Returns an array segment pointing to the same portion of the same array.
        /// </summary>
        public ArraySegment<T> ToArraySegment()
        {
            return new ArraySegment<T>(array, offset, Count);
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

        /// <summary>
        /// Number of items in the slice.
        /// </summary>
        public int Count
        {
            get { return len; }
        }

        /// <summary>
        /// Not supported!
        /// </summary>
        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported!
        /// </summary>
        void ICollection<T>.Clear()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns true if the slice contains the provided item.
        /// </summary>
        public bool Contains(T item)
        {
            foreach (T i in this)
            {
                if (i.Equals(item)) return true;
            }
            return false;
        }

        /// <summary>
        /// Copies the slice to array beginning from the
        /// </summary>
        /// <param name="arr"></param>
        /// <param name="arrayIndex"></param>
        void ICollection<T>.CopyTo(T[] arr, int arrayIndex)
        {
            Array.Copy(array, offset, arr, arrayIndex, Count);
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Not supported!
        /// </summary>
        bool ICollection<T>.Remove(T item)
        {
            throw new NotSupportedException();
        }

        #endregion ICollection<T>

        #region IList<T>

        /// <summary>
        /// Returns the index of the item or -1 if it wasn't found.
        /// </summary>
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

        /// <summary>
        /// Not supported!
        /// </summary>
        void IList<T>.Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported!
        /// </summary>
        void IList<T>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        #endregion IList<T>

        #region object overrides

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is Slice<T>)
            {
                return this == (Slice<T>)obj;
            }
            return false;
        }

        public bool Equals(Slice<T> s)
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
        /// <summary>
        /// Slicing operation end index that marks the full capacity of the slice.
        /// </summary>
        public static readonly SliceTo Full = SliceTo.Full;

        /// <summary>
        /// Slicing operation end index that marks the length of the slice.
        /// </summary>
        public static readonly SliceTo End = SliceTo.End;

        /// <summary>
        /// Creates a new slice with the provided items.
        /// </summary>
        public static Slice<T> Make<T>(params T[] items)
        {
            if (items == null) throw new ArgumentNullException("Slice method argument items was null");
            return new Slice<T>(items, 0, items.Length);
        }

        /// <summary>
        /// Converts an IEnumerable to a slice
        /// </summary>
        public static Slice<T> ToSlice<T>(this IEnumerable<T> e)
        {
            if (e == null) throw new ArgumentNullException("Slice method argument e was null");
            var arr = e.ToArray<T>();
            return new Slice<T>(arr, 0, arr.Length);
        }

        /// <summary>
        /// Read bytes to the slice using the provided reader function.
        /// </summary>
        /// <param name="readf">
        ///     Reader function taking a byte array,
        ///     offset from the beginning of the array and length to read to the array.
        ///     Returns number of bytes read.
        /// </param>
        /// <returns>Number of bytes read</returns>
        public static int ReadWith(this Slice<byte> slice, Func<byte[], int, int, int> readf)
        {
            if (readf == null) throw new ArgumentNullException("Slice method argument readf was null");
            return readf(slice.array, slice.offset, slice.len);
        }

        /// <summary>
        /// Write slice.Count bytes to the provided writer function.
        /// </summary>
        /// <param name="slice"></param>
        /// <param name="writef">
        ///     Writer function that takes a byte array,
        ///     offset from the beginning of the array and
        ///     number of bytes to write.
        /// </param>
        public static void WriteWith(this Slice<byte> slice, Action<byte[], int, int> writef)
        {
            if (writef == null) throw new ArgumentNullException("Slice method argument writef was null");
            writef(slice.array, slice.offset, slice.len);
        }

        /// <summary>
        /// Read bytes to the slice using the provided asynchronous reader function.
        /// </summary>
        /// <param name="readf">
        ///     Reader function taking a byte array,
        ///     offset from the beginning of the array and length to read to the array.
        ///     Returns a task containing the number of bytes read.
        /// </param>
        /// <returns>Task with number of bytes read.</returns>
        public static Task<int> ReadAsyncWith(this Slice<byte> slice, Func<byte[], int, int, Task<int>> readf)
        {
            if (readf == null) throw new ArgumentNullException("Slice method argument readf was null");
            return readf(slice.array, slice.offset, slice.len);
        }

        /// <summary>
        /// Write slice.Count bytes to the provided asynchronous writer function.
        /// </summary>
        /// <param name="writef">
        ///     Writer function that takes a byte array,
        ///     offset from the beginning of the array and
        ///     number of bytes to write.
        ///     Returns a task that is completed when the write operation finishes.
        /// </param>
        /// <returns>Task that completes when the write operation finishes.</returns>
        public static Task WriteAsyncWith(this Slice<byte> slice, Func<byte[], int, int, Task> writef)
        {
            if (writef == null) throw new ArgumentNullException("Slice method argument writef was null");
            return writef(slice.array, slice.offset, slice.len);
        }
    }
}