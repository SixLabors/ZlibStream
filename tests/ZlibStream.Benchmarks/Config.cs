// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;

namespace ZlibStream.Benchmarks
{
    public class Config : ManualConfig
    {
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
