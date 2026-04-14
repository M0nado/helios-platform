// C:\helios-v4\telemetry\comprehensive-request-logger.js

const crypto = require('crypto');
const sqlite3 = require('sqlite3').verbose();
const path = require('path');

/**
 * Comprehensive Request Logger - Tracks every aspect of request execution
 * Captures: code execution flow, variables, functions, performance, errors
 */
class ComprehensiveRequestLogger {
  constructor(dbPath = path.join(__dirname, '../metrics.db')) {
    this.dbPath = dbPath;
    this.db = null;
    this.activeRequests = new Map();
    this.executionStack = [];
  }

  initialize() {
    return new Promise((resolve, reject) => {
      this.db = new sqlite3.Database(this.dbPath, (err) => {
        if (err) reject(err);
        else resolve();
      });
    });
  }

  /**
   * Start logging a new request
   */
  startRequest(method, endpoint, userId = null) {
    const requestId = crypto.randomUUID();
    const timestamp = new Date().toISOString();

    const request = {
      requestId,
      method,
      endpoint,
      userId,
      startTime: Date.now(),
      timestamp,
      trace: [],
      variables: new Map(),
      functions: [],
      errors: [],
      metrics: {},
      cacheOps: [],
      dbQueries: [],
      dependencies: [],
    };

    this.activeRequests.set(requestId, request);
    this.executionStack.push(requestId);

    // Log to database
    const stmt = this.db.prepare(`
      INSERT INTO request_log (request_id, method, endpoint, user_id, timestamp)
      VALUES (?, ?, ?, ?, ?)
    `);
    stmt.run(requestId, method, endpoint, userId, timestamp);
    stmt.finalize();

    return requestId;
  }

  /**
   * Track function execution
   */
  trackFunctionCall(requestId, functionName, parameters = {}) {
    const request = this.activeRequests.get(requestId);
    if (!request) return;

    const executionOrder = request.functions.length;
    const timestamp = new Date().toISOString();

    request.functions.push({
      name: functionName,
      parameters,
      executionOrder,
      startTime: Date.now(),
      returnValue: null,
      success: false,
    });

    // Log function trace
    const stmt = this.db.prepare(`
      INSERT INTO code_execution_trace (request_id, function_name, execution_order, timestamp)
      VALUES (?, ?, ?, ?)
    `);
    stmt.run(requestId, functionName, executionOrder, timestamp);
    stmt.finalize();

    // Log parameters
    for (const [paramName, paramValue] of Object.entries(parameters)) {
      const paramStmt = this.db.prepare(`
        INSERT INTO function_parameters (request_id, function_name, parameter_name, parameter_value, parameter_type, timestamp)
        VALUES (?, ?, ?, ?, ?, ?)
      `);
      const paramType = typeof paramValue;
      const paramValueStr = JSON.stringify(paramValue).substring(0, 1000);
      paramStmt.run(requestId, functionName, paramName, paramValueStr, paramType, timestamp);
      paramStmt.finalize();
    }
  }

  /**
   * Track function return value
   */
  trackFunctionReturn(requestId, functionName, returnValue, success = true) {
    const request = this.activeRequests.get(requestId);
    if (!request) return;

    const func = request.functions[request.functions.length - 1];
    if (func && func.name === functionName) {
      func.returnValue = returnValue;
      func.success = success;
      func.duration = Date.now() - func.startTime;
    }

    const timestamp = new Date().toISOString();
    const returnStr = JSON.stringify(returnValue).substring(0, 1000);
    const returnType = typeof returnValue;

    const stmt = this.db.prepare(`
      INSERT INTO function_return_values (request_id, function_name, return_value, return_type, success, timestamp)
      VALUES (?, ?, ?, ?, ?, ?)
    `);
    stmt.run(requestId, functionName, returnStr, returnType, success, timestamp);
    stmt.finalize();
  }

  /**
   * Track variable state changes
   */
  trackVariableChange(requestId, functionContext, variableName, oldValue, newValue) {
    const request = this.activeRequests.get(requestId);
    if (!request) return;

    request.variables.set(variableName, {
      old: oldValue,
      new: newValue,
      function: functionContext,
      timestamp: Date.now(),
    });

    const timestamp = new Date().toISOString();
    const oldStr = JSON.stringify(oldValue).substring(0, 500);
    const newStr = JSON.stringify(newValue).substring(0, 500);
    const dataType = typeof newValue;

    const stmt = this.db.prepare(`
      INSERT INTO variable_state (request_id, function_context, variable_name, old_value, new_value, data_type, timestamp)
      VALUES (?, ?, ?, ?, ?, ?, ?)
    `);
    stmt.run(requestId, functionContext, variableName, oldStr, newStr, dataType, timestamp);
    stmt.finalize();
  }

