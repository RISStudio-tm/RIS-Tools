@ECHO OFF

ECHO any section

dotnet publish "CopyPackage\CopyPackage.csproj" -c "Release" -p:PublishProfile="netcore3.1 any.pubxml"
dotnet publish "PublishPackages\PublishPackages.csproj" -c "Release" -p:PublishProfile="netcore3.1 any.pubxml"

