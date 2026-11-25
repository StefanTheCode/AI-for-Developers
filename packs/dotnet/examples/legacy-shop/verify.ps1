<#
.SYNOPSIS
    Builds LegacyShop, starts it in IIS Express and runs a smoke test. Prints a short PASS/FAIL report.

.DESCRIPTION
    Needs Windows with:
      - Visual Studio 2022/2026 with the "ASP.NET and web development" workload (MSBuild + web targets + IIS Express)
      - SQL Server Express LocalDB (installed with that workload)
    Works in Windows PowerShell 5.1 and PowerShell 7.

.EXAMPLE
    .\verify.ps1                 # restore, build, run, test, stop
    .\verify.ps1 -SkipBuild      # only run + test
    .\verify.ps1 -KeepRunning    # leave both sites running afterwards (http://localhost:5100)
#>
param(
    [switch]$SkipBuild,
    [switch]$KeepRunning
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$webPort = 5100
$inventoryPort = 5101
$results = New-Object System.Collections.Generic.List[object]

function Add-Result($name, $ok, $detail) {
    $results.Add([pscustomobject]@{ Check = $name; Result = $(if ($ok) { 'PASS' } else { 'FAIL' }); Detail = $detail })
}

# ---------- 1. Find MSBuild and IIS Express ----------
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path $vswhere)) { throw 'vswhere.exe not found - install Visual Studio with the ASP.NET and web development workload.' }
$msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) { throw 'MSBuild not found.' }

$iisExpress = Join-Path $env:ProgramFiles 'IIS Express\iisexpress.exe'
if (-not (Test-Path $iisExpress)) { $iisExpress = Join-Path ${env:ProgramFiles(x86)} 'IIS Express\iisexpress.exe' }
if (-not (Test-Path $iisExpress)) { throw 'IIS Express not found.' }

# ---------- 2. Restore + build ----------
if (-not $SkipBuild) {
    Write-Host 'Restoring packages (packages.config)...'
    & $msbuild (Join-Path $root 'LegacyShop.sln') -t:restore -p:RestorePackagesConfig=true -v:q -nologo | Out-Null
    Write-Host 'Building...'
    $buildLog = & $msbuild (Join-Path $root 'LegacyShop.sln') -p:Configuration=Debug -v:m -nologo 2>&1
    $errors = $buildLog | Select-String -Pattern ': error ' | ForEach-Object { $_.Line.Trim() } | Select-Object -Unique
    Add-Result 'Build' ($LASTEXITCODE -eq 0) $(if ($LASTEXITCODE -eq 0) { 'msbuild OK' } else { ($errors | Select-Object -First 5) -join ' | ' })
    if ($LASTEXITCODE -ne 0) {
        $results | Format-Table -AutoSize | Out-String -Width 220 | Write-Host
        exit 1
    }
}

# ---------- 3. LocalDB ----------
try {
    & sqllocaldb start MSSQLLocalDB | Out-Null
    Add-Result 'LocalDB' $true 'MSSQLLocalDB running'
} catch {
    Add-Result 'LocalDB' $false 'sqllocaldb start MSSQLLocalDB failed'
}

# ---------- 4. Start both sites ----------
$web = Start-Process $iisExpress -ArgumentList "/path:`"$(Join-Path $root 'LegacyShop.Web')`"", "/port:$webPort", '/systray:false' -PassThru -WindowStyle Hidden
$inventory = Start-Process $iisExpress -ArgumentList "/path:`"$(Join-Path $root 'LegacyShop.Inventory')`"", "/port:$inventoryPort", '/systray:false' -PassThru -WindowStyle Hidden

Add-Type -AssemblyName System.Net.Http
$handler = New-Object System.Net.Http.HttpClientHandler
$handler.AllowAutoRedirect = $false
$http = New-Object System.Net.Http.HttpClient($handler)
$http.Timeout = [TimeSpan]::FromSeconds(90)

function Get-Url($url, $accept) {
    $request = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::Get, $url)
    if ($accept) { $request.Headers.Add('Accept', $accept) }
    $response = $http.SendAsync($request).GetAwaiter().GetResult()
    $body = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    [pscustomobject]@{
        Status      = [int]$response.StatusCode
        Body        = $body
        ContentType = $(if ($response.Content.Headers.ContentType) { $response.Content.Headers.ContentType.MediaType } else { '' })
        Location    = $(if ($response.Headers.Location) { $response.Headers.Location.ToString() } else { '' })
        Timing      = $(if ($response.Headers.Contains('X-Response-Time-ms')) { ($response.Headers.GetValues('X-Response-Time-ms') | Select-Object -First 1) } else { '' })
    }
}

