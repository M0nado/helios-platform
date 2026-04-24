using System;
using System.Collections.Generic;
using System.Linq;

namespace HELIOS.Platform.AI.MLService
{
    /// <summary>
    /// Feature preprocessing pipeline for normalization, scaling, and feature engineering.
    /// </summary>
    public class FeaturePreprocessor
    {
        private Dictionary<int, NormalizationStats> _statsCache = new();
        private List<string> _featureNames = new();

        /// <summary>
        /// Normalize features to 0-1 range using min-max scaling.
        /// </summary>
        public float[] NormalizeMinMax(float[] features, Dictionary<int, (float Min, float Max)>? ranges = null)
        {
            if (features == null || features.Length == 0) return features;

            var normalized = new float[features.Length];

            for (int i = 0; i < features.Length; i++)
            {
                if (ranges != null && ranges.TryGetValue(i, out var range))
                {
                    var denominator = range.Max - range.Min;
                    normalized[i] = denominator == 0 ? 0 : (features[i] - range.Min) / denominator;
                }
                else
                {
                    normalized[i] = features[i];
                }
            }

            return normalized;
        }

        /// <summary>
        /// Standardize features using z-score normalization.
        /// </summary>
        public float[] Standardize(float[] features, Dictionary<int, (float Mean, float StdDev)>? stats = null)
        {
            if (features == null || features.Length == 0) return features;

            var standardized = new float[features.Length];

            for (int i = 0; i < features.Length; i++)
            {
                if (stats != null && stats.TryGetValue(i, out var stat))
                {
                    standardized[i] = stat.StdDev == 0 ? 0 : (features[i] - stat.Mean) / stat.StdDev;
                }
                else
                {
                    standardized[i] = features[i];
                }
            }

            return standardized;
        }

        /// <summary>
        /// Calculate statistics for a batch of feature vectors.
        /// </summary>
        public Dictionary<int, (float Mean, float StdDev)> CalculateStats(List<float[]> featureBatch)
        {
            var stats = new Dictionary<int, (float, float)>();

            if (featureBatch == null || featureBatch.Count == 0) return stats;

            int featureCount = featureBatch[0].Length;

            for (int i = 0; i < featureCount; i++)
            {
                var values = featureBatch.Select(f => (double)f[i]).ToList();
                var mean = values.Average();
                var variance = values.Sum(v => Math.Pow(v - mean, 2)) / values.Count;
                var stdDev = Math.Sqrt(variance);

                stats[i] = ((float)mean, (float)stdDev);
            }

            return stats;
        }

        /// <summary>
        /// Handle missing values with mean imputation.
        /// </summary>
        public float[] ImputeMissing(float[] features, float missingValue = float.NaN, float defaultValue = 0)
        {
            if (features == null) return features;

            var imputed = new float[features.Length];
            for (int i = 0; i < features.Length; i++)
            {
                imputed[i] = float.IsNaN(features[i]) || features[i] == missingValue ? defaultValue : features[i];
            }

            return imputed;
        }

        /// <summary>
        /// Detect and remove outliers using IQR method.
        /// </summary>
        public List<float[]> RemoveOutliers(List<float[]> featureBatch, float iqrMultiplier = 1.5f)
        {
            if (featureBatch == null || featureBatch.Count < 4) return featureBatch;

            var filtered = new List<float[]>();
            int featureCount = featureBatch[0].Length;

            for (int i = 0; i < featureCount; i++)
            {
                var values = featureBatch.Select(f => f[i]).OrderBy(v => v).ToList();
                int n = values.Count;

                var q1 = values[(int)(n * 0.25)];
                var q3 = values[(int)(n * 0.75)];
                var iqr = q3 - q1;

                var lowerBound = q1 - iqrMultiplier * iqr;
                var upperBound = q3 + iqrMultiplier * iqr;

                foreach (var features in featureBatch)
                {
                    if (features[i] >= lowerBound && features[i] <= upperBound)
                    {
                        filtered.Add(features);
                    }
                }
            }

            return filtered;
        }

        /// <summary>
        /// Extract basic statistics as features.
        /// </summary>
        public float[] ExtractStatisticFeatures(float[] values)
        {
            if (values == null || values.Length == 0) return Array.Empty<float>();

            var features = new List<float>
            {
                values.Average(),
                values.Max(),
                values.Min(),
                (float)values.StandardDeviation(),
                values.Sum()
            };

            return features.ToArray();
        }

        public class NormalizationStats
        {
            public float Mean { get; set; }
            public float StdDev { get; set; }
            public float Min { get; set; }
            public float Max { get; set; }
        }
    }

    /// <summary>
    /// Extension methods for statistical calculations.
    /// </summary>
    public static class StatisticExtensions
    {
        public static double StandardDeviation(this IEnumerable<float> values)
        {
            var valueList = values.ToList();
            if (valueList.Count < 2) return 0;

            var mean = valueList.Average();
            var variance = valueList.Sum(v => Math.Pow(v - mean, 2)) / valueList.Count;
            return Math.Sqrt(variance);
        }

        public static double StandardDeviation(this IEnumerable<double> values)
        {
            var valueList = values.ToList();
            if (valueList.Count < 2) return 0;

            var mean = valueList.Average();
            var variance = valueList.Sum(v => Math.Pow(v - mean, 2)) / valueList.Count;
            return Math.Sqrt(variance);
        }
    }
}
