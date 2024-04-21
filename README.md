<div align="center">

## CessilCellsCeaChells
[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/wwwDayDream/CessilCellsCeaChells/build.yml?style=plastic&logo=github)]()
[![Nuget Package Version](https://img.shields.io/nuget/v/CessilCellsCeaChells?style=plastic&logo=nuget&color=%23004880)](https://www.nuget.org/packages/CessilCellsCeaChells)
[![Thunderstore Package Version](https://img.shields.io/thunderstore/v/www_Day_Dream/CessilCellsCeaChells?style=plastic&logo=thunderstore&color=%233498db)](https://thunderstore.io/c/content-warning/p/www_Day_Dream/CessilCellsCeaChells/)
</div>
Allows installed plugins to request creation of Fields, Properties, and Methods in Managed DLLs.

### Technical Jargon
A BepInEx 5.4.21 patcher that scans all installed plugins for assembly attributes that inform us of any desired Fields, Properties, and Methods to inject on the `/Game_Data/Managed/` DLLs.

### Usage & Documentation
The wiki contains a [Getting Started](https://github.com/wwwDayDream/CessilCellsCeaChells/wiki) section as well as usages of assembly attributes.

## Features
- Injecting Public Instance Properties on any type in `/Game_Data/Managed/*.dll`
- Injecting Private Instance Fields on any type in `/Game_Data/Managed/*.dll`
- Injecting Public Methods on any type in `/Game_Data/Managed/*.dll` with any parameters and/or return type.

### Publishing To New Thunderstore Communities
- Create an issue for the specific community OR
- Create a pull request with the implementation into the [TCLI TOML](https://github.com/wwwDayDream/CessilCellsCeaChells/blob/master/CessilCellsCeaChells/ts-assets/thunderstore.toml).

### Version Compatability
[BepInEx](https://github.com/BepInEx/BepInEx/) - v5.4.*
