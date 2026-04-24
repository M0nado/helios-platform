using System;
using System.Collections.Generic;
using System.Text;

namespace Helios.Security.Validation
{
    /// <summary>
    /// Represents the result of a security validation check
    /// </summary>
    public class ValidationResult
    {
        public bool Passed { get; set; }
        public string Details { get; set; }
        public string CheckName { get; set; }
        public DateTime CheckedAt { get; set; }
        public string Category { get; set; }
        public int Score { get; set; } // 0-100

        public ValidationResult()
        {
            CheckedAt = DateTime.UtcNow;
            Details = string.Empty;
            CheckName = string.Empty;
            Category = string.Empty;
            Score = 0;
        }

        public override string ToString()
        {
            return $"[{Category}] {CheckName}: {(Passed ? "PASSED" : "FAILED")} (Score: {Score}/100)\n  {Details}";
        }
    }

    /// <summary>
    /// Aggregated results from audit run
    /// </summary>
    public class AuditResults
    {
        public List<ValidationResult> Results { get; set; }
        public DateTime AuditStartTime { get; set; }
        public DateTime AuditEndTime { get; set; }
        public int TotalChecks { get; set; }
        public int PassedChecks { get; set; }
        public int FailedChecks { get; set; }
        public double OverallScore { get; set; } // 0-100

        public AuditResults()
        {
            Results = new List<ValidationResult>();
            AuditStartTime = DateTime.UtcNow;
        }

        public void CalculateMetrics()
        {
            AuditEndTime = DateTime.UtcNow;
            TotalChecks = Results.Count;
            PassedChecks = Results.Count(r => r.Passed);
            FailedChecks = Results.Count(r => !r.Passed);
            OverallScore = TotalChecks > 0 ? (PassedChecks * 100.0) / TotalChecks : 0;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"\n=== SECURITY AUDIT REPORT ===");
            sb.AppendLine($"Audit Time: {AuditStartTime:yyyy-MM-dd HH:mm:ss UTC}");
            sb.AppendLine($"Duration: {(AuditEndTime - AuditStartTime).TotalSeconds:F2} seconds");
            sb.AppendLine($"\nOverall Score: {OverallScore:F1}/100");
            sb.AppendLine($"Passed: {PassedChecks}/{TotalChecks}");
            sb.AppendLine($"Failed: {FailedChecks}/{TotalChecks}");
            sb.AppendLine($"\n--- Detailed Results ---");
            
            foreach (var result in Results)
            {
                sb.AppendLine(result.ToString());
            }
            
            return sb.ToString();
        }
    }
}
