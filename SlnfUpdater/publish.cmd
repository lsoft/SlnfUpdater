@if exist publish rmdir /s /q publish
@dotnet publish SlnfUpdater.sln -p:PublishProfile=FolderProfile --nologo --verbosity q