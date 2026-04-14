/**
 * HELIOS v4.0 - Request Validation Module
 * Production-ready validation with schema, sanitization, and type checking
 * @module feat-validation/implementation
 */

/**
 * JSON Schema Validator
 * Validates data against JSON schema with detailed error reporting
 * @class SchemaValidator
 */
class SchemaValidator {
  /**
   * Creates a new SchemaValidator
   * @param {Object} schema - JSON schema object
   * @throws {Error} If schema is invalid
   */
  constructor(schema) {
    if (typeof schema !== 'object' || schema === null) {
      throw new Error('Schema must be a valid object');
    }
    this.schema = schema;
    this.errors = [];
  }

  /**
   * Validate data against schema
   * @param {*} data - Data to validate
   * @returns {Object} Validation result with valid flag and errors array
   */
  validate(data) {
    this.errors = [];
    const isValid = this._validateValue(data, this.schema, '');
    
    return {
      valid: isValid,
      errors: this.errors,
      errorCount: this.errors.length,
      data: isValid ? data : null
    };
  }

  /**
   * Validate a value against schema recursively
   * @private
   */
  _validateValue(data, schema, path) {
    // Check type
    if (schema.type) {
      const actualType = Array.isArray(data) ? 'array' : typeof data;
      if (actualType !== schema.type) {
        this.errors.push({
          path: path || 'root',
          message: `Expected type ${schema.type}, got ${actualType}`,
          type: 'TYPE_MISMATCH'
        });
        return false;
      }
    }

    // Validate based on type
    if (schema.type === 'object') {
      return this._validateObject(data, schema, path);
    } else if (schema.type === 'array') {
      return this._validateArray(data, schema, path);
    } else if (schema.type === 'string') {
      return this._validateString(data, schema, path);
    } else if (schema.type === 'number' || schema.type === 'integer') {
      return this._validateNumber(data, schema, path);
    }

    return true;
  }

  /**
   * Validate object properties
   * @private
   */
  _validateObject(data, schema, path) {
    let isValid = true;

    // Check required properties
    if (schema.required) {
      for (const prop of schema.required) {
        if (!(prop in data)) {
          this.errors.push({
            path: path ? `${path}.${prop}` : prop,
            message: `Missing required property: ${prop}`,
            type: 'REQUIRED_FIELD'
          });
          isValid = false;
        }
      }
    }

    // Validate properties
    if (schema.properties) {
      for (const [key, propSchema] of Object.entries(schema.properties)) {
        const newPath = path ? `${path}.${key}` : key;
        if (key in data) {
          if (!this._validateValue(data[key], propSchema, newPath)) {
            isValid = false;
          }
        }
      }
    }

    // Check additional properties not allowed
    if (schema.additionalProperties === false && schema.properties) {
      for (const key of Object.keys(data)) {
        if (!(key in schema.properties)) {
          this.errors.push({
            path: path ? `${path}.${key}` : key,
            message: `Additional property not allowed: ${key}`,
            type: 'ADDITIONAL_PROPERTIES'
          });
          isValid = false;
        }
      }
    }

    return isValid;
  }

  /**
   * Validate array
   * @private
   */
  _validateArray(data, schema, path) {
    let isValid = true;

    if (schema.minItems && data.length < schema.minItems) {
      this.errors.push({
        path: path || 'root',
        message: `Array must have at least ${schema.minItems} items`,
        type: 'MIN_ITEMS'
      });
      isValid = false;
    }

    if (schema.maxItems && data.length > schema.maxItems) {
      this.errors.push({
        path: path || 'root',
        message: `Array must have at most ${schema.maxItems} items`,
        type: 'MAX_ITEMS'
      });
      isValid = false;
    }

    if (schema.items) {
      for (let i = 0; i < data.length; i++) {
        if (!this._validateValue(data[i], schema.items, `${path}[${i}]`)) {
          isValid = false;
        }
      }
    }

    return isValid;
  }

