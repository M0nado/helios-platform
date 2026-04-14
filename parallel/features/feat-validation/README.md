# HELIOS v4.0 Request Validation Module (70 KB)

Production-ready validation with schema validation, input sanitization, type checking, and custom validators.

## Features

- **Schema Validation**: JSON schema validation with detailed error paths
- **Input Sanitization**: XSS prevention, SQL injection escaping, HTML encoding
- **Type Checking**: Built-in type validators (email, URL, UUID, phone)
- **Custom Validators**: Extensible validation rules for business logic
- **100% JSDoc**: Complete API documentation with examples
- **58 Quality Tests**: Comprehensive test coverage
- **Performance**: Fast validation with early exit on errors

## Installation

```javascript
const {
  SchemaValidator,
  InputSanitizer,
  TypeChecker,
  CustomValidators
} = require('./implementation');
```

## API Documentation

### SchemaValidator

```javascript
const validator = new SchemaValidator(schema);
```

**Parameters:**
- `schema` (Object): JSON schema object

**Methods:**

```javascript
// Validate data against schema
validator.validate(data) // { valid, errors, errorCount, data }

// Get schema
validator.getSchema() // Object
```

**Supported Schema Properties:**
- `type` (string): 'object', 'array', 'string', 'number', 'integer'
- `properties` (Object): Property schemas for objects
- `required` (Array): Required property names
- `items` (Object): Item schema for arrays
- `minLength`/`maxLength`: String length constraints
- `minimum`/`maximum`: Number constraints
- `minItems`/`maxItems`: Array length constraints
- `pattern` (string): Regular expression for strings
- `additionalProperties` (boolean): Allow extra properties

**Example:**

```javascript
const schema = {
  type: 'object',
  properties: {
    email: { type: 'string', pattern: '^[^@]+@[^@]+$' },
    age: { type: 'integer', minimum: 18, maximum: 120 }
  },
  required: ['email']
};

const validator = new SchemaValidator(schema);
const result = validator.validate({ email: 'user@example.com', age: 25 });
// { valid: true, errors: [], errorCount: 0, data: {...} }
```

### InputSanitizer

Static utility methods for sanitizing untrusted input.

```javascript
// Remove XSS threats
InputSanitizer.sanitizeXSS(input) // string

// Escape SQL injection
InputSanitizer.sanitizeSQLInjection(input) // string

// Escape HTML entities
InputSanitizer.escapeHTML(input) // string

// Normalize whitespace
InputSanitizer.normalize(input) // string (trimmed, lowercase, normalized)

// Remove control characters
InputSanitizer.removeControlChars(input) // string

// Full sanitization
InputSanitizer.sanitize(input) // string
```

**Example:**

```javascript
const dangerous = '<img src=x onerror="alert()">';
const safe = InputSanitizer.sanitizeXSS(dangerous);
// Result: '<img src=x >'

const forSQL = "O'Brien";
const escaped = InputSanitizer.sanitizeSQLInjection(forSQL);
// Result: "O''Brien"
```

### TypeChecker

Static utility for type validation and coercion.

```javascript
// Check type
TypeChecker.isType(value, 'string') // boolean

// Coerce to type
TypeChecker.coerce(value, 'number') // number

// Format-specific validators
TypeChecker.isEmail(email) // boolean
TypeChecker.isURL(url) // boolean
TypeChecker.isUUID(uuid) // boolean
TypeChecker.isPhoneNumber(phone) // boolean

// Get JavaScript type
TypeChecker.getType(value) // string
```

**Supported Types:**
- `string`, `number`, `integer`, `boolean`, `array`, `object`
- `null`, `date`, `email`, `url`, `uuid`

**Example:**

```javascript
TypeChecker.isEmail('user@example.com'); // true
TypeChecker.coerce('123', 'number'); // 123
TypeChecker.coerce('true', 'boolean'); // true
```

### CustomValidators

```javascript
const validators = new CustomValidators([initialValidators]);
```

**Methods:**

```javascript
// Register custom validator
validators.register(name, (value) => boolean)

// Validate single rule
validators.validate(name, value) // { valid, error }

// Validate multiple rules
validators.validateAll(names, value) // { valid, results, errors }

// List validators
validators.getValidators() // Array<string>
```

