// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

using System.Reflection;
using BenchmarkDotNet.Running;

namespace ZlibStream.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            new BenchmarkSwitcher(typeof(Program).GetTypeInfo().Assembly).Run(args);
        }
    }
}