  /**
   * Track code branch execution
   */
  trackBranchExecution(requestId, branchType, condition, result, lineNumber) {
    const request = this.activeRequests.get(requestId);
    if (!request) return;

    const timestamp = new Date().toISOString();

    const stmt = this.db.prepare(`
      INSERT INTO code_branch_tracking (request_id, branch_type, condition, result, line_number, timestamp)
      VALUES (?, ?, ?, ?, ?, ?)
    `);
    stmt.run(requestId, branchType, condition, result, lineNumber, timestamp);
    stmt.finalize();
  }

  /**
   * Track custom metrics
   */
  trackMetric(requestId, metricName, metricValue, metricUnit = '') {
    const request = this.activeRequests.get(requestId);
    if (!request) return;

    request.metrics[metricName] = metricValue;

    const timestamp = new Date().toISOString();

    const stmt = this.db.prepare(`
      INSERT INTO performance_metrics (request_id, metric_name, metric_value, metric_unit, timestamp)
      VALUES (?, ?, ?, ?, ?)
    `);
    stmt.run(requestId, metricName, metricValue, metricUnit, timestamp);
    stmt.finalize();
  }

  /**
   * Track errors and exceptions
   */
  trackError(requestId, errorType, errorMessage, stackTrace = '', severity = 'error', lineNumber = null) {
    const request = this.activeRequests.get(requestId);
    if (!request) return;

    request.errors.push({
      type: errorType,
      message: errorMessage,
      stack: stackTrace,
      severity,
      timestamp: Date.now(),
    });

    const timestamp = new Date().toISOString();

    const stmt = this.db.prepare(`
      INSERT INTO error_tracking (request_id, error_type, error_message, stack_trace, severity, line_number, timestamp)
      VALUES (?, ?, ?, ?, ?, ?, ?)
    `);
    stmt.run(requestId, errorType, errorMessage, stackTrace.substring(0, 2000), severity, lineNumber, timestamp);
    stmt.finalize();
  }

  /**
   * Track cache operations
   */
  trackCacheOperation(requestId, cacheKey, operationType, hit = null, valueSize = null) {
    const request = this.activeRequests.get(requestId);
    if (!request) return;

    request.cacheOps.push({
      key: cacheKey,
      operation: operationType,
      hit,
      size: valueSize,
      timestamp: Date.now(),
    });

    const timestamp = new Date().toISOString();

    const stmt = this.db.prepare(`
      INSERT INTO cache_operations (request_id, cache_key, operation_type, hit, value_size_bytes, timestamp)
      VALUES (?, ?, ?, ?, ?, ?)
    `);
    stmt.run(requestId, cacheKey, operationType, hit, valueSize, timestamp);
    stmt.finalize();
  }

  /**
   * Track database queries
   */
  trackDatabaseQuery(requestId, queryText, durationMs, rowsAffected = 0, queryType = 'select') {
    const request = this.activeRequests.get(requestId);
    if (!request) return;

    request.dbQueries.push({
      query: queryText,
      duration: durationMs,
      rows: rowsAffected,
      type: queryType,
      timestamp: Date.now(),
    });

    const timestamp = new Date().toISOString();

    const stmt = this.db.prepare(`
      INSERT INTO database_queries (request_id, query_text, duration_ms, rows_affected, query_type, timestamp)
      VALUES (?, ?, ?, ?, ?, ?)
    `);
    stmt.run(requestId, queryText.substring(0, 2000), durationMs, rowsAffected, queryType, timestamp);
    stmt.finalize();
  }

  /**
   * Track request dependencies
   */
  trackDependency(requestId, dependsOnRequestId, dependencyType = 'blocks') {
    const request = this.activeRequests.get(requestId);
    if (!request) return;

    request.dependencies.push({
      on: dependsOnRequestId,
      type: dependencyType,
    });

    const stmt = this.db.prepare(`
      INSERT INTO request_dependencies (request_id, depends_on_request_id, dependency_type)
      VALUES (?, ?, ?)
    `);
    stmt.run(requestId, dependsOnRequestId, dependencyType);
    stmt.finalize();
  }

  /**
   * Complete logging for a request
   */
  endRequest(requestId, statusCode = 200, responseSize = 0, errorMessage = null) {
    const request = this.activeRequests.get(requestId);
    if (!request) return;

    const duration = Date.now() - request.startTime;

    // Update request_log with completion status
    const stmt = this.db.prepare(`
      UPDATE request_log 
      SET status_code = ?, duration_ms = ?, response_size_bytes = ?, error_message = ?
      WHERE request_id = ?
    `);
    stmt.run(statusCode, duration, responseSize, errorMessage, requestId);
    stmt.finalize();

    this.activeRequests.delete(requestId);
    this.executionStack.pop();

    return {
      requestId,
      duration,
      statusCode,
      errorCount: request.errors.length,
      cacheOps: request.cacheOps.length,
      dbQueries: request.dbQueries.length,
      functionsExecuted: request.functions.length,
    };
  }

