param(
    [int]$Port = 8088
)

$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath((Split-Path -Parent $MyInvocation.MyCommand.Path))

function Get-ContentType {
    param([string]$Path)

    switch ([System.IO.Path]::GetExtension($Path).ToLowerInvariant()) {
        '.json' { 'application/json' }
        '.zip' { 'application/zip' }
        '.md' { 'text/markdown; charset=utf-8' }
        '.txt' { 'text/plain; charset=utf-8' }
        '.html' { 'text/html; charset=utf-8' }
        default { 'application/octet-stream' }
    }
}

function Write-Response {
    param(
        [System.Net.Sockets.TcpClient]$Client,
        [int]$StatusCode,
        [string]$StatusText,
        [byte[]]$Body,
        [string]$ContentType
    )

    $stream = $Client.GetStream()
    $header = "HTTP/1.1 $StatusCode $StatusText`r`nContent-Type: $ContentType`r`nContent-Length: $($Body.Length)`r`nConnection: close`r`n`r`n"
    $headerBytes = [System.Text.Encoding]::ASCII.GetBytes($header)
    $stream.Write($headerBytes, 0, $headerBytes.Length)
    if ($Body.Length -gt 0) {
        $stream.Write($Body, 0, $Body.Length)
    }
    $stream.Flush()
}

$listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Any, $Port)
$listener.Start()
Write-Host "Serving update files from $root on http://0.0.0.0:$Port/"

while ($true) {
    $client = $listener.AcceptTcpClient()
    try {
        $stream = $client.GetStream()
        $buffer = [byte[]]::new(8192)
        $read = $stream.Read($buffer, 0, $buffer.Length)
        if ($read -le 0) {
            continue
        }

        $request = [System.Text.Encoding]::ASCII.GetString($buffer, 0, $read)
        $firstLine = ($request -split "`r?`n", 2)[0]
        if ($firstLine -notmatch '^GET\s+([^\s]+)\s+HTTP/') {
            $body = [System.Text.Encoding]::UTF8.GetBytes('Only GET requests are supported.')
            Write-Response -Client $client -StatusCode 405 -StatusText 'Method Not Allowed' -Body $body -ContentType 'text/plain; charset=utf-8'
            continue
        }

        $requestPath = $Matches[1]
        $requestPath = ($requestPath -split '\?', 2)[0]
        if ([string]::IsNullOrWhiteSpace($requestPath) -or $requestPath -eq '/') {
            $requestPath = '/update-manifest.json'
        }

        $relativePath = [Uri]::UnescapeDataString($requestPath.TrimStart('/')).Replace('/', [System.IO.Path]::DirectorySeparatorChar)
        $filePath = [System.IO.Path]::GetFullPath((Join-Path $root $relativePath))

        if (-not $filePath.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase) -or -not (Test-Path -LiteralPath $filePath -PathType Leaf)) {
            $body = [System.Text.Encoding]::UTF8.GetBytes('File not found.')
            Write-Response -Client $client -StatusCode 404 -StatusText 'Not Found' -Body $body -ContentType 'text/plain; charset=utf-8'
            continue
        }

        $bodyBytes = [System.IO.File]::ReadAllBytes($filePath)
        Write-Response -Client $client -StatusCode 200 -StatusText 'OK' -Body $bodyBytes -ContentType (Get-ContentType -Path $filePath)
    }
    catch {
        try {
            $body = [System.Text.Encoding]::UTF8.GetBytes("Server error: $($_.Exception.Message)")
            Write-Response -Client $client -StatusCode 500 -StatusText 'Internal Server Error' -Body $body -ContentType 'text/plain; charset=utf-8'
        }
        catch {
        }
    }
    finally {
        $client.Close()
    }
}
