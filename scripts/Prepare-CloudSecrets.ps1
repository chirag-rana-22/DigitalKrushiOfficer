# Creates only local, ignored deployment settings. Does not sign up, deploy, or contact providers.
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$outputDirectory = Join-Path $projectRoot 'App_Data/cloud'
$outputFile = Join-Path $outputDirectory 'deployment-secrets.json'
if (Test-Path -LiteralPath $outputFile) { throw 'Deployment secrets already exist. Refusing to replace the encryption certificate.' }
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$rsa = [System.Security.Cryptography.RSA]::Create(3072)
try {
    $request = [System.Security.Cryptography.X509Certificates.CertificateRequest]::new('CN=Digital Krushi Officer Data Protection', $rsa, [System.Security.Cryptography.HashAlgorithmName]::SHA256, [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)
    $certificate = $request.CreateSelfSigned([DateTimeOffset]::UtcNow.AddDays(-1), [DateTimeOffset]::UtcNow.AddYears(5))
    try {
        $password = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(36))
        $pfx = $certificate.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx, $password)
        $settings = [ordered]@{
            'ConnectionStrings__DefaultConnection' = ''
            'Database__Provider' = 'MySql'
            'Database__ServerVersion' = ''
            'Hosting__Platform' = 'Render'
            'Hosting__TrustedProxyNetworks__0' = ''
            'AllowedHosts' = ''
            'Storage__Provider' = 'Supabase'
            'Storage__Supabase__Url' = ''
            'Storage__Supabase__Bucket' = 'dko-media'
            'Storage__Supabase__ServiceRoleKey' = ''
            'Email__Provider' = 'Brevo'
            'Email__FromEmail' = ''
            'Email__Brevo__ApiKey' = ''
            'DataProtection__PersistToDatabase' = 'true'
            'DataProtection__CertificateBase64' = [Convert]::ToBase64String($pfx)
            'DataProtection__CertificatePassword' = $password
        }
        $settings | ConvertTo-Json | Set-Content -LiteralPath $outputFile -Encoding utf8
    } finally { $certificate.Dispose() }
} finally { $rsa.Dispose() }
Write-Output "Private deployment settings prepared at $outputFile. Keep a secure backup; do not paste or commit its contents."
