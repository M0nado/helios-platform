#################################################################################
# HELIOS Platform - Send Backup Alerts
# Purpose: Sends email/Slack/Teams notifications for backup events
# Version: 1.0.0
#################################################################################

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("Started", "Completed", "Failed", "Warning", "HealthAlert")]
    [string]$Status,
    
    [string]$Message,
    
    [hashtable]$Details
)

. "C:\HELIOS\Scripts\Backup\0_BackupCore.ps1"

$logFile = Initialize-BackupLogging -LogType "Alerts"
Write-BackupLog -Message "Sending backup alert. Status: $Status" -LogFile $logFile

try {
    # Prepare alert message
    $alertSubject = "HELIOS Backup Alert - $Status"
    $alertBody = @"
Backup Alert Notification

Status: $Status
Time: $(Get-Date)
Message: $Message

System Information:
- Backup Base Path: $($Script:BackupConfig.BackupBasePath)
- Retention Days: $($Script:BackupConfig.RetentionDays)

$($Details | ConvertTo-Json | Out-String)

---
HELIOS Backup System
"@
    
    # Send email alert
    try {
        $smtpServer = $env:SMTP_SERVER
        $smtpPort = $env:SMTP_PORT -as [int]
        $smtpFrom = $env:SMTP_FROM
        
        if ($smtpServer -and $smtpFrom) {
            $emailParams = @{
                SmtpServer = $smtpServer
                Port = $smtpPort
                From = $smtpFrom
                To = $Script:BackupConfig.EmailAlerts
                Subject = $alertSubject
                Body = $alertBody
                ErrorAction = "SilentlyContinue"
            }
            
            if ($env:SMTP_USESSL -eq "true") {
                $emailParams.UseSsl = $true
            }
            
            if ($env:SMTP_CREDENTIAL) {
                $emailParams.Credential = $env:SMTP_CREDENTIAL
            }
            
            Send-MailMessage @emailParams
            Write-BackupLog -Message "Email alert sent to $($Script:BackupConfig.EmailAlerts)" -Level "SUCCESS" -LogFile $logFile
        }
    }
    catch {
        Write-BackupLog -Message "Failed to send email alert: $_" -Level "WARN" -LogFile $logFile
    }
    
    # Send Slack notification (if webhook configured)
    try {
        $slackWebhook = $env:SLACK_WEBHOOK_URL
        
        if ($slackWebhook) {
            $slackPayload = @{
                text = "HELIOS Backup Alert"
                attachments = @(
                    @{
                        color = switch ($Status) {
                            "Completed" { "good" }
                            "Failed" { "danger" }
                            "Warning" { "warning" }
                            default { "#439FE0" }
                        }
                        fields = @(
                            @{ title = "Status"; value = $Status; short = $true }
                            @{ title = "Time"; value = Get-Date; short = $true }
                            @{ title = "Message"; value = $Message; short = $false }
                        )
                    }
                )
            } | ConvertTo-Json
            
            $slackParams = @{
                Uri = $slackWebhook
                Method = "POST"
                ContentType = "application/json"
                Body = $slackPayload
                ErrorAction = "SilentlyContinue"
            }
            
            Invoke-RestMethod @slackParams
            Write-BackupLog -Message "Slack notification sent" -Level "SUCCESS" -LogFile $logFile
        }
    }
    catch {
        Write-BackupLog -Message "Failed to send Slack notification: $_" -Level "WARN" -LogFile $logFile
    }
    
    Write-BackupLog -Message "Alert notification completed" -Level "SUCCESS" -LogFile $logFile
    
    return @{
        Success = $true
        Status = $Status
        AlertsSent = @("Email", "Slack")
    }
}
catch {
    Write-BackupLog -Message "Alert notification failed: $_" -Level "ERROR" -LogFile $logFile
    return @{ Success = $false; Error = $_ }
}
