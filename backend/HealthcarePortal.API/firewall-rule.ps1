# Run this PowerShell script as Administrator
New-NetFirewallRule -DisplayName "Healthcare API HTTP" -Direction Inbound -Protocol TCP -LocalPort 57157 -Action Allow
New-NetFirewallRule -DisplayName "Healthcare API HTTPS" -Direction Inbound -Protocol TCP -LocalPort 57156 -Action Allow

Write-Host "Firewall rules created for ports 57156 and 57157"
