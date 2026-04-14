using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HELIOS.Platform.Demos
{
    /// <summary>
    /// Demo 3: Developer Setup Demo
    /// Pre-configured development environment with modern tools.
    /// Installs VS Code, Git, Docker, Node.js, Python, and sets up workspace.
    /// </summary>
    public class DeveloperSetupDemo : DemoBase
    {
        public DeveloperSetupDemo() : base("DeveloperSetupDemo") { }

        protected override async Task ExecuteDemoAsync()
        {
            LogSection("Developer Environment Setup");
            LogLine("Profile: Full-Stack Development Setup", ConsoleColor.Blue);
            LogLine();

            LogSection("PHASE 0: Development Environment Validation");
            await ValidateDeveloperEnvironment();
            LogLine();

            LogSection("PHASE 1: Core Developer Tools");
            var coreTools = new List<string>
            {
                "Visual Studio Code",
                "Git (v2.42.0)",
                "GitHub CLI",
                "CMake"
            };
            await DeployToolsAsync(coreTools);
            LogLine();

            LogSection("PHASE 2: Container & Virtualization");
            var containerTools = new List<string>
            {
                "Docker Desktop",
                "Docker Compose",
                "Kubernetes (kubectl)",
                "Podman"
            };
            await DeployToolsAsync(containerTools);
            LogLine();

            LogSection("PHASE 3: Runtime Environments");
            var runtimes = new List<string>
            {
                "Node.js LTS (v20.x)",
                "Python 3.12",
                ".NET 8 SDK",
                "Java 21 JDK"
            };
            await DeployToolsAsync(runtimes);
            LogLine();

            LogSection("PHASE 4: Development Utilities");
            var utilities = new List<string>
            {
                "Visual Studio Build Tools",
                "WinGet Package Manager",
                "Postman (API Testing)",
                "DBeaver (Database Client)"
            };
            await DeployToolsAsync(utilities);
            LogLine();

            LogSection("PHASE 5: IDE Extensions");
            await ConfigureVSCodeExtensions();
            LogLine();

            LogSection("PHASE 6: Workspace Configuration");
            await SetupWorkspace();
            LogLine();

            LogSection("PHASE 7: Development Profile Creation");
            await CreateDeveloperProfile();
            LogLine();

            DisplayDeveloperDashboard();
            LogLine();

            LogSuccess("✓ Developer setup completed successfully");
            Metrics["ToolsInstalled"] = DeployedComponents.Count;
            Metrics["SetupProfile"] = "FullStack";
            Metrics["WorkspaceReady"] = true;
        }

        private async Task ValidateDeveloperEnvironment()
        {
            LogLine("✓ Windows 11 Pro (Build 23H2)");
            LogLine("✓ Administrator privileges confirmed");
            LogLine("✓ 500GB free disk space available");
            LogLine("✓ 16GB RAM available");
            LogLine("✓ Internet connectivity: 500 Mbps");
            await Task.Delay(300);
        }

        private async Task DeployToolsAsync(List<string> tools)
        {
            int toolIndex = 0;
            foreach (var tool in tools)
            {
                toolIndex++;
                LogLine($"[{toolIndex}/{tools.Count}] Installing {tool}...", ConsoleColor.Cyan);
                await DeployComponentAsync(tool, 600);
                LogLine();
            }
        }

        private async Task ConfigureVSCodeExtensions()
        {
            var extensions = new List<(string name, string id)>
            {
                ("C# Dev Kit", "ms-dotnettools.csharp"),
                ("Python", "ms-python.python"),
                ("Pylance", "ms-python.vscode-pylance"),
                ("Docker", "ms-azuretools.vscode-docker"),
                ("Git Graph", "mhutchie.git-graph"),
                ("Prettier - Code formatter", "esbenp.prettier-vscode"),
                ("ESLint", "dbaeumer.vscode-eslint"),
                ("REST Client", "humao.rest-client"),
                ("Thunder Client", "rangav.vscode-thunder-client"),
                ("Postman", "Postman.postman-for-vscode")
            };

            LogLine("Installing VS Code extensions:", ConsoleColor.Cyan);
            int extIndex = 0;
            foreach (var ext in extensions)
            {
                extIndex++;
                LogLine($"  [{extIndex}/{extensions.Count}] {ext.name}", ConsoleColor.Cyan);
                await Task.Delay(400);
                LogLine($"    ✓ Extension installed", ConsoleColor.Green);
            }
        }

        private async Task SetupWorkspace()
        {
            LogLine("• Creating workspace directories...", ConsoleColor.Cyan);
            await Task.Delay(300);
            LogLine("  ✓ C:\\Dev\\Projects created", ConsoleColor.Green);
            LogLine("  ✓ C:\\Dev\\Workspaces created", ConsoleColor.Green);
            LogLine("  ✓ C:\\Dev\\Docker created", ConsoleColor.Green);

            LogLine("• Initializing Git repositories...", ConsoleColor.Cyan);
            await Task.Delay(300);
            LogLine("  ✓ Global Git config: user.name set", ConsoleColor.Green);
            LogLine("  ✓ Global Git config: user.email set", ConsoleColor.Green);

            LogLine("• Setting up SSH keys...", ConsoleColor.Cyan);
            await Task.Delay(400);
            LogLine("  ✓ SSH key generated (RSA 4096)", ConsoleColor.Green);
            LogLine("  ✓ SSH config updated", ConsoleColor.Green);

            LogLine("• Configuring shell environment...", ConsoleColor.Cyan);
            await Task.Delay(300);
            LogLine("  ✓ PowerShell profile enhanced", ConsoleColor.Green);
            LogLine("  ✓ Aliases configured", ConsoleColor.Green);
            LogLine("  ✓ Environment variables set", ConsoleColor.Green);
        }

        private async Task CreateDeveloperProfile()
        {
            LogLine("Exporting developer profile...", ConsoleColor.Cyan);
            await Task.Delay(800);
            LogSuccess("✓ Profile exported: FullStack_Development.helios");
            LogLine("  Location: C:\\Users\\Public\\HELIOS\\Profiles\\");
            LogLine();
            LogLine("Profile includes:");
            LogLine("  • Tool configurations");
            LogLine("  • VS Code settings.json");
            LogLine("  • Git configuration");
            LogLine("  • Docker Compose templates");
            LogLine("  • Environment variables");
        }

        private void DisplayDeveloperDashboard()
        {
            LogSection("Developer Dashboard");
            LogLine("┌─────────────────────────────────────────────────────────────┐");
            LogLine("│ INSTALLATION SUMMARY                                        │");
            LogLine("├─────────────────────────────────────────────────────────────┤");
            LogLine($"│ {" IDEs & Editors",-55} │");
            LogLine($"│   ✓ Visual Studio Code v1.84.2                             │");
            LogLine($"│ {" Version Control",-55} │");
            LogLine($"│   ✓ Git v2.42.0                                            │");
            LogLine($"│   ✓ GitHub CLI v2.40.1                                     │");
            LogLine($"│ {" Container Tools",-55} │");
            LogLine($"│   ✓ Docker Desktop v24.0.6                                 │");
            LogLine($"│   ✓ Docker Compose v2.21.0                                 │");
            LogLine($"│ {" Runtime Environments",-55} │");
            LogLine($"│   ✓ Node.js v20.10.0 (LTS)                                 │");
            LogLine($"│   ✓ Python 3.12.0                                          │");
            LogLine($"│   ✓ .NET 8.0 SDK                                           │");
            LogLine($"│   ✓ Java 21 JDK                                            │");
            LogLine($"│ {" Extensions: 10 installed",-55} │");
            LogLine($"│ {" Setup Time: 18 minutes 34 seconds",-55} │");
            LogLine("└─────────────────────────────────────────────────────────────┘");
        }
    }
}
