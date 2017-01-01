$manifestfile = Get-Item -Path "$ENV:APPVEYOR_BUILD_FOLDER\src\wallabag\Package.appxmanifest"
  
$manifestXml = New-Object -TypeName System.Xml.XmlDocument
$manifestXml.Load($manifestfile.Fullname)

$updatedVersion = [Version]"$ENV:APPVEYOR_BUILD_VERSION.0";

$manifestXml.Package.Identity.Version = [String]$updatedVersion
$manifestXml.save($manifestfile.FullName)