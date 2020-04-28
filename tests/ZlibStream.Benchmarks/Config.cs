// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

#if Windows_NT
using System.Security.Principal;
using BenchmarkDotNet.Diagnostics.Windows;
#endif

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace ZlibStream.Benchmarks
{
    public class Config : ManualConfig
    {
        public Config()
        {
            this.Add(MemoryDiagnoser.Default);

#if Windows_NT
            if (this.IsElevated)
            {
                this.Add(new NativeMemoryProfiler());
            }
#endif
        }

#if Windows_NT
        private bool IsElevated
        {
            get
            {
                return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
#endif

    }

    public class ShortRun : Config
    {
        public ShortRun()
        {
            this.AddJob(Job.Default.WithRuntime(
                CoreRuntime.Core31).WithLaunchCount(1)
                .WithWarmupCount(3)
                .WithIterationCount(3));
        }
    }
}
