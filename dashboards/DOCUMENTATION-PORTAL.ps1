<#
.SYNOPSIS
    HELIOS Documentation Portal - Interactive Documentation Server
    
.DESCRIPTION
    Uses PROJECT_DOCUMENTATION_INDEX.md and related documentation
    to serve an interactive portal for navigating all 78+ docs
    
.EXAMPLE
    .\DOCUMENTATION-PORTAL.ps1
    
    Then access at http://localhost:8888

#>

param(
    [int]$Port = 8888,
    [switch]$NoLaunch
)

# Colors
$colors = @{
    Success = 'Green'
    Info = 'Cyan'
    Warning = 'Yellow'
}

Write-Host "`n🚀 Starting HELIOS Documentation Portal..." -ForegroundColor $colors['Info']
Write-Host "   Port: $Port`n"

# Load documentation index
$indexFile = "PROJECT_DOCUMENTATION_INDEX.md"
if (-not (Test-Path $indexFile)) {
    Write-Host "❌ Index not found: $indexFile" -ForegroundColor Red
    exit 1
}

$indexContent = Get-Content $indexFile -Raw
Write-Host "✅ Loaded: $indexFile" -ForegroundColor $colors['Success']

# Create HTTP listener
$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://localhost:$Port/")
$listener.Start()
Write-Host "✅ Listening on http://localhost:$Port/" -ForegroundColor $colors['Success']

# Launch browser if not disabled
if (-not $NoLaunch) {
    Start-Sleep -Milliseconds 500
    Start-Process "http://localhost:$Port/"
}

