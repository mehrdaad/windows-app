Add-Type -Assembly System.IO.Compression.FileSystem

$rootPath = $ENV:APPVEYOR_BUILD_FOLDER;
$versionNumber = $ENV:APPVEYOR_BUILD_VERSION;
$platforms = "x86|x64|ARM".Split("|");
$completeVersionNumber = "$versionNumber.0";

$appSymbolsDirectory = [System.IO.Path]::Combine($rootPath, "src\wallabag\AppPackages\wallabag_$($versionNumber).0_Test\");
$finalSymbolRootDirectory = [System.IO.Path]::Combine($rootPath, "hockeyapp-symbols");

if ([System.IO.DirectoryInfo]::new($finalSymbolRootDirectory).Exists -eq $false)
{
    [System.IO.Directory]::CreateDirectory($finalSymbolRootDirectory);

    foreach ($platform in $platforms)
    {
        $resultSymbolDirectory = [System.IO.Directory]::CreateDirectory("$($finalSymbolRootDirectory)\$($platform)");
        $librariesSymbolDirectory = [System.IO.Path]::Combine($rootPath, "src\wallabag\obj\$($platform)\Release\ilc\in\");
               
        Write-Host "Extracting $($platform) symbols for application...";

        $appSymFile = [System.IO.Path]::Combine($appSymbolsDirectory, "wallabag_$($versionNumber).0_$($platform).appxsym");
  
        $zipFile = [System.IO.Compression.ZipFile]::ExtractToDirectory($appSymFile, $resultSymbolDirectory.FullName);

        Write-Host "Copying symbols for libraries...";        
        Copy-Item -Path "$($librariesSymbolDirectory)\*" -Destination "$($resultSymbolDirectory.FullName)" -Filter "*.pdb" -Exclude "wallabag.pdb";

        Write-Host "Creating ZIP file for HockeyApp...";
        [System.IO.Compression.ZipFile]::CreateFromDirectory($resultSymbolDirectory.FullName, "$($finalSymbolRootDirectory)\symbols-$($platform).zip");
    }
}
else
{
    Write-Host "Symbols directory already exists. Aborting." -ForegroundColor Red
}