  /**
   * Get comprehensive request trace
   */
  getRequestTrace(requestId) {
    return new Promise((resolve, reject) => {
      const traces = {};

      // Get request info
      this.db.get('SELECT * FROM request_log WHERE request_id = ?', [requestId], (err, row) => {
        if (err) reject(err);
        traces.request = row;

        // Get execution trace
        this.db.all('SELECT * FROM code_execution_trace WHERE request_id = ? ORDER BY execution_order', [requestId], (err, rows) => {
          if (err) reject(err);
          traces.execution = rows;

          // Get variables
          this.db.all('SELECT * FROM variable_state WHERE request_id = ?', [requestId], (err, rows) => {
            if (err) reject(err);
            traces.variables = rows;

            // Get errors
            this.db.all('SELECT * FROM error_tracking WHERE request_id = ?', [requestId], (err, rows) => {
              if (err) reject(err);
              traces.errors = rows;

              // Get cache ops
              this.db.all('SELECT * FROM cache_operations WHERE request_id = ?', [requestId], (err, rows) => {
                if (err) reject(err);
                traces.cache = rows;

                // Get DB queries
                this.db.all('SELECT * FROM database_queries WHERE request_id = ?', [requestId], (err, rows) => {
                  if (err) reject(err);
                  traces.queries = rows;

                  resolve(traces);
                });
              });
            });
          });
        });
      });
    });
  }

  /**
   * Generate experiment metrics summary
   */
  getExperimentMetrics(experimentName) {
    return new Promise((resolve, reject) => {
      const query = `
        SELECT 
          COUNT(*) as total_requests,
          SUM(CASE WHEN status_code = 200 THEN 1 ELSE 0 END) as successful_requests,
          SUM(CASE WHEN status_code != 200 THEN 1 ELSE 0 END) as failed_requests,
          AVG(duration_ms) as avg_duration_ms,
          MIN(duration_ms) as min_duration_ms,
          MAX(duration_ms) as max_duration_ms,
          AVG(response_size_bytes) as avg_response_size,
          COUNT(DISTINCT user_id) as unique_users
        FROM request_log
        WHERE timestamp >= datetime('now', '-1 hour')
      `;

      this.db.get(query, (err, row) => {
        if (err) reject(err);
        else {
          const successRate = row.total_requests > 0 
            ? (row.successful_requests / row.total_requests * 100).toFixed(2)
            : 0;

          const stmt = this.db.prepare(`
            INSERT INTO experiment_metadata (experiment_name, total_requests, total_errors, success_rate)
            VALUES (?, ?, ?, ?)
          `);
          stmt.run(experimentName, row.total_requests, row.failed_requests, successRate);
          stmt.finalize();

          resolve({
            ...row,
            successRate: `${successRate}%`,
          });
        }
      });
    });
  }

  /**
   * Get hot spots (most executed functions, slowest operations, etc.)
   */
  getHotSpots(limit = 10) {
    return new Promise((resolve, reject) => {
      const hotSpots = {};

      // Most executed functions
      this.db.all(`
        SELECT function_name, COUNT(*) as execution_count, AVG(duration_ms) as avg_duration
        FROM code_execution_trace
        GROUP BY function_name
        ORDER BY execution_count DESC
        LIMIT ?
      `, [limit], (err, rows) => {
        if (err) reject(err);
        hotSpots.mostExecuted = rows;

        // Slowest operations
        this.db.all(`
          SELECT function_name, MAX(duration_ms) as max_duration, AVG(duration_ms) as avg_duration
          FROM code_execution_trace
          GROUP BY function_name
          ORDER BY avg_duration DESC
          LIMIT ?
        `, [limit], (err, rows) => {
          if (err) reject(err);
          hotSpots.slowest = rows;

          // Most common errors
          this.db.all(`
            SELECT error_type, COUNT(*) as error_count
            FROM error_tracking
            GROUP BY error_type
            ORDER BY error_count DESC
            LIMIT ?
          `, [limit], (err, rows) => {
            if (err) reject(err);
            hotSpots.commonErrors = rows;

            resolve(hotSpots);
          });
        });
      });
    });
  }

  /**
   * Close database connection
   */
  close() {
    return new Promise((resolve, reject) => {
      if (this.db) {
        this.db.close((err) => {
          if (err) reject(err);
          else resolve();
        });
      } else {
        resolve();
      }
    });
  }
}

module.exports = { ComprehensiveRequestLogger };
