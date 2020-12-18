$service_name = "ipasvc"

$service = Get-WmiObject -Class Win32_Service -Filter "Name='$service_name'"

if ($service) {
    $service.StopService() | Out-Null
    $service.delete() | Out-Null
}

Remove-EventLog -Source $service_name -ErrorAction SilentlyContinue