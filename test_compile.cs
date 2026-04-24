using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HELIOS.Platform.Core.Intelligence;

class TestCompile
{
    static async Task Main()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        
        using var collector = new DataCollector(loggerFactory.CreateLogger<DataCollector>());
        using var normalizer = new DataNormalizer(loggerFactory.CreateLogger<DataNormalizer>());
        using var extractor = new FeatureExtractor(loggerFactory.CreateLogger<FeatureExtractor>());
        using var db = new InMemoryTimeSeriesDB(loggerFactory.CreateLogger<InMemoryTimeSeriesDB>());
        using var detector = new AnomalyDetector(loggerFactory.CreateLogger<AnomalyDetector>());
        using var analytics = new PredictiveAnalytics(loggerFactory.CreateLogger<PredictiveAnalytics>());
        using var manager = new MLModelManager(loggerFactory.CreateLogger<MLModelManager>());
        
        Console.WriteLine("All ML Intelligence services compiled successfully!");
    }
}
