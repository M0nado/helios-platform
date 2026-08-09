# Preview toolchain evaluation

The preview jobs compile .NET 11 C#, F#, WinUI 3, and C++23 probes without publishing
or copying output to `dist`. Python 3.14 evaluates exact, hashed versions of Microsoft
Agent Framework, Azure AI Projects, OpenAI, Anthropic, LangChain, and Microsoft Graph.
The resolved graph intentionally contains prerelease transitive packages and therefore
must remain under this directory.

`Bootstrap-WindowsDeveloperTools.ps1` defaults to audit mode. Machine-wide winget or
Chocolatey upgrades require an explicit mode and PowerShell confirmation. GitHub forks,
Azure DevOps projects, tenant permissions, SharePoint/Purview policy, and deployments
remain external approval-gated actions and are never created by this bootstrap.
