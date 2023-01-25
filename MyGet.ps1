param(
    [string[]]$projects = @(
        "src\Stratis.CodeAnalysis.Cs\Stratis.CodeAnalysis.Cs.Package\Stratis.CodeAnalysis.Cs.Package.csproj"
    ),
    [string[]]$platforms = @(
        "AnyCpu"
    ),
    [string[]]$targetFrameworks = @(
        "v4.7.2"
    ),
    [string]$packageVersion = $null,
    [string]$config = "Debug",
    [string]$target = "Rebuild",
    [string]$verbosity = "Minimal",
    [bool]$clean = $true
)

# Initialization
$currentFolder = Split-Path -parent $script:MyInvocation.MyCommand.Path
$rootFolder = Join-Path $currentFolder ..

. $currentFolder\myget.include.ps1

# MyGet
$packageVersion = MyGet-Package-Version $packageVersion

# Solution
$solutionName = "Stratis.DevEx"
$solutionFolder = Join-Path $currentFolder "src\$solutionName"
$outputFolder = Join-Path $currentFolder "bin"

# Clean
if($clean) { MyGet-Build-Clean $currentFolder }

# Platforms to build for
$platforms | ForEach-Object {
    $platform = $_

    # Projects to build
    $projects | ForEach-Object {
        
        $project = $_
        $buildOutputFolder = Join-Path $outputFolder "$packageVersion\$platform\$config"

        # Build solution for current platform
        MyGet-Build-Solution -sln $solutionFolder\$solutionName.sln `
            -rootFolder $currentFolder `
            -outputFolder $outputFolder `
            -version $packageVersion `
            -config $config `
            -target $target `
            -platforms $platforms `
            -targetFrameworks $targetFrameworks `
            -verbosity $verbosity

        # Build .nupkg
        MyGet-Build-Nupkg -rootFolder $ rootFolder `
            -outputFolder $ buildOutputFolder `
            -project $ project `
            -config $ config `
            version $ packageVersion `
            platform $ platform

    }
}