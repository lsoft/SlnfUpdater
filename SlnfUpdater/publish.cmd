@if exist publish del publish
@dotnet publish SlnfUpdater.sln -p:PublishProfile=FolderProfile --nologo --verbosity q