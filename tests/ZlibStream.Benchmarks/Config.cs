// Copyright (c) Six Labors and contributors.
// See LICENSE for more details.

#if Windows_NT
using System.Security.Principal;
using BenchmarkDotNet.Diagnostics.Windows;
#endif

using System;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using System.Linq;
using System.Reflection;

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

    public class DeflateConfig : Config
    {
        public DeflateConfig()
        {
            this.AddColumn(new ByteSizeColumn());
        }
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

    public class ByteSizeColumn : IColumn
    {
        private readonly string parameterName;

        public ByteSizeColumn(string parameterName)
        {
            this.parameterName = parameterName;
        }

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
            descriptor.GlobalSetupMethod.Invoke(instance, Array.Empty<object>());

            var p = (int)benchmarkCase.Parameters.Items[0].Value;
            PropertyInfo prop = descriptor.Type.GetProperty(this.parameterName);
            prop.SetValue(instance, p);
            return descriptor.WorkloadMethod.Invoke(instance, Array.Empty<object>()).ToString();
        }

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
            => this.GetValue(summary, benchmarkCase);

        public bool IsAvailable(Summary summary) => true;

        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase)
            => false;
    }
}
