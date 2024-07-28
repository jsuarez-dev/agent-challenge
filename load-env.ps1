# Load environment variables from .env file and set them as user secrets

$projectName = $args[0]

if (!$projectName){
    Write-Host "select project first"
    exit 1
}

$envFile = ".env"
if (Test-Path $envFile) {
    Get-Content $envFile | ForEach-Object {
        if ($_ -match "^\s*([^=]+)\s*=\s*(.+?)\s*$") {
            $key = $matches[1].Trim()
            $value = $matches[2].Trim()
            dotnet user-secrets set $key $value --project $projectName
        }
    }
} else {
    Write-Host ".env file not found."
}