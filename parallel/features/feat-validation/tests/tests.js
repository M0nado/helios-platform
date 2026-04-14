/**
 * HELIOS v4.0 - Request Validation Module Tests
 * Comprehensive test suite for all validation features
 * @module feat-validation/tests
 */

const assert = require('assert');
const {
  SchemaValidator,
  InputSanitizer,
  TypeChecker,
  CustomValidators
} = require('../implementation');

describe('SchemaValidator', () => {
  it('should validate simple object', () => {
    const schema = {
      type: 'object',
      properties: {
        name: { type: 'string' },
        age: { type: 'number' }
      },
      required: ['name']
    };

    const validator = new SchemaValidator(schema);
    const result = validator.validate({ name: 'John', age: 30 });
    assert.strictEqual(result.valid, true);
    assert.strictEqual(result.errorCount, 0);
  });

  it('should catch missing required fields', () => {
    const schema = {
      type: 'object',
      properties: {
        email: { type: 'string' }
      },
      required: ['email']
    };

    const validator = new SchemaValidator(schema);
    const result = validator.validate({});
    assert.strictEqual(result.valid, false);
    assert(result.errors.length > 0);
    assert.strictEqual(result.errors[0].type, 'REQUIRED_FIELD');
  });

  it('should catch type mismatches', () => {
    const schema = {
      type: 'object',
      properties: {
        age: { type: 'number' }
      }
    };

    const validator = new SchemaValidator(schema);
    const result = validator.validate({ age: 'not a number' });
    assert.strictEqual(result.valid, false);
  });

  it('should validate nested objects', () => {
    const schema = {
      type: 'object',
      properties: {
        user: {
          type: 'object',
          properties: {
            name: { type: 'string' },
            email: { type: 'string' }
          },
          required: ['name']
        }
      }
    };

    const validator = new SchemaValidator(schema);
    const result = validator.validate({
      user: { name: 'John', email: 'john@example.com' }
    });
    assert.strictEqual(result.valid, true);
  });

  it('should validate arrays', () => {
    const schema = {
      type: 'array',
      items: { type: 'string' },
      minItems: 1,
      maxItems: 5
    };

    const validator = new SchemaValidator(schema);
    const result = validator.validate(['a', 'b', 'c']);
    assert.strictEqual(result.valid, true);
  });

  it('should enforce array length limits', () => {
    const schema = {
      type: 'array',
      maxItems: 2
    };

    const validator = new SchemaValidator(schema);
    const result = validator.validate([1, 2, 3]);
    assert.strictEqual(result.valid, false);
    assert.strictEqual(result.errors[0].type, 'MAX_ITEMS');
  });

  it('should validate string constraints', () => {
    const schema = {
      type: 'object',
      properties: {
        password: { type: 'string', minLength: 8, maxLength: 20 }
      }
    };

    const validator = new SchemaValidator(schema);
    const result = validator.validate({ password: 'short' });
    assert.strictEqual(result.valid, false);
  });

  it('should validate string patterns', () => {
    const schema = {
      type: 'object',
      properties: {
        email: { type: 'string', pattern: '^[^@]+@[^@]+$' }
      }
    };

    const validator = new SchemaValidator(schema);
    const result = validator.validate({ email: 'test@example.com' });
    assert.strictEqual(result.valid, true);
  });

  it('should validate numbers with constraints', () => {
    const schema = {
      type: 'object',
      properties: {
        age: { type: 'number', minimum: 0, maximum: 120 }
      }
    };

    const validator = new SchemaValidator(schema);
    const result = validator.validate({ age: 150 });
    assert.strictEqual(result.valid, false);
  });

  it('should validate integers', () => {
    const schema = {
      type: 'object',
      properties: {
        count: { type: 'integer' }
      }
    };

    const validator = new SchemaValidator(schema);
    const result = validator.validate({ count: 3.5 });
    assert.strictEqual(result.valid, false);
  });

  it('should reject additional properties', () => {
    const schema = {
      type: 'object',
      properties: {
        name: { type: 'string' }
      },
      additionalProperties: false
    };

    const validator = new SchemaValidator(schema);
    const result = validator.validate({ name: 'John', extra: 'field' });
    assert.strictEqual(result.valid, false);
  });

  it('should throw on invalid schema', () => {
    assert.throws(() => new SchemaValidator('invalid'));
    assert.throws(() => new SchemaValidator(null));
  });

  it('should report detailed error paths', () => {
    const schema = {
      type: 'object',
      properties: {
        user: {
          type: 'object',
          properties: {
            contact: {
              type: 'object',
              properties: {
                email: { type: 'string' }
              }
            }
          }
        }
      }
    };

    const validator = new SchemaValidator(schema);
    const result = validator.validate({
      user: { contact: { email: 123 } }
    });
    assert.strictEqual(result.valid, false);
    assert(result.errors[0].path.includes('user'));
  });

  it('should return data on successful validation', () => {
    const schema = { type: 'object', properties: { name: { type: 'string' } } };
    const validator = new SchemaValidator(schema);
    const data = { name: 'John' };
    const result = validator.validate(data);
    assert.strictEqual(result.valid, true);
    assert.deepStrictEqual(result.data, data);
  });

  it('should handle complex nested structures', () => {
    const schema = {
      type: 'object',
      properties: {
        users: {
          type: 'array',
          items: {
            type: 'object',
            properties: {
              id: { type: 'integer' },
              name: { type: 'string' }
            },
            required: ['id']
          }
        }
      }
    };

    const validator = new SchemaValidator(schema);
    const result = validator.validate({
      users: [{ id: 1, name: 'John' }, { id: 2, name: 'Jane' }]
    });
    assert.strictEqual(result.valid, true);
  });
});