  /**
   * Validate string
   * @private
   */
  _validateString(data, schema, path) {
    let isValid = true;

    if (schema.minLength && data.length < schema.minLength) {
      this.errors.push({
        path: path || 'root',
        message: `String must be at least ${schema.minLength} characters`,
        type: 'MIN_LENGTH'
      });
      isValid = false;
    }

    if (schema.maxLength && data.length > schema.maxLength) {
      this.errors.push({
        path: path || 'root',
        message: `String must be at most ${schema.maxLength} characters`,
        type: 'MAX_LENGTH'
      });
      isValid = false;
    }

    if (schema.pattern) {
      const regex = new RegExp(schema.pattern);
      if (!regex.test(data)) {
        this.errors.push({
          path: path || 'root',
          message: `String does not match pattern: ${schema.pattern}`,
          type: 'PATTERN_MISMATCH'
        });
        isValid = false;
      }
    }

    return isValid;
  }

  /**
   * Validate number
   * @private
   */
  _validateNumber(data, schema, path) {
    let isValid = true;

    if (schema.type === 'integer' && !Number.isInteger(data)) {
      this.errors.push({
        path: path || 'root',
        message: 'Value must be an integer',
        type: 'NOT_INTEGER'
      });
      isValid = false;
    }

    if (schema.minimum !== undefined && data < schema.minimum) {
      this.errors.push({
        path: path || 'root',
        message: `Value must be at least ${schema.minimum}`,
        type: 'MINIMUM'
      });
      isValid = false;
    }

    if (schema.maximum !== undefined && data > schema.maximum) {
      this.errors.push({
        path: path || 'root',
        message: `Value must be at most ${schema.maximum}`,
        type: 'MAXIMUM'
      });
      isValid = false;
    }

    return isValid;
  }

  /**
   * Get schema
   */
  getSchema() {
    return this.schema;
  }
}

/**
 * Input Sanitizer
 * Removes dangerous content from user inputs
 * @class InputSanitizer
 */
class InputSanitizer {
  /**
   * XSS dangerous patterns
   * @private
   */
  static XSS_PATTERNS = [
    /<script[^>]*>[\s\S]*?<\/script>/gi,
    /on\w+\s*=/gi,
    /<iframe[^>]*>/gi,
    /javascript:/gi,
    /<embed[^>]*>/gi,
    /<object[^>]*>/gi
  ];

