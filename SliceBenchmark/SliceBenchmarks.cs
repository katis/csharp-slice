using Katis.Data;
using pUnit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SliceBenchmark
{
    [ProfileClass]
    public class SliceBenchmarks
    {
        [ProfileMethod(10000)]
        public void BenchNewArray()
        {
            var a = new int[1024];
            for (int i = 0; i < 100; i++)
            {
                a = new int[1024];
            }
            a[100] = 11;
        }

        [ProfileMethod(10000)]
        public void BenchNewSlice()
        {
            var a = new Slice<int>(1024);
            for (int i = 0; i < 100; i++)
            {
                a = new Slice<int>(1024);
            }
            a[100] = 11;
        }

        [ProfileMethod(1000)]
        public void BenchSmallAppend()
        {
            var s1 = new Slice<int>(5);
            var s2 = new Slice<int>(5);
            for (int i = 0; i < 20; i++)
            {
                s1 = s1.Append(s2);
            }
        }

        [ProfileMethod(1000)]
        public void BenchLargeAppend()
        {
            var s1 = new Slice<int>(1024);
            var s2 = new Slice<int>(1024);
            for (int i = 0; i < 20; i++)
            {
                s1 = s1.Append(s2);
            }
        }

        [ProfileMethod(1000)]
        public void BenchHugeAppend()
        {
            var s1 = new Slice<int>(66560);
            var s2 = new Slice<int>(66560);
            for (int i = 0; i < 20; i++)
            {
                s1 = s1.Append(s2);
            }
        }
    }
}