**Example:**

```javascript
const validators = new CustomValidators();
validators.register('even', (n) => n % 2 === 0);
validators.register('positive', (n) => n > 0);

const result = validators.validateAll(['even', 'positive'], 4);
// { valid: true, results: [...], errors: [] }
```

**Built-in Default Validators:**

Create with `CustomValidators.createDefault()`:
- `strongPassword`: At least 8 chars, uppercase, lowercase, number, special
- `noSpaces`: String without whitespace
- `alphanumeric`: Only letters and numbers
- `positiveNumber`: Number > 0
- `creditCard`: Valid credit card format
- `zipCode`: US zip code format

## Error Reporting

All validators provide detailed error information:

```javascript
const result = validator.validate(data);

// Errors contain:
// - path: Location in nested structure (e.g., "user.email")
// - message: Human-readable description
// - type: Error code (e.g., "TYPE_MISMATCH", "REQUIRED_FIELD")
```

## Performance Characteristics

| Feature | Time Complexity | Notes |
|---------|-----------------|-------|
| Type Check | O(1) | Single comparison |
| Schema Validation | O(n) | n = properties/items |
| Sanitization | O(m) | m = string length |
| Custom Validation | O(v) | v = number of validators |

## Real-World Scenarios

### 1. API Request Validation

```javascript
const schema = {
  type: 'object',
  properties: {
    userId: { type: 'string', pattern: '^[0-9]+$' },
    action: { type: 'string' },
    timestamp: { type: 'integer' }
  },
  required: ['userId', 'action']
};

const validator = new SchemaValidator(schema);
if (!validator.validate(req.body).valid) {
  return res.status(400).json({ error: 'Invalid request' });
}
```

### 2. Form Validation with Sanitization

```javascript
const userInput = '<script>alert("xss")</script>';
const sanitized = InputSanitizer.sanitize(userInput);
const escaped = InputSanitizer.escapeHTML(sanitized);
// Safe for display
```

### 3. Database Query Protection

```javascript
const username = req.query.username;
const safe = InputSanitizer.sanitizeSQLInjection(username);
const query = `SELECT * FROM users WHERE username = '${safe}'`;
```

### 4. Custom Business Rules

```javascript
const validators = CustomValidators.createDefault();
validators.register('companyEmail', (email) => {
  return TypeChecker.isEmail(email) && email.endsWith('@company.com');
});

const result = validators.validate('companyEmail', 'user@company.com');
```

## Security Best Practices

1. **Always Sanitize User Input**
   ```javascript
   const safe = InputSanitizer.sanitize(userInput);
   ```

2. **Validate Before Processing**
   ```javascript
   const result = validator.validate(data);
   if (!result.valid) throw new Error('Invalid data');
   ```

3. **Use Type Coercion Safely**
   ```javascript
   try {
     const num = TypeChecker.coerce(value, 'number');
   } catch (e) {
     // Handle coercion failure
   }
   ```

4. **Escape Output**
   ```javascript
   const safe = InputSanitizer.escapeHTML(userContent);
   ```

## Testing

The module includes 58 comprehensive tests:

- **SchemaValidator**: 13 tests for schemas, types, constraints
- **InputSanitizer**: 13 tests for XSS, SQL injection, HTML encoding
- **TypeChecker**: 16 tests for type validation and coercion
- **CustomValidators**: 16 tests for custom rules and defaults

Run tests with your preferred framework:

```bash
mocha tests/tests.js
```

## Error Types

| Type | Description |
|------|-------------|
| `TYPE_MISMATCH` | Value type doesn't match expected |
| `REQUIRED_FIELD` | Required property is missing |
| `ADDITIONAL_PROPERTIES` | Extra properties not allowed |
| `MIN_LENGTH` | String too short |
| `MAX_LENGTH` | String too long |
| `MIN_ITEMS` | Array too few items |
| `MAX_ITEMS` | Array too many items |
| `MINIMUM` | Number too small |
| `MAXIMUM` | Number too large |
| `PATTERN_MISMATCH` | String doesn't match pattern |
| `NOT_INTEGER` | Value is not an integer |

## License

HELIOS v4.0 - Proprietary

## Support

For issues or feature requests, contact the HELIOS development team.