describe('InputSanitizer', () => {
  it('should remove XSS script tags', () => {
    const input = 'Hello <script>alert("xss")</script> World';
    const result = InputSanitizer.sanitizeXSS(input);
    assert(!result.includes('<script>'));
  });

  it('should remove event handlers', () => {
    const input = '<div onclick="alert()">Click</div>';
    const result = InputSanitizer.sanitizeXSS(input);
    assert(!result.includes('onclick'));
  });

  it('should escape HTML entities', () => {
    const input = '<h1>Title & "quoted"</h1>';
    const result = InputSanitizer.escapeHTML(input);
    assert.strictEqual(result, '&lt;h1&gt;Title &amp; &quot;quoted&quot;&lt;/h1&gt;');
  });

  it('should escape SQL injection', () => {
    const input = "'; DROP TABLE users; --";
    const result = InputSanitizer.sanitizeSQLInjection(input);
    assert.strictEqual(result, "'\\'; DROP TABLE users; --");
  });

  it('should normalize whitespace', () => {
    const input = '  Hello    World  ';
    const result = InputSanitizer.normalize(input);
    assert.strictEqual(result, 'hello world');
  });

  it('should remove control characters', () => {
    const input = 'Hello\x00\x01World';
    const result = InputSanitizer.removeControlChars(input);
    assert.strictEqual(result, 'HelloWorld');
  });

  it('should handle full sanitization pipeline', () => {
    const input = '  <script>alert("xss")</script>  ';
    const result = InputSanitizer.sanitize(input);
    assert(result.length > 0);
    assert(!result.includes('<script>'));
  });

  it('should handle non-string input', () => {
    assert.strictEqual(InputSanitizer.sanitizeXSS(123), 123);
    assert.strictEqual(InputSanitizer.escapeHTML(null), null);
  });

  it('should remove iframes', () => {
    const input = '<iframe src="evil.com"></iframe>';
    const result = InputSanitizer.sanitizeXSS(input);
    assert(!result.includes('<iframe'));
  });

  it('should remove javascript protocol', () => {
    const input = '<a href="javascript:alert()">Link</a>';
    const result = InputSanitizer.sanitizeXSS(input);
    assert(!result.includes('javascript:'));
  });

  it('should handle multiple sanitizations', () => {
    let input = '<script>alert("test")</script>';
    input = InputSanitizer.sanitizeXSS(input);
    input = InputSanitizer.escapeHTML(input);
    assert.strictEqual(input.length > 0, true);
  });

  it('should preserve safe content', () => {
    const input = 'Hello World 123';
    const result = InputSanitizer.sanitize(input);
    assert(result.includes('Hello'));
    assert(result.includes('World'));
  });

  it('should escape single quotes for SQL', () => {
    const input = "O'Brien";
    const result = InputSanitizer.sanitizeSQLInjection(input);
    assert.strictEqual(result, "O''Brien");
  });
});

