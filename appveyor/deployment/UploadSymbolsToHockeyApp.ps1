$completeVersionNumber = "$ENV:APPVEYOR_BUILD_VERSION.0"

$finalSymbolRootDirectory = [System.IO.Path]::Combine($rootPath, "hockeyapp-symbols")

$HockeyAppAppID = $ENV:HOCKEYAPP_APP_ID
$HockeyAppApiToken = $ENV:HOCKEYAPP_API_TOKEN
 
Write-Host "Creating new HockeyApp version..."
$create_url = "https://rink.hockeyapp.net/api/2/apps/$HockeyAppAppID/app_versions/new"
$response = Invoke-RestMethod -Method POST -Uri $create_url  -Header @{ "X-HockeyAppToken" = $HockeyAppApiToken } -Body @{bundle_version = $completeVersionNumber}
$update_url = "https://rink.hockeyapp.net/api/2/apps/$($HockeyAppAppID)/app_versions/$($response.id)"

$symbolFiles = Get-ChildItem $finalSymbolRootDirectory -Filter "*.zip"

Write-Host "Uploading symbols to HockeyApp..."
foreach ($zipFile in $symbolFiles)
{
    Write-Host "Uploading $zipFile to HockeyApp...";
    $fileBin = [IO.File]::ReadAllBytes($zipFile.FullName)
    $enc = [System.Text.Encoding]::GetEncoding("ISO-8859-1")
    $fileEnc = $enc.GetString($fileBin)
    $boundary = [System.Guid]::NewGuid().ToString()
    $LF = "`r`n"

    $bodyLines = (
        "--$boundary",
	    "content-transfer-encoding: base64",
	    "Content-Disposition: form-data; content-transfer-encoding: `"base64`"; name=`"dsym`"; filename=`"$([System.IO.Path]::GetFileName($zipFile))`"$LF",$fileEnc,
        "--$boundary",
        "Content-Disposition: form-data; name=`"status`"$LF","2",
        "--$boundary--$LF") -join $LF
	
    Invoke-RestMethod -Uri $update_url -Method PUT -Headers @{ "X-HockeyAppToken" = $HockeyAppApiToken } -ContentType "multipart/form-data; boundary=`"$boundary`"" -Body $bodyLines
}