function Test-Page($name, $url, $expectStatus, $mustContain) {
    try {
        $r = Get-Url $url
        $ok = ($r.Status -eq $expectStatus) -and (-not $mustContain -or $r.Body -like "*$mustContain*")
        Add-Result $name $ok ("HTTP {0}{1}" -f $r.Status, $(if ($mustContain -and $r.Body -notlike "*$mustContain*") { " - missing '$mustContain'" } else { '' }))
        return $r
    } catch {
        Add-Result $name $false $_.Exception.GetBaseException().Message
    }
}

try {
    # Warm up the web app FIRST: its Application_Start sets the EF initializer that creates + seeds the database.
    Write-Host 'Waiting for the web app (first request creates and seeds the database)...'
    $deadline = (Get-Date).AddSeconds(120)
    do {
        try { $warm = Get-Url "http://localhost:$webPort/"; if ($warm.Status -eq 200) { break } } catch { }
        Start-Sleep -Seconds 2
    } while ((Get-Date) -lt $deadline)

    $base = "http://localhost:$webPort"
    $homePage = Test-Page 'Home page (Default.aspx)' "$base/" 200 'Products'
    if ($homePage) { Add-Result 'HTTP module header X-Response-Time-ms' ([bool]$homePage.Timing) $(if ($homePage.Timing) { "$($homePage.Timing) ms" } else { 'header missing' }) }
    Test-Page 'Category filter (?cat=2)' "$base/Default.aspx?cat=2" 200 'products' | Out-Null
    Test-Page 'Product page' "$base/Product.aspx?id=1" 200 'Add to cart' | Out-Null
    Test-Page 'Product not found = 404' "$base/Product.aspx?id=99999" 404 'Product not found' | Out-Null
    Test-Page 'Cart page' "$base/Cart.aspx" 200 'Your cart' | Out-Null
    Test-Page 'MVC report' "$base/Reports" 200 'Sales' | Out-Null

    $img = Get-Url "$base/Handlers/ProductImage.ashx?id=1"
    Add-Result 'Image handler (.ashx)' ($img.Status -eq 200 -and $img.ContentType -eq 'image/png') ("HTTP {0} {1}" -f $img.Status, $img.ContentType)

    $api = Get-Url "$base/api/products" 'application/json'
    $apiOk = $api.Status -eq 200 -and $api.Body -like '*"name"*'
    Add-Result 'Web API list (camelCase JSON)' $apiOk ("HTTP {0}" -f $api.Status)
    $apiXml = Get-Url "$base/api/products/1" 'application/xml'
    Add-Result 'Web API XML negotiation' ($apiXml.Status -eq 200 -and $apiXml.ContentType -like '*xml*') ("HTTP {0} {1}" -f $apiXml.Status, $apiXml.ContentType)
    Test-Page 'Web API 404' "$base/api/products/99999" 404 $null | Out-Null

    $orders = Get-Url "$base/api/orders/mine" 'application/json'
    Add-Result 'Web API [Authorize] without login = 302 (Forms auth quirk)' ($orders.Status -eq 302) ("HTTP {0} -> {1}" -f $orders.Status, $orders.Location)
    $admin = Get-Url "$base/Admin/Products.aspx"
    Add-Result 'Admin without login = 302 to login' ($admin.Status -eq 302 -and $admin.Location -like '*Login.aspx*') ("HTTP {0}" -f $admin.Status)

    Test-Page 'WCF service WSDL' "http://localhost:$inventoryPort/InventoryService.svc?wsdl" 200 'InventoryService' | Out-Null
    $stock = Get-Url "$base/api/products/1/stock" 'application/json'
    Add-Result 'Web -> WCF call (stock)' ($stock.Status -eq 200 -and $stock.Body -like '*available*') ("HTTP {0}" -f $stock.Status)
}
finally {
    $http.Dispose()
    if (-not $KeepRunning) {
        foreach ($p in @($web, $inventory)) { if ($p -and -not $p.HasExited) { Stop-Process -Id $p.Id -Force } }
    } else {
        Write-Host "Sites are still running: http://localhost:$webPort and http://localhost:$inventoryPort/InventoryService.svc"
    }
}

# ---------- 5. Report ----------
$passed = ($results | Where-Object Result -eq 'PASS').Count
$report = @()
$report += "LegacyShop verify - $(Get-Date -Format s) - $passed/$($results.Count) passed"
$report += ($results | Format-Table -AutoSize | Out-String -Width 220).TrimEnd()
$report -join [Environment]::NewLine | Tee-Object -FilePath (Join-Path $root 'verify-result.txt') | Write-Host
Write-Host ''
Write-Host 'Paste verify-result.txt back into the chat if anything failed.'
if ($passed -ne $results.Count) { exit 1 }
