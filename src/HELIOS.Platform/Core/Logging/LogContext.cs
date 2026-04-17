using Serilog.Context;
using System;
using System.Collections.Generic;

namespace HELIOS.Platform.Core.Logging
{
    /// <summary>
    /// Provides structured logging context for enriching log entries
    /// </summary>
    public class LogContext : IDisposable
    {
        private readonly List<IDisposable> _disposables = new();
        private bool _disposed = false;

        /// <summary>
        /// Push a property to the log context
        /// </summary>
        public LogContext WithProperty(string name, object value)
        {
            var disposable = LogicalCallContext.PushProperty(name, value);
            _disposables.Add(disposable);
            return this;
        }

        /// <summary>
        /// Push a user ID to the context
        /// </summary>
        public LogContext WithUserId(string userId)
        {
            return WithProperty("UserId", userId);
        }

        /// <summary>
        /// Push a request ID to the context
        /// </summary>
        public LogContext WithRequestId(string requestId)
        {
            return WithProperty("RequestId", requestId);
        }

        /// <summary>
        /// Push a correlation ID to the context
        /// </summary>
        public LogContext WithCorrelationId(string correlationId)
        {
            return WithProperty("CorrelationId", correlationId);
        }

        /// <summary>
        /// Push an operation name to the context
        /// </summary>
        public LogContext WithOperation(string operationName)
        {
            return WithProperty("Operation", operationName);
        }

        /// <summary>
        /// Push module/component name to the context
        /// </summary>
        public LogContext WithModule(string moduleName)
        {
            return WithProperty("Module", moduleName);
        }

        /// <summary>
        /// Push custom metadata to the context
        /// </summary>
        public LogContext WithMetadata(string key, object value)
        {
            return WithProperty($"Metadata_{key}", value);
        }

        /// <summary>
        /// Push performance timing information
        /// </summary>
        public LogContext WithTiming(string operationName, long elapsedMilliseconds)
        {
            return WithProperty($"Timing_{operationName}_Ms", elapsedMilliseconds);
        }

        /// <summary>
        /// Push performance timing information with start time
        /// </summary>
        public LogContext WithTimingFromStart(string operationName, DateTime startTime)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            return WithTiming(operationName, (long)elapsed);
        }

        /// <summary>
        /// Create a scoped log context
        /// </summary>
        public static LogContext CreateScope()
        {
            return new LogContext();
        }

        /// <summary>
        /// Dispose and pop all properties from context
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            foreach (var disposable in _disposables)
            {
                disposable?.Dispose();
            }
            _disposables.Clear();
            _disposed = true;
        }
    }

    /// <summary>
    /// Extension methods for LogicalCallContext
    /// </summary>
    public static class LogicalCallContextExtensions
    {
        /// <summary>
        /// Push a property to the logical call context
        /// </summary>
        public static IDisposable PushProperty(string name, object value)
        {
            return LogicalCallContext.Push(name, value);
        }

        /// <summary>
        /// Push a property to the logical call context with automatic disposal
        /// </summary>
        public static IDisposable PushPropertyScoped(string name, object value, out LogContext context)
        {
            var ctx = new LogContext().WithProperty(name, value);
            context = ctx;
            return ctx;
        }
    }
}