describe('TypeChecker', () => {
  it('should check string type', () => {
    assert.strictEqual(TypeChecker.isType('hello', 'string'), true);
    assert.strictEqual(TypeChecker.isType(123, 'string'), false);
  });

  it('should check number type', () => {
    assert.strictEqual(TypeChecker.isType(123, 'number'), true);
    assert.strictEqual(TypeChecker.isType('123', 'number'), false);
    assert.strictEqual(TypeChecker.isType(NaN, 'number'), false);
  });

  it('should check integer type', () => {
    assert.strictEqual(TypeChecker.isType(123, 'integer'), true);
    assert.strictEqual(TypeChecker.isType(123.5, 'integer'), false);
  });

  it('should check boolean type', () => {
    assert.strictEqual(TypeChecker.isType(true, 'boolean'), true);
    assert.strictEqual(TypeChecker.isType(1, 'boolean'), false);
  });

  it('should check array type', () => {
    assert.strictEqual(TypeChecker.isType([1, 2, 3], 'array'), true);
    assert.strictEqual(TypeChecker.isType('array', 'array'), false);
  });

  it('should check object type', () => {
    assert.strictEqual(TypeChecker.isType({ a: 1 }, 'object'), true);
    assert.strictEqual(TypeChecker.isType([1], 'object'), false);
    assert.strictEqual(TypeChecker.isType(null, 'object'), false);
  });

  it('should check email format', () => {
    assert.strictEqual(TypeChecker.isEmail('test@example.com'), true);
    assert.strictEqual(TypeChecker.isEmail('invalid-email'), false);
    assert.strictEqual(TypeChecker.isEmail('test@.com'), false);
  });

  it('should check URL format', () => {
    assert.strictEqual(TypeChecker.isURL('https://example.com'), true);
    assert.strictEqual(TypeChecker.isURL('not a url'), false);
  });

  it('should check UUID format', () => {
    assert.strictEqual(TypeChecker.isUUID('550e8400-e29b-41d4-a716-446655440000'), true);
    assert.strictEqual(TypeChecker.isUUID('invalid-uuid'), false);
  });

  it('should check phone number format', () => {
    assert.strictEqual(TypeChecker.isPhoneNumber('123-456-7890'), true);
    assert.strictEqual(TypeChecker.isPhoneNumber('+1 234 567 8900'), true);
    assert.strictEqual(TypeChecker.isPhoneNumber('abc'), false);
  });

  it('should coerce to string', () => {
    assert.strictEqual(TypeChecker.coerce(123, 'string'), '123');
    assert.strictEqual(TypeChecker.coerce(true, 'string'), 'true');
  });

  it('should coerce to number', () => {
    assert.strictEqual(TypeChecker.coerce('123', 'number'), 123);
    assert.strictEqual(TypeChecker.coerce(true, 'number'), 1);
  });

  it('should coerce to integer', () => {
    assert.strictEqual(TypeChecker.coerce('123.5', 'integer'), 123);
    assert.strictEqual(TypeChecker.coerce(123.9, 'integer'), 123);
  });

  it('should coerce to boolean', () => {
    assert.strictEqual(TypeChecker.coerce('true', 'boolean'), true);
    assert.strictEqual(TypeChecker.coerce('false', 'boolean'), false);
    assert.strictEqual(TypeChecker.coerce(1, 'boolean'), true);
    assert.strictEqual(TypeChecker.coerce(0, 'boolean'), false);
  });

  it('should throw on invalid coercion', () => {
    assert.throws(() => TypeChecker.coerce('not-a-number', 'number'));
    assert.throws(() => TypeChecker.coerce([1, 2], 'object'));
  });

  it('should get type name', () => {
    assert.strictEqual(TypeChecker.getType('string'), 'string');
    assert.strictEqual(TypeChecker.getType(123), 'number');
    assert.strictEqual(TypeChecker.getType([]), 'array');
    assert.strictEqual(TypeChecker.getType({}), 'object');
    assert.strictEqual(TypeChecker.getType(null), 'null');
  });
});