# HTML Template
$htmlTemplate = @'
<!DOCTYPE html>
<html>
<head>
    <title>HELIOS Documentation Portal</title>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            padding: 20px;
        }
        .container {
            max-width: 1200px;
            margin: 0 auto;
            background: white;
            border-radius: 10px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            overflow: hidden;
        }
        .header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 40px 20px;
            text-align: center;
        }
        .header h1 { font-size: 2.5em; margin-bottom: 10px; }
        .header p { font-size: 1.1em; opacity: 0.9; }
        .content {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
            padding: 40px;
        }
        .card {
            background: #f8f9fa;
            border: 1px solid #ddd;
            border-radius: 8px;
            padding: 20px;
            cursor: pointer;
            transition: all 0.3s ease;
        }
        .card:hover {
            transform: translateY(-5px);
            box-shadow: 0 10px 20px rgba(0,0,0,0.1);
            border-color: #667eea;
        }
        .card h3 {
            color: #667eea;
            margin-bottom: 10px;
            font-size: 1.3em;
        }
        .card p {
            color: #666;
            font-size: 0.95em;
            line-height: 1.5;
        }
        .card .meta {
            margin-top: 15px;
            padding-top: 15px;
            border-top: 1px solid #ddd;
            font-size: 0.85em;
            color: #999;
        }
        .badge {
            display: inline-block;
            background: #667eea;
            color: white;
            padding: 3px 8px;
            border-radius: 3px;
            font-size: 0.8em;
            margin-right: 5px;
            margin-bottom: 5px;
        }
        .search {
            padding: 30px 40px;
            background: #f8f9fa;
            border-bottom: 1px solid #ddd;
        }
        .search input {
            width: 100%;
            padding: 12px;
            border: 1px solid #ddd;
            border-radius: 5px;
            font-size: 1em;
        }
        .stats {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 20px;
            padding: 30px 40px;
            background: #f8f9fa;
            border-bottom: 1px solid #ddd;
        }
        .stat {
            text-align: center;
        }
        .stat .number {
            font-size: 2em;
            color: #667eea;
            font-weight: bold;
        }
        .stat .label {
            color: #666;
            font-size: 0.9em;
            margin-top: 5px;
        }
        .footer {
            padding: 20px 40px;
            background: #f8f9fa;
            border-top: 1px solid #ddd;
            text-align: center;
            color: #999;
            font-size: 0.9em;
        }
        @media (max-width: 768px) {
            .content { grid-template-columns: 1fr; }
            .header h1 { font-size: 1.8em; }
        }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>📚 HELIOS Documentation Portal</h1>
            <p>Complete documentation for the HELIOS Platform ecosystem</p>
        </div>
        
        <div class="search">
            <input type="text" id="searchBox" placeholder="🔍 Search documentation...">
        </div>
        
        <div class="stats">
            <div class="stat">
                <div class="number">78+</div>
                <div class="label">Documentation Files</div>
            </div>
            <div class="stat">
                <div class="number">7</div>
                <div class="label">Components</div>
            </div>
            <div class="stat">
                <div class="number">7</div>
                <div class="label">Deployment Phases</div>
            </div>
            <div class="stat">
                <div class="number">200+</div>
                <div class="label">Documentation Sections</div>
            </div>
        </div>
        
        <div class="content" id="content">
            <div class="card">
                <h3>🚀 Getting Started</h3>
                <p>Quick start guide for new team members and setup procedures.</p>
                <div class="meta">
                    <span class="badge">Beginner</span>
                    <span class="badge">Setup</span>
                </div>
            </div>
            
            <div class="card">
                <h3>📋 Project Board</h3>
                <p>Configure and manage GitHub Project board for team coordination.</p>
                <div class="meta">
                    <span class="badge">Manager</span>
                    <span class="badge">Planning</span>
                </div>
            </div>
            
            <div class="card">
                <h3>⚙️ Workflows & CI/CD</h3>
                <p>Deployment pipelines and automated testing workflows.</p>
                <div class="meta">
                    <span class="badge">DevOps</span>
                    <span class="badge">Automation</span>
                </div>
            </div>
            
            <div class="card">
                <h3>🔒 Security Setup</h3>
                <p>Security configuration, hardening, and compliance.</p>
                <div class="meta">
                    <span class="badge">Security</span>
                    <span class="badge">Critical</span>
                </div>
            </div>
            
            <div class="card">
                <h3>💻 API Reference</h3>
                <p>Complete API documentation and code examples.</p>
                <div class="meta">
                    <span class="badge">Developer</span>
                    <span class="badge">Reference</span>
                </div>
            </div>
            
            <div class="card">
                <h3>🛠️ Troubleshooting</h3>
                <p>Common issues, debugging tips, and solutions.</p>
                <div class="meta">
                    <span class="badge">Support</span>
                    <span class="badge">Help</span>
                </div>
            </div>
        </div>
        
        <div class="footer">
            <p>📖 HELIOS Platform v1.0 | Documentation Portal | Updated April 2026</p>
            <p style="margin-top: 10px; font-size: 0.85em;">
                <a href="#" style="color: #667eea; text-decoration: none;">GitHub</a> | 
                <a href="#" style="color: #667eea; text-decoration: none;">Report Issue</a> | 
                <a href="#" style="color: #667eea; text-decoration: none;">Contribute</a>
            </p>
        </div>
    </div>
</body>
</html>
'@

# Request handler
$stop = $false
try {
    while (-not $stop) {
        $context = $listener.GetContext()
        $request = $context.Request
        $response = $context.Response
        
        # Route handling
        $path = $request.Url.LocalPath
        
        if ($path -eq "/" -or $path -eq "") {
            $response.ContentType = "text/html; charset=utf-8"
            $buffer = [System.Text.Encoding]::UTF8.GetBytes($htmlTemplate)
            $response.OutputStream.Write($buffer, 0, $buffer.Length)
        }
        elseif ($path -eq "/index") {
            $response.ContentType = "text/markdown; charset=utf-8"
            $buffer = [System.Text.Encoding]::UTF8.GetBytes($indexContent)
            $response.OutputStream.Write($buffer, 0, $buffer.Length)
        }
        else {
            $response.StatusCode = 404
            $response.ContentType = "text/plain"
            $buffer = [System.Text.Encoding]::UTF8.GetBytes("Not Found")
            $response.OutputStream.Write($buffer, 0, $buffer.Length)
        }
        
        $response.OutputStream.Close()
    }
}
finally {
    $listener.Stop()
}
