<#
.SYNOPSIS
    Dashboard UI themes and customization
.DESCRIPTION
    Theme management and UI customization for monitoring dashboard
.VERSION
    1.0.0
#>

param()

. "$PSScriptRoot\..\lib\monitoring-library.ps1"

Initialize-MonitoringLog -Component "Dashboard-Themes"

$ThemeConfig = @{
    Dark = @{
        Name = "Dark"
        Background = @{ R = 30; G = 30; B = 30 }
        Foreground = @{ R = 255; G = 255; B = 255 }
        Accent = @{ R = 0; G = 120; B = 212 }
        Header = "Cyan"
        Success = "Green"
        Warning = "Yellow"
        Error = "Red"
        Info = "Cyan"
        CSS = @"
body { background: #1e1e1e; color: #ffffff; font-family: 'Segoe UI', sans-serif; }
.card { background: #2d2d2d; border: 1px solid #3d3d3d; border-radius: 8px; padding: 16px; }
.card.critical { border-left: 4px solid #ff0000; }
.card.warning { border-left: 4px solid #ffaa00; }
.status-ok { color: #00ff00; }
.status-warning { color: #ffaa00; }
.status-critical { color: #ff0000; }
"@
    }
    
    Light = @{
        Name = "Light"
        Background = @{ R = 255; G = 255; B = 255 }
        Foreground = @{ R = 0; G = 0; B = 0 }
        Accent = @{ R = 0; G = 120; B = 212 }
        Header = "Blue"
        Success = "Green"
        Warning = "DarkYellow"
        Error = "Red"
        Info = "Blue"
        CSS = @"
body { background: #ffffff; color: #000000; font-family: 'Segoe UI', sans-serif; }
.card { background: #f5f5f5; border: 1px solid #d0d0d0; border-radius: 8px; padding: 16px; }
.card.critical { border-left: 4px solid #c00000; }
.card.warning { border-left: 4px solid #ff8c00; }
.status-ok { color: #00b050; }
.status-warning { color: #ff8c00; }
.status-critical { color: #c00000; }
"@
    }
    
    Ocean = @{
        Name = "Ocean"
        Background = @{ R = 15; G = 35; B = 65 }
        Foreground = @{ R = 200; G = 220; B = 255 }
        Accent = @{ R = 100; G = 200; B = 255 }
        Header = "Cyan"
        Success = "Green"
        Warning = "Yellow"
        Error = "Red"
        Info = "Cyan"
        CSS = @"
body { background: #0f2341; color: #c8dcff; font-family: 'Courier New', monospace; }
.card { background: #1a3d5c; border: 1px solid #2d5a7f; border-radius: 4px; padding: 14px; }
.card.critical { border-left: 4px solid #ff6b6b; }
.card.warning { border-left: 4px solid #ffd93d; }
.status-ok { color: #6bcf7f; }
.status-warning { color: #ffd93d; }
.status-critical { color: #ff6b6b; }
"@
    }
    
    Retro = @{
        Name = "Retro"
        Background = @{ R = 0; G = 255; B = 0 }
        Foreground = @{ R = 0; G = 0; B = 0 }
        Accent = @{ R = 255; G = 255; B = 0 }
        Header = "Green"
        Success = "Green"
        Warning = "Yellow"
        Error = "Red"
        Info = "Green"
        CSS = @"
body { background: #00ff00; color: #000000; font-family: 'Courier New', monospace; text-shadow: 1px 1px 0px #00aa00; }
.card { background: #00dd00; border: 2px dashed #000000; border-radius: 0px; padding: 12px; }
.card.critical { border-color: #ff0000; }
.card.warning { border-color: #ffaa00; }
"@
    }
}

class ThemeManager {
    [string]$CurrentTheme = "Dark"
    [hashtable]$Themes = $ThemeConfig
    [hashtable]$CustomSettings = @{}
    
    ThemeManager() {
        $this.LoadUserSettings()
    }
    
    [void]LoadUserSettings() {
        $settingsPath = "$PSScriptRoot\..\config\theme-settings.json"
        if (Test-Path $settingsPath) {
            try {
                $settings = Get-Content $settingsPath | ConvertFrom-Json
                $this.CurrentTheme = $settings.CurrentTheme
                $this.CustomSettings = $settings.CustomSettings
            }
            catch {
                Write-MonitoringLog "Failed to load theme settings: $_" -Level "DEBUG"
            }
        }
    }
    
    [void]SaveUserSettings() {
        $settingsPath = "$PSScriptRoot\..\config\theme-settings.json"
        try {
            $settings = @{
                CurrentTheme = $this.CurrentTheme
                CustomSettings = $this.CustomSettings
            }
            $settings | ConvertTo-Json | Out-File -FilePath $settingsPath -Encoding UTF8
        }
        catch {
            Write-MonitoringLog "Failed to save theme settings: $_" -Level "DEBUG"
        }
    }
    
    [void]SetTheme([string]$ThemeName) {
        if ($this.Themes.ContainsKey($ThemeName)) {
            $this.CurrentTheme = $ThemeName
            $this.SaveUserSettings()
            Write-MonitoringLog "Theme changed to: $ThemeName"
        }
        else {
            Write-MonitoringLog "Theme not found: $ThemeName" -Level "WARNING"
        }
    }
    
    [hashtable]GetCurrentTheme() {
        return $this.Themes[$this.CurrentTheme].Clone()
    }
    
    [array]GetAvailableThemes() {
        return $this.Themes.Keys | Sort-Object
    }
    
    [void]SetCustomColor([string]$Element, [string]$Color) {
        $this.CustomSettings[$Element] = $Color
        $this.SaveUserSettings()
    }
    
    [string]GetThemeCSS() {
        return $this.Themes[$this.CurrentTheme].CSS
    }
}

function Get-ThemePreview {
    param(
        [string]$ThemeName = "Dark"
    )
    
    if (-not $ThemeConfig.ContainsKey($ThemeName)) {
        Write-Error "Theme not found: $ThemeName"
        return
    }
    
    $theme = $ThemeConfig[$ThemeName]
    
    Clear-Host
    Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor $theme.Header
    Write-Host "║  THEME PREVIEW: $($theme.Name)" -PadRight 50 -ForegroundColor $theme.Header
    Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor $theme.Header
    Write-Host ""
    
    Write-Host "COLOR PALETTE" -ForegroundColor $theme.Header
    Write-Host "  Header Text" -ForegroundColor $theme.Header
    Write-Host "  Success Status" -ForegroundColor $theme.Success
    Write-Host "  Warning Status" -ForegroundColor $theme.Warning
    Write-Host "  Error Status" -ForegroundColor $theme.Error
    Write-Host "  Info Status" -ForegroundColor $theme.Info
    Write-Host ""
    
    Write-Host "SAMPLE CARDS" -ForegroundColor $theme.Header
    Write-Host ""
    Write-Host "┌─ Normal Card ──────────────────────────────────────────────────┐" -ForegroundColor $theme.Accent
    Write-Host "│ This is a normal information card                             │" -ForegroundColor $theme.Accent
    Write-Host "└────────────────────────────────────────────────────────────────┘" -ForegroundColor $theme.Accent
    Write-Host ""
    
    Write-Host "┌─ Success Card ─────────────────────────────────────────────────┐" -ForegroundColor $theme.Success
    Write-Host "│ All systems operational                                        │" -ForegroundColor $theme.Success
    Write-Host "└────────────────────────────────────────────────────────────────┘" -ForegroundColor $theme.Success
    Write-Host ""
    
    Write-Host "┌─ Warning Card ─────────────────────────────────────────────────┐" -ForegroundColor $theme.Warning
    Write-Host "│ Attention required - High CPU usage detected                   │" -ForegroundColor $theme.Warning
    Write-Host "└────────────────────────────────────────────────────────────────┘" -ForegroundColor $theme.Warning
    Write-Host ""
    
    Write-Host "┌─ Critical Card ────────────────────────────────────────────────┐" -ForegroundColor $theme.Error
    Write-Host "│ Critical: Disk space critically low                            │" -ForegroundColor $theme.Error
    Write-Host "└────────────────────────────────────────────────────────────────┘" -ForegroundColor $theme.Error
    Write-Host ""
}

function Show-ThemeGallery {
    foreach ($themeName in @("Dark", "Light", "Ocean", "Retro")) {
        Get-ThemePreview -ThemeName $themeName
        Write-Host ""
        Write-Host "Press any key to continue..." -ForegroundColor Gray
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        Clear-Host
    }
}

function Export-ThemeCSS {
    param(
        [string]$ThemeName = "Dark",
        [string]$OutputPath = "$PSScriptRoot\..\dashboard-theme.css"
    )
    
    if (-not $ThemeConfig.ContainsKey($ThemeName)) {
        Write-Error "Theme not found: $ThemeName"
        return
    }
    
    $css = $ThemeConfig[$ThemeName].CSS
    
    $css | Out-File -FilePath $OutputPath -Encoding UTF8
    Write-MonitoringLog "Theme CSS exported to: $OutputPath"
    return $true
}

function Get-DashboardHTML {
    param(
        [string]$ThemeName = "Dark",
        [hashtable]$Metrics = @{}
    )
    
    if (-not $ThemeConfig.ContainsKey($ThemeName)) {
        $ThemeName = "Dark"
    }
    
    $theme = $ThemeConfig[$ThemeName]
    $css = $theme.CSS
    
    $html = @"
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Enterprise Monitoring Dashboard - $($theme.Name) Theme</title>
    <style>
        $css
        * { margin: 0; padding: 0; box-sizing: border-box; }
        html, body { height: 100%; }
        .container { max-width: 1400px; margin: 0 auto; padding: 20px; }
        .header { margin-bottom: 30px; }
        .header h1 { margin-bottom: 10px; }
        .dashboard-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(350px, 1fr)); gap: 20px; }
        .card h3 { margin-bottom: 15px; }
        .metric-value { font-size: 32px; font-weight: bold; margin: 10px 0; }
        .metric-label { font-size: 12px; opacity: 0.7; }
        .chart { height: 200px; background: rgba(0,0,0,0.1); border-radius: 4px; margin: 10px 0; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>Enterprise Monitoring Dashboard</h1>
            <p style="opacity: 0.7;">Real-time system monitoring and health status</p>
        </div>
        
        <div class="dashboard-grid">
            <div class="card">
                <h3>System Status</h3>
                <div class="metric-value status-ok">✓ Operational</div>
                <div class="metric-label">All systems nominal</div>
            </div>
            
            <div class="card">
                <h3>CPU Utilization</h3>
                <div class="metric-value">45%</div>
                <div class="chart"></div>
            </div>
            
            <div class="card">
                <h3>Memory Usage</h3>
                <div class="metric-value">62%</div>
                <div class="chart"></div>
            </div>
            
            <div class="card">
                <h3>Active Alerts</h3>
                <div class="metric-value status-warning">3</div>
                <div class="metric-label">1 High, 2 Medium</div>
            </div>
        </div>
    </div>
    
    <script>
        // Auto-refresh every 30 seconds
        setTimeout(() => location.reload(), 30000);
    </script>
</body>
</html>
"@
    
    return $html
}

Export-ModuleMember -Function @('Get-ThemePreview', 'Show-ThemeGallery', 'Export-ThemeCSS', 'Get-DashboardHTML')
Export-ModuleMember -Class 'ThemeManager'
