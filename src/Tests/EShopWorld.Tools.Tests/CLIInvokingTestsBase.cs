﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit;

namespace EshopWorld.Tools.Tests
{
    /// <summary>
    /// base class for all CLI invoking - as a process - tests
    ///
    /// this allows for process to be run with given parameters and return the console output
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public abstract class CLIInvokingTestsBase
    {
        private double _processTimeout = TimeSpan.FromMinutes(2).TotalMilliseconds;

        // ReSharper disable once InconsistentNaming
        private Process RunCLI([NotNull] params string[] parameters)
        {
            var p = new Process();
            var sb= new StringBuilder();

            sb.AppendJoin(' ', "/c", "dotnet", "esw "); //the space is important here at the end
            
            if (parameters.Length > 0)
                sb.AppendJoin(' ', parameters);

            p.StartInfo = new ProcessStartInfo("cmd.exe", sb.ToString()) {CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true};
            p.Start().Should().BeTrue();
            p.WaitForExit((int) _processTimeout) .Should().BeTrue(); //never mind the cast

            return p;
        }

        protected string GetErrorOutput(params string[] parameters)
        {
            using (var p = RunCLI(parameters))
            {
                var errorStream = p.StandardError.ReadToEnd();
                errorStream.Should().NotBeNullOrEmpty();
                p.ExitCode.Should().NotBe(0);
                return errorStream;
            }
        }
        protected string GetStandardOutput(params string[] parameters)
        {
            using (var p = RunCLI(parameters))
            {
                p.StandardError.ReadToEnd().Should().BeNullOrWhiteSpace();
                p.ExitCode.Should().Be(0);             
                return p.StandardOutput.ReadToEnd();
            }
        }

        protected void DeleteTestFiles(string basePath, params string[] fileNames)
        {
            foreach (var f in fileNames)
            {
                File.Delete(Path.Combine(basePath, f));
            }
        }
    }
}
