/**
 * HELIOS v4.0 - Request Validation Examples
 * Real-world usage scenarios for validation module
 * @module feat-validation/examples
 */

const {
  SchemaValidator,
  InputSanitizer,
  TypeChecker,
  CustomValidators
} = require('./implementation');

// ============================================================================
// EXAMPLE 1: JSON Schema Validation for API Requests
// ============================================================================
console.log('=== Example 1: JSON Schema Validation ===\n');

const userRegistrationSchema = {
  type: 'object',
  properties: {
    username: {
      type: 'string',
      minLength: 3,
      maxLength: 20,
      pattern: '^[a-zA-Z0-9_]+$'
    },
    email: {
      type: 'string',
      pattern: '^[^@]+@[^@]+\\.[^@]+$'
    },
    age: {
      type: 'integer',
      minimum: 18,
      maximum: 120
    },
    interests: {
      type: 'array',
      items: { type: 'string' },
      maxItems: 5
    }
  },
  required: ['username', 'email', 'age'],
  additionalProperties: false
};

const userValidator = new SchemaValidator(userRegistrationSchema);

console.log('Valid User:');
const validUser = {
  username: 'john_doe',
  email: 'john@example.com',
  age: 28,
  interests: ['coding', 'music']
};
let result = userValidator.validate(validUser);
console.log('Result:', { valid: result.valid, errors: result.errors });

console.log('\nInvalid User (missing email):');
const invalidUser = {
  username: 'jane_doe',
  age: 25
};
result = userValidator.validate(invalidUser);
console.log('Result:', {
  valid: result.valid,
  errorCount: result.errorCount,
  errors: result.errors.map((e) => ({ path: e.path, type: e.type }))
});
console.log();

// ============================================================================
// EXAMPLE 2: XSS Prevention with Input Sanitization
// ============================================================================
console.log('=== Example 2: XSS Prevention ===\n');

class UserCommentProcessor {
  processComment(rawComment) {
    // Remove XSS threats
    let sanitized = InputSanitizer.sanitizeXSS(rawComment);
    
    // Escape HTML
    sanitized = InputSanitizer.escapeHTML(sanitized);
    
    // Normalize whitespace
    sanitized = sanitized.trim();
    
    return {
      original: rawComment,
      sanitized,
      isSafe: sanitized === InputSanitizer.escapeHTML(rawComment.trim())
    };
  }
}

const processor = new UserCommentProcessor();

console.log('Attack 1 (Script Tag):');
let dangerous = 'Great post! <script>stealCookies()</script>';
let processed = processor.processComment(dangerous);
console.log('Original:', dangerous);
console.log('Sanitized:', processed.sanitized);
console.log('Safe:', processed.isSafe);

console.log('\nAttack 2 (Event Handler):');
dangerous = '<img src=x onerror="alert(\'XSS\')">';
processed = processor.processComment(dangerous);
console.log('Original:', dangerous);
console.log('Sanitized:', processed.sanitized);
console.log();

// ============================================================================
// EXAMPLE 3: Type Checking and Coercion
// ============================================================================
console.log('=== Example 3: Type Checking and Coercion ===\n');

class QueryParameterHandler {
  /**
   * Parse and validate query parameters
   */
  parseParams(params) {
    const parsed = {};
    const errors = [];

    // Define expected types
    const expectedTypes = {
      page: 'integer',
      limit: 'integer',
      sort: 'string',
      active: 'boolean'
    };

    for (const [key, value] of Object.entries(params)) {
      if (!(key in expectedTypes)) {
        errors.push({ key, error: 'Unknown parameter' });
        continue;
      }

      const expectedType = expectedTypes[key];
      
      // Check if type matches
      if (TypeChecker.isType(value, expectedType)) {
        parsed[key] = value;
      } else {
        // Try to coerce
        try {
          parsed[key] = TypeChecker.coerce(value, expectedType);
        } catch (error) {
          errors.push({
            key,
            error: `Invalid ${expectedType}: ${value}`
          });
        }
      }
    }

    return { parsed, errors };
  }
}

const handler = new QueryParameterHandler();

console.log('Query Parameters:');
const params = {
  page: '1',
  limit: '20',
  sort: 'name',
  active: 'true'
};

const { parsed, errors } = handler.parseParams(params);
console.log('Parsed:', parsed);
console.log('Errors:', errors.length === 0 ? 'None' : errors);
console.log();

// ============================================================================
// EXAMPLE 4: Email and Contact Validation
// ============================================================================
console.log('=== Example 4: Contact Information Validation ===\n');

class ContactValidator {
  validateEmail(email) {
    const valid = TypeChecker.isEmail(email);
    return { email, valid };
  }

  validatePhone(phone) {
    const valid = TypeChecker.isPhoneNumber(phone);
    return { phone, valid };
  }

  validateURL(url) {
    const valid = TypeChecker.isURL(url);
    return { url, valid };
  }

  validateContact(contact) {
    return {
      email: this.validateEmail(contact.email),
      phone: this.validatePhone(contact.phone),
      website: contact.website ? this.validateURL(contact.website) : null
    };
  }
}

const contactValidator = new ContactValidator();

console.log('Email validation:');
console.log(contactValidator.validateEmail('user@example.com'));
console.log(contactValidator.validateEmail('invalid-email'));

