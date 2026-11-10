using System.Reflection;
using BenchmarkDotNet.Running;
using OwlScripts.Benchmarks;

var version = typeof(Program).Assembly
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
    .InformationalVersion.Split('+')[0];
Console.WriteLine($"""
    Owl Scripts Benchmarks [{version ?? "v0.0.0-dev"}]
    (c) Arch Leaders. MIT.

    """);

switch (args) {
    case [nameof(FindDistictInSets)]:
        BenchmarkRunner.Run<FindDistictInSets>();
        break;
}