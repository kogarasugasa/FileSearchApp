# 履歴の全件が $historyに含まれています
$history = Import-Csv -Path 'C:\Users\wahuk\AppData\Local\Temp\FileSearchApp_PowerShellSearch_all.csv'
# $filterdPathsが検索結果として FileSearchApp に渡されます
$filterdPaths = New-Object 'System.Collections.Generic.List[string]'
#
#
# これより下を編集して履歴を絞り込んでください。CSV項目「FileName Extension FilePath」


$history = $history | Sort-Object FileName
$isFirstAdd = $true
for ($index = 0; $index -lt ($history.Length - 1); $index++){
    $isExclude = $false
    switch -Exact ($history[$index].Extension.ToLower())
    {
        '.jpg'  { $isExclude = $true }
        '.jpeg' { $isExclude = $true }
        '.png'  { $isExclude = $true }
        '.gif'  { $isExclude = $true }
        '.tiff' { $isExclude = $true }
        '.tif'  { $isExclude = $true }
        '.ogg'  { $isExclude = $true }
        '.pak'  { $isExclude = $true }
        '.txt'  { $isExclude = $true }
        '.mhm'  { $isExclude = $true }
        '.log'  { $isExclude = $true }
        '.json' { $isExclude = $true }
        '.ini'  { $isExclude = $true }
        '.sh'   { $isExclude = $true }
        '.html' { $isExclude = $true }
        '.xml'  { $isExclude = $true }
        '.db'   { $isExclude = $true }
        '.cnf'  { $isExclude = $true }
        '.conf' { $isExclude = $true }
        '.css'  { $isExclude = $true }
        '.dll'  { $isExclude = $true }
        '.mst'  { $isExclude = $true }
        '.aspx' { $isExclude = $true }
        '.gst' { $isExclude = $true }
        '.ress' { $isExclude = $true }
        '.egb' { $isExclude = $true }
        '.dat' { $isExclude = $true }
    }
    if ($isExclude){
        continue
    }
    $curName = $history[$index].FileName
    if ($curName.EndsWith('(1)') -or $curName.EndsWith('(2)') -or $curName.EndsWith('(3)') -or $curName.EndsWith('(4)')){
        $curName = $curName.Substring(0, $curName.Length - 3).Trim()
    }
    $nxtName = $history[$index + 1].FileName
    if ($nxtName.EndsWith('(1)') -or $nxtName.EndsWith('(2)') -or $nxtName.EndsWith('(3)') -or $nxtName.EndsWith('(4)')){
        $nxtName = $nxtName.Substring(0, $nxtName.Length - 3).Trim()
    }
    if ($curName.Equals('')){
        continue
    }
    if ($curName -like '新しいフォルダー'){
        continue
    }
    if ($curName.Equals($nxtName)){
        $filterdPaths.Add($history[$index + 1].FilePath)
        if ($isFirstAdd){
            $filterdPaths.Add($history[$index].FilePath)
            $isFirstAdd = $false
        }
    }
    else {
        $isFirstAdd = $true
    }
}


# これより上を編集して履歴を絞り込んでください
#
#
echo $filterdPaths > 'C:\Users\wahuk\AppData\Local\Temp\FileSearchApp_PowerShellSearch_result.csv'