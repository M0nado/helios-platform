# HELIOS Platform Source Code Organization

Structure and organization of the HELIOS Platform source code.

---

## 📁 Folder Structure

### src/
Source code for the HELIOS Platform.

```
src/
├── HELIOS.Platform/
│   ├── Core/
│   │   ├── Engine.cs
│   │   ├── Orchestrator.cs
│   │   └── ...
│   ├── Components/
│   │   ├── IComponent.cs
│   │   ├── Component.cs
│   │   └── ...
│   ├── Plugins/
│   │   ├── IPlugin.cs
│   │   ├── PluginManager.cs
│   │   └── ...
│   ├── Storage/
│   │   ├── IStorage.cs
│   │   ├── StorageProvider.cs
│   │   └── ...
│   ├── API/
│   │   ├── Controllers/
│   │   ├── Models/
│   │   └── ...
│   ├── BackendServices/
│   │   ├── Services/
│   │   ├── Models/
│   │   └── ...
│   ├── Presentation/
│   │   ├── Views/
│   │   ├── ViewModels/
│   │   └── ...
│   └── Properties/
│       └── AssemblyInfo.cs
└── ...
```

---

## 🧩 Module Organization

### Core Module (src/HELIOS.Platform/Core/)
Central orchestration and execution engine.
- Engine initialization and lifecycle
- Component orchestration
- Deployment scheduling

### Components Module (src/HELIOS.Platform/Components/)
Component abstraction and management.
- Component interfaces
- Component lifecycle
- Component communication

### Plugins Module (src/HELIOS.Platform/Plugins/)
Plugin system and management.
- Plugin discovery and loading
- Plugin lifecycle management
- Plugin context and APIs

### Storage Module (src/HELIOS.Platform/Storage/)
Data persistence and retrieval.
- Data models
- Storage providers
- Database access

### API Module (src/HELIOS.Platform/API/)
REST API endpoints and controllers.
- Deployment controllers
- Monitoring controllers
- System endpoints

### BackendServices Module (src/HELIOS.Platform/BackendServices/)
Background services and workers.
- Deployment worker
- Monitoring service
- Health check service

### Presentation Module (src/HELIOS.Platform/Presentation/)
Web UI and user interfaces.
- Dashboard views
- Configuration UI
- Monitoring views

---

## 🗂️ File Organization Standards

### Naming Conventions

**Classes**
```csharp
public class PascalCaseClassName { }
```

**Interfaces**
```csharp
public interface IPascalCaseInterfaceName { }
```

**Methods**
```csharp
public void PascalCaseMethodName() { }
```

**Fields/Properties**
```csharp
private string _camelCaseField;
public string PascalCaseProperty { get; set; }
```

### File Structure

- One class per file (with exceptions for small helper classes)
- File name matches class name
- Namespace matches folder structure
- Consistent formatting and indentation

---

## 🔄 Cross-Module Communication

### Dependency Injection
All dependencies injected through constructors.

### Interfaces
Communication between modules through well-defined interfaces.

### Services
Services provide functionality across modules.

---

## 📖 Related Documentation

- **[Architecture](../docs/architecture/README.md)** - System design
- **[Components](../docs/architecture/COMPONENTS.md)** - Component details
- **[API Reference](../docs/api/README.md)** - API documentation

---

**Last Updated:** 2026-04-16 | [Back to Main Documentation](../DOCUMENTATION_INDEX.md)
