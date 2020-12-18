$service_name = "ipasvc"
$eventlog_name = "Improsec Password Auditor"

& "$PSScriptRoot\UninstallService.ps1"

New-EventLog -Source $service_name -LogName $eventlog_name
New-Service -Name $service_name -BinaryPathName "$PSScriptRoot\$service_name" -DisplayName "Improsec Password Auditor service" -StartupType Automatic | Out-Null