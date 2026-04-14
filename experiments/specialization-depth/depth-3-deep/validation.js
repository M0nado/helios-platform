/**
 * HELIOS REST API - Deep Specialist (Depth 3)
 * Agent 2: Schema & Validation
 * 
 * Responsible for:
 * - Request/response validation
 * - JSON Schema registration and management
 * - OpenAPI specification generation
 * 
 * @module validation
 * @version 1.0.0
 */

/**
 * Validation & Schema Manager
 * Handles all validation and schema concerns
 * @class Validation
 */
class Validation {
  /**
   * Initialize Validation Manager
   * @param {Object} config - Configuration
   * @param {string} config.apiVersion - API version
   */
  constructor(config = {}) {
    this.apiVersion = config.apiVersion || 'v1';
    this.schemas = new Map();
    this.openApiSpec = this._initializeOpenAPI();
  }

  /**
   * Initialize OpenAPI spec
   * @private
   * @returns {Object} OpenAPI spec
   */
  _initializeOpenAPI() {
    return {
      openapi: '3.0.0',
      info: {
        title: 'HELIOS REST API',
        version: this.apiVersion,
        description: 'REST API with deep specialization'
      },
      servers: [{ url: `/api/${this.apiVersion}` }],
      paths: {},
      components: {
        schemas: {},
        securitySchemes: {
          bearerAuth: {
            type: 'http',
            scheme: 'bearer',
            bearerFormat: 'JWT'
          }
        }
      }
    };
  }

  /**
   * Register JSON schema
   * @param {string} name - Schema name
   * @param {Object} schema - JSON Schema
   */
  registerSchema(name, schema) {
    this.schemas.set(name, schema);
    this.openApiSpec.components.schemas[name] = schema;
  }

  /**
   * Validate request data
   * @param {Object} data - Data to validate
   * @param {Object} schema - JSON Schema
   * @returns {Object} Validation result
   */
  validateRequest(data, schema) {
    const errors = [];

    if (schema.required) {
      for (const field of schema.required) {
        if (!(field in data)) {
          errors.push(`Missing required field: ${field}`);
        }
      }
    }

    if (schema.properties) {
      for (const [field, fieldSchema] of Object.entries(schema.properties)) {
        if (field in data) {
          const value = data[field];
          const expectedType = fieldSchema.type;

          if (expectedType && typeof value !== expectedType) {
            errors.push(
              `Field '${field}' expected type '${expectedType}', got '${typeof value}'`
            );
          }

          if (fieldSchema.pattern && typeof value === 'string') {
            const regex = new RegExp(fieldSchema.pattern);
            if (!regex.test(value)) {
              errors.push(`Field '${field}' does not match pattern: ${fieldSchema.pattern}`);
            }
          }

          if (fieldSchema.minLength && value.length < fieldSchema.minLength) {
            errors.push(
              `Field '${field}' must be at least ${fieldSchema.minLength} characters`
            );
          }

          if (fieldSchema.maxLength && value.length > fieldSchema.maxLength) {
            errors.push(
              `Field '${field}' must be at most ${fieldSchema.maxLength} characters`
            );
          }

          if (fieldSchema.enum && !fieldSchema.enum.includes(value)) {
            errors.push(
              `Field '${field}' must be one of: ${fieldSchema.enum.join(', ')}`
            );
          }
        }
      }
    }

    return {
      valid: errors.length === 0,
      errors
    };
  }

  /**
   * Add route to OpenAPI spec
   * @param {string} method - HTTP method
   * @param {string} path - Endpoint path
   * @param {Object} route - Route definition
   */
  addToOpenAPI(method, path, route) {
    if (!this.openApiSpec.paths[path]) {
      this.openApiSpec.paths[path] = {};
    }

    this.openApiSpec.paths[path][method.toLowerCase()] = {
      summary: route.description,
      operationId: `${method.toLowerCase()}${path.replace(/\//g, '_').replace(/{|}/g, '')}`,
      security: route.requireAuth ? [{ bearerAuth: [] }] : [],
      requestBody: route.schema?.request ? {
        required: true,
        content: {
          'application/json': {
            schema: route.schema.request
          }
        }
      } : undefined,
      responses: {
        200: { description: 'Success' },
        400: { description: 'Bad Request' },
        401: { description: 'Unauthorized' },
        404: { description: 'Not Found' },
        500: { description: 'Internal Server Error' }
      }
    };
  }

  /**
   * Get OpenAPI spec
   * @returns {Object} OpenAPI spec
   */
  getOpenAPISpec() {
    return this.openApiSpec;
  }
}

module.exports = Validation;
