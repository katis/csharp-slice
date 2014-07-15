using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Slice is a wrapper around Array that can point to a part of it and can be resliced
    /// to create a new slice which points to a part of the same data as the original slice.
    /// </summary>
    public sealed class Slice<T> : IList<T>
    {
        internal T[] array;
        internal int offset;
        internal int len;

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
            Contract.Requires(cap >= len);
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
            Contract.Requires(array != null);
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

        /// <summary>
        /// Returns an item in the provided index.
        /// </summary>
        /// <returns>Item at index i</returns>
        public T this[int i]
        {
            get
            {
                Contract.Requires(i >= 0);
                Contract.Requires(i < Count);
                return array[i + offset];
            }
            set
            {
                Contract.Requires(i < Count);
                Contract.Requires(i >= 0);
                array[i + offset] = value;
            }
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
                Contract.Requires(from >= 0);
                Contract.Requires(from <= Capacity);
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
                Contract.Requires(from >= 0);
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
            Contract.Requires(slice != null);
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

        /// <summary>
        /// Creates a new slice with the provided items appended to it.
        /// </summary>
        public Slice<T> Append(params T[] items)
        {
            Contract.Requires(items != null);
            return this.Append(Slice.Make(items));
        }

        /// <summary>
        /// Copies slice to the provided slice,
        /// limiting the copying to the length of the smaller slice.
        /// </summary>
        /// <returns>The items copied</returns>
        public int CopyTo(Slice<T> dst)
        {
            Contract.Requires(dst != null);
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
        /// Add an item to the end of the slice.
        /// </summary>
        public void Add(T item)
        {
            Insert(Count, item);
        }

        /// <summary>
        /// Clears the array and sets it's count to zero.
        /// </summary>
        public void Clear()
        {
            Array.Clear(array, offset, len);
            this.len = 0;
        }

        /// <summary>
        /// Returns true if the slice contains the provided item.
        /// </summary>
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

        /// <summary>
        /// Copies the slice to array beginning from the
        /// </summary>
        /// <param name="arr"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(T[] arr, int arrayIndex)
        {
            if (Count > arr.Length - arrayIndex) throw new ArgumentException("No room in the provided array");
            Array.Copy(array, offset, arr, arrayIndex, Count);
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Removes the provided item from the slice if it exists.
        /// </summary>
        /// <returns>True if the item was removed.</returns>
        public bool Remove(T item)
        {
            int foundi = IndexOf(item);
            if (foundi == -1) return false;

            RemoveAt(foundi);
            return true;
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
        /// Insert a new item to the provided index.
        /// </summary>
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

        /// <summary>
        /// Removes an item at the provided index index.
        /// </summary>
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
            Contract.Requires(items != null);
            return new Slice<T>(items, 0, items.Length);
        }

        /// <summary>
        /// Converts an IEnumerable to a slice
        /// </summary>
        public static Slice<T> ToSlice<T>(this IEnumerable<T> e)
        {
            Contract.Requires(e != null);
            var arr = e.ToArray<T>();
            return new Slice<T>(arr, 0, arr.Length);
        }

        /// <summary>
        /// Asynchronously Reads slice.Count bytes from the stream.
        /// </summary>
        /// <returns>Bytes read</returns>
        public static Task<int> ReadAsync(this Stream stream, Slice<byte> slice)
        {
            Contract.Requires(stream != null);
            Contract.Requires(slice != null);
            return stream.ReadAsync(slice.array, slice.offset, slice.len);
        }

        /// <summary>
        /// Writes slice.Count bytes to the stream asynchronously.
        /// </summary>
        public static Task WriteAsync(this Stream stream, Slice<byte> slice)
        {
            Contract.Requires(stream != null);
            Contract.Requires(slice != null);
            return stream.WriteAsync(slice.array, slice.offset, slice.len);
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
            Contract.Requires(slice != null);
            Contract.Requires(readf != null);
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
            Contract.Requires(slice != null);
            Contract.Requires(writef != null);
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
            Contract.Requires(slice != null);
            Contract.Requires(readf != null);
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
            Contract.Requires(slice != null);
            Contract.Requires(writef != null);
            return writef(slice.array, slice.offset, slice.len);
        }
    }
}