Add-Type -Assembly System.IO.Compression.FileSystem

$uploadDirectory = "$ENV:APPVEYOR_BUILD_FOLDER\src\wallabag\AppPackages"

$tenantID = $ENV:STORE_TENANT_ID
$clientID = $ENV:STORE_CLIENT_ID
$clientSecret = $ENV:STORE_CLIENT_SECRET
$appId = $ENV:STORE_APP_ID
$flightId = $ENV:STORE_FLIGHT_ID

$authorizationHeader = @{ "Authorization" = "Bearer $accessToken" }
$jsonContentType = "application/json"

Write-Host "Creating ZIP file with relevant files for upload..."
$temporaryZIPDirectory = [IO.Directory]::CreateDirectory("$uploadDirectory\store_upload")
$appxUploadFile = Get-Item (Resolve-Path ([IO.Path]::Combine($uploadDirectory, "*.appxupload")))
Move-Item -Path $appxUploadFile -Destination $temporaryZIPDirectory.FullName
[IO.Compression.ZipFile]::CreateFromDirectory($temporaryZIPDirectory.FullName, "$uploadDirectory\upload.zip")
$uploadFile = Get-Item "$uploadDirectory\upload.zip"
$appxUploadFile = Get-Item (Resolve-Path ([IO.Path]::Combine($temporaryZIPDirectory.FullName, "*.appxupload")))

Write-Host "Fetching the client secret..."
$loginUrl = "https://login.microsoftonline.com/$tenantID/oauth2/token"
$loginString = (
    "grant_type=client_credentials",
    "client_id=$clientID",
    "client_secret=$clientSecret",
    "resource=https://manage.devcenter.microsoft.com"
) -join "&"
$accessToken = (Invoke-RestMethod -Method POST -Uri $loginUrl -Body $loginString).access_token

$submissionId = 0
$currentFlightPackages = Invoke-RestMethod -Uri "https://manage.devcenter.microsoft.com/v1.0/my/applications/$appId/flights/$flightId" -Method Get -Headers $authorizationHeader
if ($currentFlightPackages.PSobject.Properties.Name -contains "pendingFlightSubmission")
{
    Write-Host "Pending flight package already exists. Checking status for further actions..."
    $submissionId = $currentFlightPackages.pendingFlightSubmission.id
    $submissionStatus = Invoke-RestMethod -Method Get -Uri "https://manage.devcenter.microsoft.com/v1.0/my/applications/$appId/flights/$flightId/submissions/$submissionId/status" -Headers $authorizationHeader
    
    if ($submissionStatus.status -eq "PendingCommit")
    {
        Write-Host "Deleting pending submission..."
        $deleteResponse  = Invoke-RestMethod -Method Delete -Uri "https://manage.devcenter.microsoft.com/v1.0/my/applications/$appId/flights/$flightId/submissions/$submissionId" -Headers $authorizationHeader
    }
    else
    {
        Write-Host $submissionStatus.status
    }
}

Write-Host "Creating a new flight package..."
$createFlightUrl = "https://manage.devcenter.microsoft.com/v1.0/my/applications/$appId/flights/$flightId/submissions"
$flightResult = Invoke-RestMethod -Uri $createFlightUrl -Method Post -Headers $authorizationHeader
$submissionId = $flightResult.Id

$resultedUploadUrl = $flightResult.fileUploadUrl

Write-Host "Updating flight package with proper parameters..."
[System.Collections.ArrayList]$flightPackages = $flightResult.flightPackages

foreach ($package in $flightPackages)
{
    $package.fileStatus = "PendingDelete"
}

$flightPackages.Add(@{
    "fileName" = $appxUploadFile.Name
    "fileStatus" = "PendingUpload"
    "minimumDirectXVersion" ="None"
    "minimumSystemRam"= "None"
})

$flightParameters = @{
    "flightPackages" = $flightPackages
    "packageDeliveryOptions" = @{
        "packageRollout" = @{
            "isPackageRollout" = $false
            "packageRolloutPercentage" = 0
            "packageRolloutStatus" = "PackageRolloutNotStarted"
            "fallbackSubmissionId" = "0"
        }
        "isMandantoryUpdate" = $false
        "mandatoryUpdateEffectiveDate" = "1601-01-01T00:00:00.0000000Z"
    }
    "targetPublishMode" = "Immediate"
    "targetPublishDate" = ""
    "notesForCertification" = "No special steps are required for certification of this app."
}
$updateUrl = "https://manage.devcenter.microsoft.com/v1.0/my/applications/$appId/flights/$flightId/submissions/$submissionId"
$updateResponse = Invoke-RestMethod -Uri $updateUrl -Method PUT -Headers $authorizationHeader -ContentType $jsonContentType -Body (ConvertTo-Json -InputObject $flightParameters -Compress -Depth 99)

Write-Host "Uploading app to Windows Store..."
$resultedUploadUrl = $resultedUploadUrl.Replace("+", "%2B")
$uploadResponse = Invoke-RestMethod -Uri $resultedUploadUrl -Method PUT -Headers @{ "x-ms-blob-type" = "BlockBlob" } -InFile $uploadFile.FullName

Write-Host "Commiting changes..."
$commitUrl = "https://manage.devcenter.microsoft.com/v1.0/my/applications/$appId/flights/$flightId/submissions/$submissionId/commit"
$commitResponse = Invoke-RestMethod -Uri $commitUrl -Method POST -Headers $authorizationHeader -ContentType $jsonContentType