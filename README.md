# A agent that solve a game


## Usage

create secret base on the `.env`

```bash
dotnet user-secrets init --project WebConnection
```

for PowerShell

```bash
./load-env.ps1 WebConnection
```

for linux

```bash
chmod +x load-env.sh
./load-env.sh WebConnection
```

```bash
dotnet run --project WebConnection
```


```bash
docker run --rm -it -p 18888:18888 -p 4317:18889 -d --name aspire-dashboard -e ASPIRE_ALLOW_UNSECURED_TRANSPORT=true -e DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true mcr.microsoft.com/dotnet/aspire-dashboard:8.0.0
```
