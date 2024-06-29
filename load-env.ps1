# Load environment variables from .env file and set them as user secrets
$envFile = ".env"
if (Test-Path $envFile) {
    Get-Content $envFile | ForEach-Object {
        if ($_ -match "^\s*([^=]+)\s*=\s*(.+?)\s*$") {
            $key = $matches[1].Trim()
            $value = $matches[2].Trim()
            dotnet user-secrets set $key $value --project WebConnection
        }
    }
} else {
    Write-Host ".env file not found."
}