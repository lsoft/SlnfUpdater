# SlnfUpdater

An utility which updates your `slnf` files with new `csproj`es. Non-existing `csproj`es will be removed from the `slnf`.

Only `csproj` supported now, `shproj` and `vcxproj` are skipped during analysis.

## How to use:

```
SlnfUpdater.exe "path to your slnf folder" "mask for your slnf files"
```

for example:

```
SlnfUpdater.exe "." "*.slnf"
```

Also, you can enable rebuild-from-roots mode, in which all non-root projects from the `slnf` will be removed before usual reference processing:

```
SlnfUpdater.exe "." "*.slnf" "-rebuild-from-roots" "-additional-roots:*ProjectA.csproj;*ProjectB.csproj" "-additional-roots:*ProjectC.csproj"
```

`-rebuild-from-roots` enables this mode. `-additional-roots` is a wildcard list of your roots joined with `;`. You can define many `-additional-roots` keys if you want.

## Participating

Bug reports with repro and PRs are appreciated.
