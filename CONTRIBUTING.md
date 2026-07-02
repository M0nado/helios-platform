# Contributing to MonadoBlade

## Code of Conduct

Be respectful, inclusive, and professional in all interactions.

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/YOUR-USERNAME/monadoBlade.git`
3. Create a feature branch: `git checkout -b feature/your-feature`
4. Set up your development environment (see DEVELOPMENT.md)

## Development Process

1. **Create an Issue**: Describe the feature or bug fix
2. **Implement**: Write code following our standards
3. **Test**: Write tests covering your changes (80%+ coverage)
4. **Document**: Update relevant documentation
5. **Submit PR**: Create pull request with clear description

## Code Standards

### C# Style Guide

```csharp
// Use meaningful names
public class UserAuthenticationService { }

// Use dependency injection
public UserAuthenticationService(ILogger<UserAuthenticationService> logger) 
{ 
    _logger = logger;
}

// Use nullable reference types
#nullable enable
public string? GetUserName(int userId) { }

// Prefer immutability where possible
public sealed class Configuration
{
    public required string ConnectionString { get; init; }
}

// Use async/await consistently
public async Task<User> GetUserAsync(int id)
{
    return await _userRepository.GetAsync(id);
}
```

### File Organization

```
Module/
├── Abstractions/
│   └── IMyService.cs
├── Implementation/
│   └── MyService.cs
├── Models/
│   └── MyModel.cs
├── Exceptions/
│   └── MyException.cs
└── Extensions/
    └── MyServiceExtensions.cs
```

## Testing Requirements

### Test Coverage

- **Unit Tests**: Minimum 80% coverage
- **Integration Tests**: Cover critical paths
- **Performance Tests**: For performance-critical code

### Test Structure

```csharp
[Fact]
public void MyService_WithSpecificCondition_ShouldProduceExpectedResult()
{
    // Arrange - Set up test data
    var service = CreateService();
    var input = new TestInput();

    // Act - Execute the code under test
    var result = service.DoSomething(input);

    // Assert - Verify the result
    Assert.NotNull(result);
    Assert.Equal(expected, result);
}
```

## Commit Messages

Use clear, descriptive commit messages:

```
Format: <type>(<scope>): <subject>

Types:
- feat: A new feature
- fix: A bug fix
- docs: Documentation only changes
- style: Changes that don't affect code meaning
- refactor: Code change without feature/fix
- perf: Performance improvement
- test: Adding or updating tests
- chore: Build process, dependencies, etc.

Example:
feat(security): add two-factor authentication
fix(graphics): resolve DirectX initialization bug
docs(core): update logging configuration guide
```

## Pull Request Process

1. Update CHANGELOG.md with your changes
2. Update README.md if you've added new features
3. Ensure all tests pass locally
4. Request review from relevant team members
5. Address review comments
6. Squash commits if requested
7. Merge when approved

## PR Title Format

```
[Module] Brief description of changes

Example:
[Security] Implement password hashing with bcrypt
[Graphics] Fix DirectX 11 initialization crash
[Core] Add configuration validation
```

## Documentation

### Code Comments

```csharp
// Use XML documentation for public members
/// <summary>
/// Authenticates a user with credentials.
/// </summary>
/// <param name="username">The user's username</param>
/// <param name="password">The user's password</param>
/// <returns>Authentication token if successful; null otherwise</returns>
public async Task<AuthToken?> AuthenticateAsync(string username, string password)
{
}
```

### Module Documentation

Each module should have:

1. **README.md**: Overview and usage examples
2. **ARCHITECTURE.md**: Design decisions and patterns
3. **API.md**: Public API documentation
4. **EXAMPLES.md**: Usage examples and recipes

## Release Process

1. Update version in `.csproj` files
2. Update CHANGELOG.md
3. Merge to main branch
4. Tag release: `git tag v1.2.3`
5. Push tag: `git push origin v1.2.3`
6. GitHub Actions automatically creates release

## Local Development Setup

### Build Solution
```bash
dotnet build MonadoBlade.sln
```

### Run Tests
```bash
.\build\test.ps1 -TestType All
```

### Generate Documentation
```bash
dotnet build MonadoBlade.sln /p:GenerateDocumentationFile=true
```

## Troubleshooting

### Build Issues

```bash
# Clean and rebuild
dotnet clean MonadoBlade.sln
dotnet build MonadoBlade.sln --configuration Debug

# Restore nuget packages
dotnet restore MonadoBlade.sln
```

### Test Failures

```bash
# Run with verbose output
dotnet test MonadoBlade.sln -v normal

# Run specific test
dotnet test MonadoBlade.sln --filter "NamespaceName.ClassName.MethodName"
```

## Review Checklist

- [ ] Code follows style guide
- [ ] All tests pass locally
- [ ] Test coverage >80%
- [ ] Documentation updated
- [ ] No breaking changes (or documented)
- [ ] Commit messages are clear
- [ ] No hardcoded secrets or credentials
- [ ] Performance implications considered

## Questions or Need Help?

- Check existing issues and discussions
- Ask in GitHub Discussions
- Contact team leads for guidance

---

Thank you for contributing to MonadoBlade! 🎉
