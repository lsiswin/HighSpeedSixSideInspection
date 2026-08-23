param(
    [string]$Root = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
$methodPattern = '^\s*(?:(?:public|private|protected|internal)\s+(?:(?:static|async|virtual|override|sealed|partial)\s+)*(?:[A-Za-z_][A-Za-z0-9_\.<>\[\],\?]*\s+)+(?<name>[A-Za-z_][A-Za-z0-9_]*)|(?:Task|Task<[^>]+>|ValueTask|ValueTask<[^>]+>|IAsyncEnumerable<[^>]+>)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*))\s*\('
$chinesePattern = '[\u4e00-\u9fff]'
$violations = [System.Collections.Generic.List[string]]::new()

$sourceFiles = Get-ChildItem -LiteralPath (Join-Path $Root 'src'), (Join-Path $Root 'tests') -Filter '*.cs' -Recurse -File |
    Where-Object { $_.FullName -notmatch '[\\/](?:bin|obj)[\\/]' }
foreach ($file in $sourceFiles) {
    $lines = Get-Content -LiteralPath $file.FullName -Encoding UTF8
    for ($index = 0; $index -lt $lines.Count; $index++) {
        $methodMatch = [regex]::Match($lines[$index], $methodPattern)
        if (-not $methodMatch.Success) {
            continue
        }

        $windowStart = [Math]::Max(0, $index - 12)
        $commentWindow = ($lines[$windowStart..([Math]::Max(0, $index - 1))] -join "`n")
        if ($commentWindow -notmatch '///\s*<summary>' -or $commentWindow -notmatch $chinesePattern) {
            $relativePath = [IO.Path]::GetRelativePath($Root, $file.FullName)
            $violations.Add("${relativePath}:$($index + 1) 方法 $($methodMatch.Groups['name'].Value) 缺少中文 XML summary 注释。")
        }
    }
}

if ($violations.Count -gt 0) {
    $violations | ForEach-Object { Write-Host $_ -ForegroundColor Red }
    exit 1
}

Write-Host "中文方法注释检查通过，共扫描 $($sourceFiles.Count) 个 C# 文件。"
