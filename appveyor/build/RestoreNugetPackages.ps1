Write-Host "Downloading NuGet..."
$webclient = New-Object System.Net.WebClient
$url = "https://dist.nuget.org/win-x86-commandline/v4.1.0/nuget.exe"
$path = "$ENV:APPVEYOR_BUILD_FOLDER\nuget.exe"
$webclient.DownloadFile($url, $path)

Write-Host "Restoring NuGet packages..."
& $path restore "$ENV:APPVEYOR_BUILD_FOLDER\wallabag.sln"