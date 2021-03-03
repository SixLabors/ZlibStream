// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

#if OS_WINDOWS
using System.Security.Principal;
using BenchmarkDotNet.Diagnostics.Windows;
#endif

using System;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace ZlibStream.Benchmarks
{
    public class Config : ManualConfig
    {
        public Config()
        {
            this.AddDiagnoser(MemoryDiagnoser.Default);

#if OS_WINDOWS
            if (this.IsElevated)
            {
                this.AddDiagnoser(new NativeMemoryProfiler());
            }
#endif
        }

#if OS_WINDOWS
        private bool IsElevated => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
#endif

    }

    public class DeflateConfig : ShortRun
    {
        public DeflateConfig()
            => this.AddColumn(new ByteSizeColumn(nameof(DeflateCorpusBenchmark.Compression)));
    }

    public class ShortRun : Config
    {
        public ShortRun()
            => this.AddJob(Job.Default.WithRuntime(CoreRuntime.Core31).WithLaunchCount(1).WithWarmupCount(3).WithIterationCount(3));
    }

    public class ByteSizeColumn : IColumn
    {
        private readonly string parameterName;

        public ByteSizeColumn(string parameterName) => this.parameterName = parameterName;

        public string Id => nameof(ByteSizeColumn) + "." + this.ColumnName + "." + this.parameterName;

        public string ColumnName => "Bytes";

        public bool AlwaysShow => true;

        public ColumnCategory Category => ColumnCategory.Custom;

        public int PriorityInCategory => 0;

        public bool IsNumeric => true;

        public UnitType UnitType => UnitType.Dimensionless;

        public string Legend => $"Output length in {this.ColumnName.ToLowerInvariant()}.";

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
        {
            Descriptor descriptor = benchmarkCase.Descriptor;

            var instance = Activator.CreateInstance(descriptor.Type);
            descriptor.GlobalSetupMethod?.Invoke(instance, Array.Empty<object>());

            var p = benchmarkCase.Parameters.Items.First(x => x.Name == this.parameterName).Value;
            if (p is int pint)
            {
                PropertyInfo prop = descriptor.Type.GetProperty(this.parameterName);
                prop.SetValue(instance, pint);
            }

            var args = Array.Empty<object>();
            if (benchmarkCase.HasArguments)
            {
                args = benchmarkCase.Parameters.Items.Where(x => x.IsArgument).Select(x => x.Value).ToArray();
            }

            return descriptor.WorkloadMethod.Invoke(instance, args).ToString();
        }

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
            => this.GetValue(summary, benchmarkCase);

        public bool IsAvailable(Summary summary) => true;

        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase)
            => false;
    }
}
