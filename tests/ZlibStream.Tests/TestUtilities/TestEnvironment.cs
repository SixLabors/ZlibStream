// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ZlibStream.Tests.TestUtilities
{
    /// <summary>
    /// Provides information about the currently running test environment.
    /// </summary>
    public class TestEnvironment
    {
        private const string ZlibStreamSolutionFileName = "ZlibStream.sln";

        private const string CorpusRelativePath = @"tests\corpus";

        private static readonly Lazy<string> SolutionDirectoryFullPathLazy = new Lazy<string>(GetSolutionDirectoryFullPathImpl);

        private static readonly FileInfo TestAssemblyFile =
          new FileInfo(typeof(TestEnvironment).GetTypeInfo().Assembly.Location);

        internal static string SolutionDirectoryFullPath => SolutionDirectoryFullPathLazy.Value;

        /// <summary>
        /// Gets a value indicating whether test execution runs on CI.
        /// </summary>
#if ENV_CI
        internal static bool RunsOnCI => true;
#else
        internal static bool RunsOnCI => false;
#endif

        /// <summary>
        /// Gets a value indicating whether test execution is running with code coverage testing enabled.
        /// </summary>
#if ENV_CODECOV
        internal static bool RunsWithCodeCoverage => true;
#else
        internal static bool RunsWithCodeCoverage => false;
#endif

        /// <summary>
        /// Gets the full path to the Corpus directory.
        /// </summary>
        public static string CorpusDirectoryFullPath => GetFullPath(CorpusRelativePath);

        private static string GetFullPath(string relativePath)
            => Path.Combine(SolutionDirectoryFullPath, relativePath).Replace('\\', Path.DirectorySeparatorChar);

        private static string GetSolutionDirectoryFullPathImpl()
        {
            DirectoryInfo directory = TestAssemblyFile.Directory;

            while (!directory.EnumerateFiles(ZlibStreamSolutionFileName).Any())
            {
                try
                {
                    directory = directory.Parent;
                }
                catch (Exception ex)
                {
                    throw new DirectoryNotFoundException(
                        $"Unable to find ZlibStream solution directory from {TestAssemblyFile} because of {ex.GetType().Name}!",
                        ex);
                }

                if (directory is null)
                {
                    throw new DirectoryNotFoundException($"Unable to find ZlibStream solution directory from {TestAssemblyFile}!");
                }
            }

            return directory.FullName;
        }
    }
}
