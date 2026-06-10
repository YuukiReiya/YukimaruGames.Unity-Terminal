using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace YukimaruGames.Terminal.Benchmarks
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var config = DefaultConfig.Instance.WithOption(ConfigOptions.DisableOptimizationsValidator, true);
            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "../../../../BenchmarkReports/");
            config = config.WithArtifactsPath(outputPath);
            var summary = BenchmarkRunner.Run<TerminalColorBenchmarks>(config);
        }
    }
}