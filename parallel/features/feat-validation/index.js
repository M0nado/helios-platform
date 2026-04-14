/**
 * HELIOS v4.0 - Request Validation Module Public API
 * @module feat-validation
 */

const {
  SchemaValidator,
  InputSanitizer,
  TypeChecker,
  CustomValidators
} = require('./implementation');

/**
 * Create a schema validator
 * @param {Object} schema - JSON schema
 * @returns {SchemaValidator} Validator instance
 */
const createSchemaValidator = (schema) => {
  return new SchemaValidator(schema);
};

/**
 * Sanitize input for XSS
 * @param {string} input - Input to sanitize
 * @returns {string} Sanitized input
 */
const sanitizeXSS = (input) => {
  return InputSanitizer.sanitizeXSS(input);
};

/**
 * Sanitize input for SQL injection
 * @param {string} input - Input to sanitize
 * @returns {string} Sanitized input
 */
const sanitizeSQLInjection = (input) => {
  return InputSanitizer.sanitizeSQLInjection(input);
};

/**
 * Escape HTML entities
 * @param {string} input - Input to escape
 * @returns {string} Escaped input
 */
const escapeHTML = (input) => {
  return InputSanitizer.escapeHTML(input);
};

/**
 * Create type checker utility
 * @returns {Object} Type checking utilities
 */
const createTypeChecker = () => {
  return TypeChecker;
};

/**
 * Create custom validators registry
 * @param {Object} [validators] - Initial validators
 * @returns {CustomValidators} Validators instance
 */
const createCustomValidators = (validators) => {
  return new CustomValidators(validators);
};

/**
 * Create default validators with common rules
 * @returns {CustomValidators} Pre-configured validators
 */
const createDefaultValidators = () => {
  return CustomValidators.createDefault();
};

/**
 * Express-like middleware for JSON schema validation
 * @param {SchemaValidator} validator - Validator instance
 * @returns {Function} Middleware function
 */
const createValidationMiddleware = (validator) => {
  return (req, res, next) => {
    const result = validator.validate(req.body || {});
    
    if (!result.valid) {
      return res.status(400).json({
        error: 'Validation failed',
        errors: result.errors
      });
    }
    
    next();
  };
};

/**
 * Express-like middleware for input sanitization
 * @param {Array<string>} [fields=['body']] - Fields to sanitize
 * @returns {Function} Middleware function
 */
const createSanitizationMiddleware = (fields = ['body']) => {
  return (req, res, next) => {
    for (const field of fields) {
      if (req[field] && typeof req[field] === 'object') {
        req[field] = sanitizeData(req[field]);
      }
    }
    next();
  };
};

/**
 * Recursively sanitize data structure
 * @private
 */
const sanitizeData = (data) => {
  if (typeof data === 'string') {
    return InputSanitizer.sanitize(data);
  } else if (Array.isArray(data)) {
    return data.map(sanitizeData);
  } else if (typeof data === 'object' && data !== null) {
    const sanitized = {};
    for (const [key, value] of Object.entries(data)) {
      sanitized[key] = sanitizeData(value);
    }
    return sanitized;
  }
  return data;
};

module.exports = {
  SchemaValidator,
  InputSanitizer,
  TypeChecker,
  CustomValidators,
  createSchemaValidator,
  sanitizeXSS,
  sanitizeSQLInjection,
  escapeHTML,
  createTypeChecker,
  createCustomValidators,
  createDefaultValidators,
  createValidationMiddleware,
  createSanitizationMiddleware
};
