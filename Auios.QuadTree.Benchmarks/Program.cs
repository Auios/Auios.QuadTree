using BenchmarkDotNet.Running;

// dotnet run -c Release --project Auios.QuadTree.Benchmarks -- --filter *
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
