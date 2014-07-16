using pUnit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SliceBenchmarkRunner
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var runner = new ProfileRunner();
            runner.Run();
        }
    }
}