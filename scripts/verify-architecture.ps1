$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$allowed = @{
    'Domain' = @(); 'Application' = @('Domain');
    'Infrastructure' = @('Application','Domain');
    'Api' = @('Application','Infrastructure','Contracts');
    'Web' = @('Contracts'); 'Contracts' = @()
}
$projects = @(Get-ChildItem "$root/backend","$root/frontend","$root/shared" -Recurse -Filter *.csproj)
if ($projects.Count -ne 6) { throw 'Esperados seis projetos de produção na arquitetura.' }
foreach ($project in $projects) {
    $name = $project.BaseName.Replace('GestaoComercial.','')
    [xml]$xml = Get-Content $project.FullName
    foreach ($reference in $xml.SelectNodes('//ProjectReference')) {
        $target = [IO.Path]::GetFileNameWithoutExtension($reference.Include).Replace('GestaoComercial.','')
        if ($target -notin $allowed[$name]) { throw "Referência proibida: $name -> $target" }
    }
    if ($name -in @('Domain','Application','Contracts') -and $xml.SelectNodes('//PackageReference').Count -gt 0) { throw "Pacote indevido em $name" }
}
Write-Output 'Referências de arquitetura válidas.'
