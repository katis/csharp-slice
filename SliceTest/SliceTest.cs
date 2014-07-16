using Katis.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace SliceTest
{
    [TestClass]
    public class SliceTest
    {
        private Slice<int> Zero()
        {
            var arr = new int[] { };
            return new Slice<int>(arr, 0, arr.Length);
        }

        private Slice<int> Ten()
        {
            var arr = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            return new Slice<int>(arr, 0, arr.Length);
        }

        private Slice<int> Five()
        {
            var arr = new int[] { 0, 1, 2, 3, 4 };
            return new Slice<int>(arr, 0, arr.Length);
        }

        [TestMethod]
        public void TestEquality()
        {
            var s1 = Ten();
            var s2 = Ten();
            var five = Five();
            Assert.IsTrue(s1.Equals(s2));
            Assert.IsFalse(s1.Equals(five));
            Assert.IsTrue(s1 == s2);
            Assert.IsFalse(s1 == five);
        }

        [TestMethod]
        public void TestSlicing()
        {
            var ten = Ten();
            var five = Five();

            Assert.IsTrue(ten[0, 5].Equals(five));
            Assert.IsTrue(ten[0, -5].Equals(five));
        }

        [TestMethod]
        public void TestSlicingCorners()
        {
            var ten = Ten();
            var zero = Zero();
            Assert.IsTrue(ten[0, 0].Equals(zero));
            Assert.IsTrue(ten.Equals(Ten()));
        }

        [TestMethod]
        public void TestIteration()
        {
            var i = 0;
            var ten = Ten();
            foreach (var item in ten)
            {
                Trace.WriteLine(item);
                Assert.IsTrue(item.Equals(i));
                i++;
            }
            Assert.IsTrue(i == 10);
        }

        [TestMethod]
        public void TestAppend()
        {
            var fives = Five().Append(Five());
            var res1 = Slice.Make(0, 1, 2, 3, 4, 0, 1, 2, 3, 4);
            Assert.AreEqual(fives, res1);

            var seven = Five().Append(5, 6);
            var res2 = Slice.Make(0, 1, 2, 3, 4, 5, 6);
            Assert.AreEqual(seven, res2);

            var s = new Slice<int>(5, 10);
            Assert.IsTrue(s.Count == 5);
            Assert.IsTrue(s.Capacity == 10);
            for (int k = 0; k < s.Count; k++)
            {
                s[k] = k;
            }
            s = s.Append(Five());
            var res3 = Slice.Make(0, 1, 2, 3, 4, 0, 1, 2, 3, 4);
            Assert.AreEqual(s, res3);
            Assert.IsTrue(s.Count == 10);
        }

        [TestMethod]
        public void TestCopyToSlice()
        {
            var s1 = Slice.Make(1, 1, 1, 1, 1, 1);
            var s2 = Slice.Make(2, 2, 2, 2);
            s1.CopyTo(s2);
            foreach (var i in s2)
            {
                Assert.IsTrue(i == 1);
            }

            s1 = Slice.Make(1, 1, 1);
            s2 = Slice.Make(2, 2, 2, 2, 2);
            s1.CopyTo(s2);
            Assert.AreEqual(s2, Slice.Make(1, 1, 1, 2, 2));
        }

        [TestMethod]
        public void TestCopyToOverlappingSlice()
        {
            var parent = Slice.Make(0, 1, 2, 3, 4, 5, 6, 7);
            var s1 = parent[0, 6];
            var s2 = parent[3, Slice.End];

            Trace.WriteLine(s1);
            Trace.WriteLine(s2);

            var n = s1.CopyTo(s2);
            Assert.IsTrue(n == 5);

            Trace.WriteLine(s1);
            Trace.WriteLine(s2);

            Assert.AreEqual(Slice.Make(0, 1, 2, 3, 4), s2);
        }

        [TestMethod]
        public void TestSliceTo()
        {
            var s = new Slice<int>(5, 10);
            Five().CopyTo(s);

            var end = s[2, Slice.End];
            Assert.AreEqual(Slice.Make(2, 3, 4), end);

            var full = s[0, Slice.Full];
            Assert.AreEqual(Slice.Make(0, 1, 2, 3, 4, 0, 0, 0, 0, 0), full);
        }

        [TestMethod]
        public void TestIndexOf()
        {
            var s = Five();
            Assert.AreEqual(s.IndexOf(0), 0);
            Assert.AreEqual(s.IndexOf(4), 4);
            Assert.AreEqual(s.IndexOf(99), -1);
        }

        [TestMethod]
        public void TestToString()
        {
            var s = new Slice<string>(0);
            Assert.AreEqual(s.ToString(), "Slice<String>[]");

            var i = Slice.Make(0, 1, 2, 3);
            Assert.AreEqual(i.ToString(), "Slice<Int32>[0, 1, 2, 3]");
        }
    }
}