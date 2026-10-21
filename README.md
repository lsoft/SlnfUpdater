# SlnfUpdater

An utility which updates your `slnf` files with new `csproj`es. Non-existing `csproj`es will be removed from the `slnf`.

Only `csproj` supported now, `shproj` and `vcxproj` are skipped during analysis.

## Requirements

.NET SDK must be installed: the utility evaluates projects with MSBuild of the installed SDK (the newest one, or the one selected by `global.json` of the current folder).

## How to use:

```
SlnfUpdater.exe "path to your slnf folder" "mask for your slnf files"
```

for example:

```
SlnfUpdater.exe "." "*.slnf"
```

Also, you can enable rebuild-from-roots mode, in which all projects except the roots will be removed from the `slnf` before usual reference processing, so projects which are no longer referenced by the roots disappear:

```
SlnfUpdater.exe "." "*.slnf" "-rebuild-from-roots" "-additional-roots:*ProjectA.csproj;*ProjectB.csproj" "-additional-roots:*ProjectC.csproj"
```

`-rebuild-from-roots` enables this mode. `-additional-roots` is a wildcard list of your roots joined with `;`. You can define many `-additional-roots` keys if you want; at least one root is required.
Wildcards are matched against the full path of a project (so they usually start with `*`), case-insensitively on Windows. If no root is found in a `slnf`, that `slnf` keeps its projects.

A `slnf` file is written only when its project list has changed and the whole file was processed successfully. Exit code is `0` on success and `1` if arguments are invalid or any `slnf` failed.

## Participating

Bug reports with repro and PRs are appreciated.
