using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HELIOS.Platform.Core.Plugins.Versioning
{
    /// <summary>
    /// Semantic versioning implementation following semver.org
    /// </summary>
    public class SemanticVersion : IComparable<SemanticVersion>, IEquatable<SemanticVersion>
    {
        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }
        public string PreRelease { get; }
        public string Metadata { get; }

        public SemanticVersion(int major, int minor = 0, int patch = 0, string preRelease = "", string metadata = "")
        {
            if (major < 0 || minor < 0 || patch < 0)
                throw new ArgumentException("Version numbers cannot be negative");

            Major = major;
            Minor = minor;
            Patch = patch;
            PreRelease = preRelease ?? "";
            Metadata = metadata ?? "";
        }

        /// <summary>
        /// Parse a semantic version string
        /// </summary>
        public static SemanticVersion Parse(string versionString)
        {
            if (string.IsNullOrWhiteSpace(versionString))
                throw new ArgumentException("Version string cannot be empty");

            // Remove 'v' prefix if present
            versionString = versionString.TrimStart('v');

            // Regex pattern for semver
            var pattern = @"^(\d+)\.(\d+)\.(\d+)(?:-([a-zA-Z0-9.-]+))?(?:\+([a-zA-Z0-9.-]+))?$";
            var match = Regex.Match(versionString, pattern);

            if (!match.Success)
                throw new ArgumentException($"Invalid semantic version: {versionString}");

            var major = int.Parse(match.Groups[1].Value);
            var minor = int.Parse(match.Groups[2].Value);
            var patch = int.Parse(match.Groups[3].Value);
            var preRelease = match.Groups[4].Value;
            var metadata = match.Groups[5].Value;

            return new SemanticVersion(major, minor, patch, preRelease, metadata);
        }

        /// <summary>
        /// Try to parse a semantic version string
        /// </summary>
        public static bool TryParse(string versionString, out SemanticVersion version)
        {
            version = null;
            try
            {
                version = Parse(versionString);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public int CompareTo(SemanticVersion other)
        {
            if (other == null) return 1;

            // Compare major.minor.patch
            var majorComparison = Major.CompareTo(other.Major);
            if (majorComparison != 0) return majorComparison;

            var minorComparison = Minor.CompareTo(other.Minor);
            if (minorComparison != 0) return minorComparison;

            var patchComparison = Patch.CompareTo(other.Patch);
            if (patchComparison != 0) return patchComparison;

            // Pre-release versions have lower precedence
            var thisHasPreRelease = !string.IsNullOrEmpty(PreRelease);
            var otherHasPreRelease = !string.IsNullOrEmpty(other.PreRelease);

            if (thisHasPreRelease && !otherHasPreRelease) return -1;
            if (!thisHasPreRelease && otherHasPreRelease) return 1;

            return string.CompareOrdinal(PreRelease, other.PreRelease);
        }

        public bool Equals(SemanticVersion other)
        {
            if (other == null) return false;
            return Major == other.Major &&
                   Minor == other.Minor &&
                   Patch == other.Patch &&
                   PreRelease == other.PreRelease;
        }

        public override bool Equals(object obj) => Equals(obj as SemanticVersion);
        public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, PreRelease);

        public override string ToString()
        {
            var version = $"{Major}.{Minor}.{Patch}";
            if (!string.IsNullOrEmpty(PreRelease))
                version += $"-{PreRelease}";
            if (!string.IsNullOrEmpty(Metadata))
                version += $"+{Metadata}";
            return version;
        }

        public static bool operator ==(SemanticVersion a, SemanticVersion b)
        {
            if (a is null && b is null) return true;
            if (a is null || b is null) return false;
            return a.Equals(b);
        }

        public static bool operator !=(SemanticVersion a, SemanticVersion b) => !(a == b);
        public static bool operator <(SemanticVersion a, SemanticVersion b) => a?.CompareTo(b) < 0;
        public static bool operator <=(SemanticVersion a, SemanticVersion b) => a?.CompareTo(b) <= 0;
        public static bool operator >(SemanticVersion a, SemanticVersion b) => a?.CompareTo(b) > 0;
        public static bool operator >=(SemanticVersion a, SemanticVersion b) => a?.CompareTo(b) >= 0;
    }

    /// <summary>
    /// Version constraint resolver (supports npm-style version ranges)
    /// </summary>
    public class VersionConstraint
    {
        public string RawConstraint { get; }

        public VersionConstraint(string constraint)
        {
            RawConstraint = constraint ?? "*";
        }

        /// <summary>
        /// Check if a version satisfies this constraint
        /// </summary>
        public bool IsSatisfiedBy(SemanticVersion version)
        {
            if (string.IsNullOrEmpty(RawConstraint) || RawConstraint == "*")
                return true;

            var constraint = RawConstraint.Trim();

            // Exact version
            if (!constraint.Contains(".") && !constraint.Contains("^") && !constraint.Contains("~")
                && !constraint.Contains(">") && !constraint.Contains("<") && !constraint.Contains("="))
            {
                return version == SemanticVersion.Parse(constraint);
            }

            // Caret (^) - allows changes that do not modify left-most non-zero digit
            if (constraint.StartsWith("^"))
            {
                var baseVersion = SemanticVersion.Parse(constraint.Substring(1));
                return version >= baseVersion && version.Major == baseVersion.Major;
            }

            // Tilde (~) - allows patch-level changes
            if (constraint.StartsWith("~"))
            {
                var baseVersion = SemanticVersion.Parse(constraint.Substring(1));
                return version >= baseVersion && version.Major == baseVersion.Major && version.Minor == baseVersion.Minor;
            }

            // Exact match
            if (constraint.StartsWith("="))
            {
                return version == SemanticVersion.Parse(constraint.Substring(1));
            }

            // Greater than or equal
            if (constraint.StartsWith(">="))
            {
                return version >= SemanticVersion.Parse(constraint.Substring(2));
            }

            // Greater than
            if (constraint.StartsWith(">"))
            {
                return version > SemanticVersion.Parse(constraint.Substring(1));
            }

            // Less than or equal
            if (constraint.StartsWith("<="))
            {
                return version <= SemanticVersion.Parse(constraint.Substring(2));
            }

            // Less than
            if (constraint.StartsWith("<"))
            {
                return version < SemanticVersion.Parse(constraint.Substring(1));
            }

            // Range (e.g., "1.0.0 - 2.0.0")
            if (constraint.Contains("-"))
            {
                var parts = constraint.Split('-');
                if (parts.Length == 2)
                {
                    var min = SemanticVersion.Parse(parts[0].Trim());
                    var max = SemanticVersion.Parse(parts[1].Trim());
                    return version >= min && version <= max;
                }
            }

            return true;
        }

        /// <summary>
        /// Find the best matching version from a list
        /// </summary>
        public SemanticVersion FindBestMatch(IEnumerable<SemanticVersion> availableVersions)
        {
            SemanticVersion best = null;
            foreach (var version in availableVersions)
            {
                if (IsSatisfiedBy(version))
                {
                    if (best == null || version > best)
                    {
                        best = version;
                    }
                }
            }
            return best;
        }

        public override string ToString() => RawConstraint;
    }
}
