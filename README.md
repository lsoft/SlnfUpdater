# SlnfUpdater

An utility which updates your `slnf` files with new `csproj`es. Only `csproj` supported now, `shproj` and `vcxproj` are skipped during analysis.

## How to use:

```
SlnfUpdater.exe "path to your slnf folder" "mask for your slnf files"
```

for example:

```
SlnfUpdater.exe "." "*.slnf"
```

## Participating

Bug reports with repro and PRs are appreciated.
