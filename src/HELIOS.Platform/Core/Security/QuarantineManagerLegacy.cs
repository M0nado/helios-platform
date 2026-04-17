using System;
using System.Collections.Generic;

namespace HELIOS.Platform.Core.Security;

public enum QuarantineStatus
{
    Isolated,
    Analyzed,
    Restored,
    Deleted
}

public enum ThreatLevel
{
    Safe,
    Low,
    Medium,
    High,
    Critical
}

public class QuarantinedFileEntry
{
    public string FileId { get; set; }
    public string FileName { get; set; }
    public string OriginalPath { get; set; }
    public long FileSize { get; set; }
    public DateTime QuarantinedAt { get; set; }
    public DateTime? RestoredAt { get; set; }
    public string Reason { get; set; }
    public QuarantineStatus Status { get; set; }
}

public class QuarantineAnalysis
{
    public string FileId { get; set; }
    public bool IsMalicious { get; set; }
    public ThreatLevel ThreatLevel { get; set; }
    public string Details { get; set; }
    public DateTime AnalyzedAt { get; set; }
}

public class QuarantineStats
{
    public int TotalFilesQuarantined { get; set; }
    public int CurrentlyQuarantined { get; set; }
    public int FilesRestored { get; set; }
    public int FilesDeleted { get; set; }
    public long TotalQuarantineSize { get; set; }
}
