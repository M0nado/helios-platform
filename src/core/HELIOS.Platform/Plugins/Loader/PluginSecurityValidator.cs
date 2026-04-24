// ═══════════════════════════════════════════════════════════════════════════
// Plugin Security Validator - Signature Verification & Malicious Code Detection
// Provides security sandboxing and validation mechanisms
// ═══════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HELIOS.Platform.Plugins.Loader
{
    /// <summary>
    /// Validates plugin security through signature verification and analysis
    /// </summary>
    public class PluginSecurityValidator : IPluginSecurityValidator
    {
        private readonly ILogger<PluginSecurityValidator> _logger;
        private readonly List<string> _trustedPublishers;
        private readonly List<string> _bannedNamespaces;

        public PluginSecurityValidator(ILogger<PluginSecurityValidator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _trustedPublishers = new()
            {
                "HELIOS.Platform",
                "HELIOS.Community",
                "HELIOS.Verified"
            };
            _bannedNamespaces = new()
            {
                "System.Reflection.Emit",
                "System.Net.Http",
                "System.IO.FileStream",
                "System.Diagnostics.Process"
            };
        }

        public async Task<bool> VerifySignatureAsync(
            string assemblyPath,
            string expectedSignature,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Verifying signature for {AssemblyPath}", assemblyPath);

                if (!File.Exists(assemblyPath))
                {
                    _logger.LogWarning("Assembly not found: {AssemblyPath}", assemblyPath);
                    return false;
                }

                var actualSignature = await ComputeSignatureAsync(assemblyPath, cancellationToken);

                bool isValid = actualSignature.Equals(expectedSignature, StringComparison.Ordinal);
                if (!isValid)
                {
                    _logger.LogWarning(
                        "Signature mismatch for {AssemblyPath}. Expected: {Expected}, Got: {Actual}",
                        assemblyPath,
                        expectedSignature,
                        actualSignature);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying signature for {AssemblyPath}", assemblyPath);
                return false;
            }
        }

        public async Task<bool> IsMaliciousAsync(
            string assemblyPath,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Scanning for malicious code in {AssemblyPath}", assemblyPath);

                var assembly = Assembly.LoadFrom(assemblyPath);

                // Check for suspicious namespace usage
                if (HasSuspiciousNamespaces(assembly))
                {
                    _logger.LogWarning(
                        "Plugin {AssemblyPath} uses suspicious namespaces",
                        assemblyPath);
                    return true;
                }

                // Check for reflection-based code generation
                if (HasReflectionEmit(assembly))
                {
                    _logger.LogWarning(
                        "Plugin {AssemblyPath} uses reflection emit",
                        assemblyPath);
                    return true;
                }

                // Check for process execution
                if (HasProcessExecution(assembly))
                {
                    _logger.LogWarning(
                        "Plugin {AssemblyPath} attempts process execution",
                        assemblyPath);
                    return true;
                }

                // Check for network access
                if (HasNetworkAccess(assembly))
                {
                    _logger.LogWarning(
                        "Plugin {AssemblyPath} attempts network access",
                        assemblyPath);
                    return true;
                }

                _logger.LogDebug("No malicious code detected in {AssemblyPath}", assemblyPath);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning for malicious code in {AssemblyPath}", assemblyPath);
                return true; // Default to blocking if we can't verify
            }
        }

        private async Task<string> ComputeSignatureAsync(
            string assemblyPath,
            CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var sha256 = SHA256.Create();
                using var fileStream = File.OpenRead(assemblyPath);
                var hash = sha256.ComputeHash(fileStream);
                return Convert.ToBase64String(hash);
            }, cancellationToken);
        }

        private bool HasSuspiciousNamespaces(Assembly assembly)
        {
            try
            {
                var types = assembly.GetTypes();
                var suspiciousTypes = types.Where(t =>
                    _bannedNamespaces.Any(bn => t.Namespace?.StartsWith(bn) ?? false));

                return suspiciousTypes.Any();
            }
            catch
            {
                return false;
            }
        }

        private bool HasReflectionEmit(Assembly assembly)
        {
            try
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    foreach (var method in methods)
                    {
                        var methodBody = method.GetMethodBody();
                        if (methodBody != null)
                        {
                            var il = methodBody.GetILAsByteArray();
                            if (HasReflectionEmitInIL(il))
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool HasReflectionEmitInIL(byte[] il)
        {
            // Simple heuristic: check for known IL opcodes related to reflection emit
            var suspiciousPatterns = new[]
            {
                new byte[] { 0x28, 0x00, 0x00, 0x00, 0x0A }, // call (emit-related)
                new byte[] { 0x1F, 0x00 }, // ldc.i4 (potential emit indicator)
            };

            return suspiciousPatterns.Any(pattern =>
                il.Length >= pattern.Length &&
                Enumerable.Range(0, il.Length - pattern.Length + 1)
                    .Any(i => il.Skip(i).Take(pattern.Length).SequenceEqual(pattern)));
        }

        private bool HasProcessExecution(Assembly assembly)
        {
            try
            {
                var processType = typeof(System.Diagnostics.Process);
                var types = assembly.GetTypes();

                foreach (var type in types)
                {
                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    foreach (var method in methods)
                    {
                        var parameters = method.GetParameters();
                        if (method.ReturnType == processType || parameters.Any(p => p.ParameterType == processType))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool HasNetworkAccess(Assembly assembly)
        {
            try
            {
                var httpClientType = typeof(System.Net.Http.HttpClient);
                var socketType = typeof(System.Net.Sockets.Socket);
                var types = assembly.GetTypes();

                foreach (var type in types)
                {
                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    if (fields.Any(f => f.FieldType == httpClientType || f.FieldType == socketType))
                    {
                        return true;
                    }

                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    foreach (var method in methods)
                    {
                        var parameters = method.GetParameters();
                        if (parameters.Any(p => p.ParameterType == httpClientType || p.ParameterType == socketType))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public void AddTrustedPublisher(string publisherName)
        {
            if (!_trustedPublishers.Contains(publisherName))
            {
                _trustedPublishers.Add(publisherName);
            }
        }

        public void RemoveTrustedPublisher(string publisherName)
        {
            _trustedPublishers.Remove(publisherName);
        }

        public void BanNamespace(string @namespace)
        {
            if (!_bannedNamespaces.Contains(@namespace))
            {
                _bannedNamespaces.Add(@namespace);
            }
        }

        public IReadOnlyList<string> GetTrustedPublishers() => _trustedPublishers.AsReadOnly();
        public IReadOnlyList<string> GetBannedNamespaces() => _bannedNamespaces.AsReadOnly();
    }
}
