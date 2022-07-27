﻿param([string]$exe_path)

$root = (Get-Item $exe_path).Directory.FullName
$folder_resources = Join-Path $root "bin" "resources" 
$folder_runtimes = Join-Path $root "bin" "runtimes" 

./DotNetDllPathPatcher.ps1 $exe_path
if ($LASTEXITCODE -ne 0) {
    exit -1
}

Move-Item $folder_resources $root
Move-Item $folder_runtimes $root