console.log('\nPhone validation:');
console.log(contactValidator.validatePhone('+1-555-123-4567'));
console.log(contactValidator.validatePhone('555-1234'));

console.log('\nURL validation:');
console.log(contactValidator.validateURL('https://example.com'));
console.log(contactValidator.validateURL('not a url'));
console.log();

// ============================================================================
// EXAMPLE 5: Custom Validators for Business Rules
// ============================================================================
console.log('=== Example 5: Custom Business Rules ===\n');

class PasswordValidator {
  constructor() {
    this.validators = CustomValidators.createDefault();
    
    // Add custom business rules
    this.validators.register('noCommonPasswords', (pwd) => {
      const common = ['password', '123456', 'admin', 'letmein'];
      return !common.includes(pwd.toLowerCase());
    });

    this.validators.register('uniqueCharTypes', (pwd) => {
      const hasUpper = /[A-Z]/.test(pwd);
      const hasLower = /[a-z]/.test(pwd);
      const hasNumber = /[0-9]/.test(pwd);
      const hasSpecial = /[!@#$%^&*]/.test(pwd);
      return [hasUpper, hasLower, hasNumber, hasSpecial].filter(Boolean).length >= 3;
    });
  }

  validate(password) {
    const allRules = ['strongPassword', 'noCommonPasswords', 'uniqueCharTypes'];
    const result = this.validators.validateAll(allRules, password);

    return {
      password: '*'.repeat(password.length),
      valid: result.valid,
      feedback: result.valid ? 'Password is strong' : `Validation failed: ${result.errors.join(', ')}`
    };
  }
}

const pwdValidator = new PasswordValidator();

console.log('Password 1 (weak):');
console.log(pwdValidator.validate('password'));

console.log('\nPassword 2 (strong):');
console.log(pwdValidator.validate('MySecureP@ss123'));
console.log();

// ============================================================================
// EXAMPLE 6: SQL Injection Prevention
// ============================================================================
console.log('=== Example 6: SQL Injection Prevention ===\n');

class DatabaseQueryBuilder {
  /**
   * Build safe SQL query
   */
  buildUserQuery(userInput) {
    // Sanitize for SQL injection
    const sanitized = InputSanitizer.sanitizeSQLInjection(userInput);

    return {
      original: userInput,
      sanitized,
      query: `SELECT * FROM users WHERE username = '${sanitized}'`,
      safe: sanitized !== userInput
    };
  }
}

const queryBuilder = new DatabaseQueryBuilder();

console.log('Attack attempt:');
const attack = "admin'; DROP TABLE users; --";
const query = queryBuilder.buildUserQuery(attack);
console.log('Original:', attack);
console.log('Sanitized:', query.sanitized);
console.log('Safe Query:', query.query);
console.log();

// ============================================================================
// EXAMPLE 7: Complex Form Validation
// ============================================================================
console.log('=== Example 7: Complex Form Validation ===\n');

const productFormSchema = {
  type: 'object',
  properties: {
    name: { type: 'string', minLength: 3, maxLength: 100 },
    description: { type: 'string', minLength: 10, maxLength: 1000 },
    price: { type: 'number', minimum: 0.01, maximum: 999999 },
    quantity: { type: 'integer', minimum: 0, maximum: 10000 },
    categories: {
      type: 'array',
      items: { type: 'string' },
      minItems: 1,
      maxItems: 5
    },
    tags: {
      type: 'array',
      items: { type: 'string' }
    }
  },
  required: ['name', 'description', 'price', 'quantity', 'categories'],
  additionalProperties: false
};

const productValidator = new SchemaValidator(productFormSchema);

const validProduct = {
  name: 'Premium Widget',
  description: 'High-quality widget for advanced users',
  price: 99.99,
  quantity: 500,
  categories: ['Electronics', 'Tools'],
  tags: ['premium', 'bestseller']
};

const invalidProduct = {
  name: 'W', // Too short
  price: -10, // Negative
  categories: [] // Empty array
};

console.log('Valid Product:');
result = productValidator.validate(validProduct);
console.log('Valid:', result.valid);

console.log('\nInvalid Product:');
result = productValidator.validate(invalidProduct);
console.log('Valid:', result.valid);
console.log('Errors:', result.errors.length, 'error(s)');
console.log('Sample errors:', result.errors.slice(0, 2).map((e) => ({ path: e.path, type: e.type })));
console.log();

// ============================================================================
// EXAMPLE 8: UUID Validation
// ============================================================================
console.log('=== Example 8: UUID and ID Validation ===\n');

class IDValidator {
  validateUUID(id) {
    return TypeChecker.isUUID(id);
  }

  validateNumericID(id) {
    return TypeChecker.isType(id, 'integer') && id > 0;
  }

  validateResource(resource) {
    return {
      id: resource.id,
      isValid: this.validateUUID(resource.id),
      type: 'UUID'
    };
  }
}

const idValidator = new IDValidator();

console.log('UUID Validation:');
console.log(idValidator.validateResource({ id: '550e8400-e29b-41d4-a716-446655440000' }));
console.log(idValidator.validateResource({ id: 'not-a-uuid' }));
console.log();

console.log('All examples completed successfully!\n');
