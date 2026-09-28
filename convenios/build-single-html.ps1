param(
    [string]$OutputFile = "convenios-financieros.html"
)

$ErrorActionPreference = "Stop"

$sourceDirectory = $PSScriptRoot
$templatePath = Join-Path $sourceDirectory "index.html"
$outputPath = Join-Path $sourceDirectory $OutputFile
$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)

function Read-Utf8File([string]$relativePath) {
    $path = Join-Path $sourceDirectory $relativePath
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
}

$html = Read-Utf8File "index.html"
$css = Read-Utf8File "css/app.css"

$html = $html.Replace(
    '  <link rel="stylesheet" href="css/app.css">',
    "  <style>`n$css`n  </style>"
)

$scriptTags = '  <script src="js/config.js"></script><script src="js/api.js"></script><script src="js/convenios.js"></script><script src="js/movimientos.js"></script><script src="js/app.js"></script>'
$scripts = @(
    "js/config.js",
    "js/api.js",
    "js/convenios.js",
    "js/movimientos.js",
    "js/app.js"
) | ForEach-Object {
    "/* $_ */`n$(Read-Utf8File $_)"
}

$inlineScripts = "  <script>`n$($scripts -join "`n`n")`n  </script>"
$html = $html.Replace($scriptTags, $inlineScripts)

if ($html.Contains('href="css/app.css"') -or $html.Contains('src="js/')) {
    throw "No fue posible reemplazar todas las dependencias locales del HTML."
}

[System.IO.File]::WriteAllText($outputPath, $html, $utf8WithoutBom)
Write-Output "Archivo generado: $outputPath"