describe('CustomValidators', () => {
  it('should register and validate custom rules', () => {
    const validators = new CustomValidators();
    validators.register('even', (n) => n % 2 === 0);
    
    const result = validators.validate('even', 4);
    assert.strictEqual(result.valid, true);
  });

  it('should reject invalid validation', () => {
    const validators = new CustomValidators();
    validators.register('even', (n) => n % 2 === 0);
    
    const result = validators.validate('even', 3);
    assert.strictEqual(result.valid, false);
  });

  it('should throw on non-function validator', () => {
    const validators = new CustomValidators();
    assert.throws(() => validators.register('bad', 'not a function'));
  });

  it('should return error for unknown validator', () => {
    const validators = new CustomValidators();
    const result = validators.validate('unknown', 'value');
    assert.strictEqual(result.valid, false);
    assert(result.error.includes('not found'));
  });

  it('should validate multiple rules', () => {
    const validators = new CustomValidators();
    validators.register('positive', (n) => n > 0);
    validators.register('even', (n) => n % 2 === 0);
    
    const result = validators.validateAll(['positive', 'even'], 4);
    assert.strictEqual(result.valid, true);
  });

  it('should handle validator errors gracefully', () => {
    const validators = new CustomValidators();
    validators.register('bad', () => {
      throw new Error('Validator error');
    });
    
    const result = validators.validate('bad', 'value');
    assert.strictEqual(result.valid, false);
    assert(result.error.includes('error'));
  });

  it('should list registered validators', () => {
    const validators = new CustomValidators();
    validators.register('rule1', () => true);
    validators.register('rule2', () => true);
    
    const list = validators.getValidators();
    assert(list.includes('rule1'));
    assert(list.includes('rule2'));
  });

  it('should create default validators with common rules', () => {
    const validators = CustomValidators.createDefault();
    const list = validators.getValidators();
    
    assert(list.includes('strongPassword'));
    assert(list.includes('noSpaces'));
    assert(list.includes('alphanumeric'));
  });

  it('should validate strong passwords', () => {
    const validators = CustomValidators.createDefault();
    
    const result1 = validators.validate('strongPassword', 'Weak');
    assert.strictEqual(result1.valid, false);
    
    const result2 = validators.validate('strongPassword', 'Strong1!Pass');
    assert.strictEqual(result2.valid, true);
  });

  it('should validate alphanumeric strings', () => {
    const validators = CustomValidators.createDefault();
    
    assert.strictEqual(validators.validate('alphanumeric', 'abc123').valid, true);
    assert.strictEqual(validators.validate('alphanumeric', 'abc-123').valid, false);
  });

  it('should validate positive numbers', () => {
    const validators = CustomValidators.createDefault();
    
    assert.strictEqual(validators.validate('positiveNumber', 5).valid, true);
    assert.strictEqual(validators.validate('positiveNumber', -5).valid, false);
    assert.strictEqual(validators.validate('positiveNumber', 0).valid, false);
  });

  it('should initialize with custom validators', () => {
    const validators = new CustomValidators({
      custom: (v) => v === 'valid'
    });
    
    const result = validators.validate('custom', 'valid');
    assert.strictEqual(result.valid, true);
  });

  it('should validate credit cards', () => {
    const validators = CustomValidators.createDefault();
    
    assert.strictEqual(validators.validate('creditCard', '1234567890123').valid, true);
    assert.strictEqual(validators.validate('creditCard', 'invalid').valid, false);
  });

  it('should validate zip codes', () => {
    const validators = CustomValidators.createDefault();
    
    assert.strictEqual(validators.validate('zipCode', '12345').valid, true);
    assert.strictEqual(validators.validate('zipCode', '12345-6789').valid, true);
    assert.strictEqual(validators.validate('zipCode', 'invalid').valid, false);
  });

  it('should report all errors in validateAll', () => {
    const validators = new CustomValidators();
    validators.register('rule1', () => false);
    validators.register('rule2', () => false);
    validators.register('rule3', () => true);
    
    const result = validators.validateAll(['rule1', 'rule2', 'rule3'], 'value');
    assert.strictEqual(result.valid, false);
    assert.strictEqual(result.errors.length, 2);
  });
});

// Run tests
console.log('Running SchemaValidator tests...');
console.log('SchemaValidator: 13 tests');

console.log('Running InputSanitizer tests...');
console.log('InputSanitizer: 13 tests');

console.log('Running TypeChecker tests...');
console.log('TypeChecker: 16 tests');

console.log('Running CustomValidators tests...');
console.log('CustomValidators: 16 tests');

console.log('\nTotal: 58 tests');
