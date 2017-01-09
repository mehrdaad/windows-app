Add-Type -Assembly System.IO.Compression.FileSystem

$rootPath = $ENV:APPVEYOR_BUILD_FOLDER
$versionNumber = $ENV:APPVEYOR_BUILD_VERSION
$platforms = "x86|x64|ARM".Split("|")

$appSymbolsDirectory = Resolve-Path ([System.IO.Path]::Combine($rootPath, "src\wallabag\AppPackages\*_Test\"))
$finalSymbolRootDirectory = [System.IO.Path]::Combine($rootPath, "hockeyapp-symbols")

if ([System.IO.DirectoryInfo]::new($finalSymbolRootDirectory).Exists -eq $false)
{
    [System.IO.Directory]::CreateDirectory($finalSymbolRootDirectory)

    foreach ($platform in $platforms)
    {
        $resultSymbolDirectory = [System.IO.Directory]::CreateDirectory("$finalSymbolRootDirectory\$platform")
        $librariesSymbolDirectory = [System.IO.Path]::Combine($rootPath, "src\wallabag\obj\$platform\Release\ilc\in\")
               
        Write-Host "Extracting $platform symbols for application..."
		$appSymFile = Get-ChildItem -Path $appSymbolsDirectory -Filter "wallabag_*_$platform.appxsym" | Select-Object -First 1
        [System.IO.Compression.ZipFile]::ExtractToDirectory($appSymFile.FullName, $resultSymbolDirectory.FullName)

        Write-Host "Copying symbols for libraries..."
        Copy-Item -Path "$librariesSymbolDirectory\*" -Destination "$($resultSymbolDirectory.FullName)" -Filter "*.pdb" -Exclude "wallabag.pdb"

        Write-Host "Creating ZIP file for HockeyApp..."
        $zipFilePath = "$finalSymbolRootDirectory\symbols-$platform.zip"
        [System.IO.Compression.ZipFile]::CreateFromDirectory($resultSymbolDirectory.FullName, $zipFilePath)
        
        Push-AppveyorArtifact $zipFilePath;
    }

    Write-Host "Pushing appxbundle to AppVeyor artifacts..."
    $appxbundleFile = Get-ChildItem -Path $appSymbolsDirectory -Filter "wallabag*.appxbundle" | Select-Object -First 1
    Push-AppveyorArtifact $appxbundleFile.FullName
}
else
{
    Write-Host "Symbols directory already exists. Aborting." -ForegroundColor Red
}