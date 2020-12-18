$domain = Get-ADDomain
$ldap = "LDAP://" + ($domain.DistinguishedName).Replace(",DC=", ".").Replace("DC=", "")

Write-Host "'domain' = $($domain.NetBIOSName) [alternative: $(([ADSI]$ldap).dc)]"
Write-Host "'context' = $($domain.DistinguishedName)"