  /**
   * SQL injection patterns
   * @private
   */
  static SQL_PATTERNS = [
    /(\b(UNION|SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|EXEC|EXECUTE|SCRIPT)\b)/gi,
    /'[^']*'/g,
    /--/g,
    /\/\*/g
  ];

  /**
   * Sanitize input for XSS
   * @param {string} input - Input to sanitize
   * @returns {string} Sanitized input
   */
  static sanitizeXSS(input) {
    if (typeof input !== 'string') return input;
    
    let sanitized = input;
    for (const pattern of InputSanitizer.XSS_PATTERNS) {
      sanitized = sanitized.replace(pattern, '');
    }
    return sanitized;
  }

  /**
   * Sanitize for SQL injection
   * @param {string} input - Input to sanitize
   * @returns {string} Escaped input
   */
  static sanitizeSQLInjection(input) {
    if (typeof input !== 'string') return input;
    
    return input
      .replace(/\\/g, '\\\\')
      .replace(/'/g, "''")
      .replace(/"/g, '\\"')
      .replace(/\0/g, '\\0')
      .replace(/\n/g, '\\n')
      .replace(/\r/g, '\\r')
      .replace(/\x1a/g, '\\Z');
  }

  /**
   * Escape HTML entities
   * @param {string} input - Input to escape
   * @returns {string} Escaped input
   */
  static escapeHTML(input) {
    if (typeof input !== 'string') return input;
    
    const map = {
      '&': '&amp;',
      '<': '&lt;',
      '>': '&gt;',
      '"': '&quot;',
      "'": '&#39;'
    };
    return input.replace(/[&<>"']/g, (char) => map[char]);
  }

  /**
   * Trim and normalize whitespace
   * @param {string} input - Input to normalize
   * @returns {string} Normalized input
   */
  static normalize(input) {
    if (typeof input !== 'string') return input;
    
    return input
      .trim()
      .replace(/\s+/g, ' ')
      .toLowerCase();
  }

  /**
   * Remove null bytes and control characters
   * @param {string} input - Input to clean
   * @returns {string} Cleaned input
   */
  static removeControlChars(input) {
    if (typeof input !== 'string') return input;
    
    return input.replace(/[\x00-\x1F\x7F]/g, '');
  }

  /**
   * Full sanitization pipeline
   * @param {string} input - Input to sanitize
   * @returns {string} Fully sanitized input
   */
  static sanitize(input) {
    if (typeof input !== 'string') return input;
    
    return InputSanitizer
      .removeControlChars(input)
      .split('')
      .filter((char) => {
        const code = char.charCodeAt(0);
        return code >= 32 && code !== 127;
      })
      .join('')
      .trim();
  }
}

/**
 * Type Checker
 * Validates and coerces types with detailed reporting
 * @class TypeChecker
 */
class TypeChecker {
  /**
   * Check if value is of expected type
   * @param {*} value - Value to check
   * @param {string} expectedType - Expected type name
   * @returns {boolean} True if type matches
   */
  static isType(value, expectedType) {
    switch (expectedType.toLowerCase()) {
      case 'string': return typeof value === 'string';
      case 'number': return typeof value === 'number' && !isNaN(value);
      case 'integer': return Number.isInteger(value);
      case 'boolean': return typeof value === 'boolean';
      case 'array': return Array.isArray(value);
      case 'object': return value !== null && typeof value === 'object' && !Array.isArray(value);
      case 'null': return value === null;
      case 'date': return value instanceof Date;
      case 'email': return TypeChecker.isEmail(value);
      case 'url': return TypeChecker.isURL(value);
      case 'uuid': return TypeChecker.isUUID(value);
      default: return false;
    }
  }

  /**
   * Coerce value to type
   * @param {*} value - Value to coerce
   * @param {string} targetType - Target type
   * @returns {*} Coerced value
   * @throws {Error} If coercion not possible
   */
  static coerce(value, targetType) {
    switch (targetType.toLowerCase()) {
      case 'string': return String(value);
      case 'number': {
        const num = Number(value);
        if (isNaN(num)) throw new Error(`Cannot coerce to number: ${value}`);
        return num;
      }
      case 'integer': {
        const num = parseInt(value, 10);
        if (isNaN(num)) throw new Error(`Cannot coerce to integer: ${value}`);
        return num;
      }
      case 'boolean': {
        if (typeof value === 'boolean') return value;
        if (value === 'true' || value === '1' || value === 1) return true;
        if (value === 'false' || value === '0' || value === 0) return false;
        throw new Error(`Cannot coerce to boolean: ${value}`);
      }
      case 'array': {
        if (Array.isArray(value)) return value;
        throw new Error(`Cannot coerce to array: ${value}`);
      }
      case 'object': {
        if (typeof value === 'object' && value !== null && !Array.isArray(value)) return value;
        throw new Error(`Cannot coerce to object: ${value}`);
      }
      default: throw new Error(`Unknown type: ${targetType}`);
    }
  }

  /**
   * Validate email format
   * @param {string} email - Email to validate
   * @returns {boolean} True if valid email
   */
  static isEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return typeof email === 'string' && emailRegex.test(email);
  }

  /**
   * Validate URL format
   * @param {string} url - URL to validate
   * @returns {boolean} True if valid URL
   */
  static isURL(url) {
    try {
      new URL(url);
      return true;
    } catch {
      return false;
    }
  }

  /**
   * Validate UUID format
   * @param {string} uuid - UUID to validate
   * @returns {boolean} True if valid UUID
   */
  static isUUID(uuid) {
    const uuidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
    return typeof uuid === 'string' && uuidRegex.test(uuid);
  }

  /**
   * Validate phone number (basic)
   * @param {string} phone - Phone number to validate
   * @returns {boolean} True if valid format
   */
  static isPhoneNumber(phone) {
    const phoneRegex = /^[+]?[(]?[0-9]{1,4}[)]?[-\s.]?[0-9]{1,4}[-\s.]?[0-9]{1,9}$/;
    return typeof phone === 'string' && phoneRegex.test(phone.replace(/\s/g, ''));
  }

  /**
   * Get JavaScript type name
   * @param {*} value - Value to check
   * @returns {string} Type name
   */
  static getType(value) {
    if (value === null) return 'null';
    if (Array.isArray(value)) return 'array';
    if (value instanceof Date) return 'date';
    return typeof value;
  }
}

/**
 * Custom Validators
 * Extensible validation rules for complex scenarios
 * @class CustomValidators
 */
class CustomValidators {
  /**
   * Creates validator registry
   * @param {Object} [validators={}] - Initial validators
   */
  constructor(validators = {}) {
    this.validators = new Map(Object.entries(validators));
  }

  /**
   * Register custom validator
   * @param {string} name - Validator name
   * @param {Function} fn - Validation function(value) -> boolean
   * @throws {Error} If validator not a function
   */
  register(name, fn) {
    if (typeof fn !== 'function') {
      throw new Error('Validator must be a function');
    }
    this.validators.set(name, fn);
  }

  /**
   * Validate using registered validator
   * @param {string} name - Validator name
   * @param {*} value - Value to validate
   * @returns {Object} Result with valid and error
   */
  validate(name, value) {
    if (!this.validators.has(name)) {
      return {
        valid: false,
        error: `Validator not found: ${name}`
      };
    }

    try {
      const isValid = this.validators.get(name)(value);
      return {
        valid: isValid,
        error: isValid ? null : `Validation failed: ${name}`
      };
    } catch (error) {
      return {
        valid: false,
        error: error.message
      };
    }
  }

  /**
   * Validate value against multiple validators
   * @param {Array<string>} names - Validator names
   * @param {*} value - Value to validate
   * @returns {Object} Combined result
   */
  validateAll(names, value) {
    const results = [];
    let allValid = true;

    for (const name of names) {
      const result = this.validate(name, value);
      results.push(result);
      if (!result.valid) allValid = false;
    }

    return {
      valid: allValid,
      results,
      errors: results.filter((r) => !r.valid).map((r) => r.error)
    };
  }

  /**
   * Get all registered validators
   * @returns {Array<string>} Validator names
   */
  getValidators() {
    return Array.from(this.validators.keys());
  }

  /**
   * Create validators for common scenarios
   * @static
   * @returns {CustomValidators} Pre-populated validators
   */
  static createDefault() {
    const validators = new CustomValidators();

    validators.register('strongPassword', (pwd) => {
      if (typeof pwd !== 'string' || pwd.length < 8) return false;
      return /[A-Z]/.test(pwd) && /[a-z]/.test(pwd) && /[0-9]/.test(pwd) && /[!@#$%^&*]/.test(pwd);
    });

    validators.register('noSpaces', (str) => {
      return typeof str === 'string' && !/\s/.test(str);
    });

    validators.register('alphanumeric', (str) => {
      return typeof str === 'string' && /^[a-zA-Z0-9]+$/.test(str);
    });

    validators.register('positiveNumber', (num) => {
      return typeof num === 'number' && num > 0 && !isNaN(num);
    });

    validators.register('creditCard', (cc) => {
      return typeof cc === 'string' && /^\d{13,19}$/.test(cc.replace(/\s/g, ''));
    });

    validators.register('zipCode', (zip) => {
      return typeof zip === 'string' && /^\d{5}(-\d{4})?$/.test(zip);
    });

    return validators;
  }
}

module.exports = {
  SchemaValidator,
  InputSanitizer,
  TypeChecker,
  CustomValidators
};
