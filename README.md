<div align="center">

## CessilCellsCeaChells
[![Thunderstore Package Version](https://img.shields.io/thunderstore/v/www_Day_Dream/CessilCellsCeaChells?style=plastic&logo=thunderstore&color=%233498db&label=TS)](https://thunderstore.io/c/content-warning/p/www_Day_Dream/CessilCellsCeaChells/)
[![GitHub (CLI) Release Version](https://img.shields.io/github/v/release/wwwDayDream/CessilCellsCeaChells?style=plastic&logo=github&color=%233498db&label=CLI)]()
[![Nuget CCCC Package Version](https://img.shields.io/nuget/v/CessilCellsCeaChells?style=plastic&logo=nuget&color=%23004880&label=CCCC)](https://www.nuget.org/packages/CessilCellsCeaChells)
[![Nuget MSBuild Package Version](https://img.shields.io/nuget/v/CessilCellsCeaChells.MSBuild?style=plastic&logo=nuget&color=%23004880&label=MSBuild)](https://www.nuget.org/packages/CessilCellsCeaChells.MSBuild)

[![Github Commits Since Release](https://img.shields.io/github/commits-since/wwwDayDream/CessilCellsCeaChells/latest?style=plastic&logo=github&color=%23995500)]()
</div>
Allows installed plugins to request creation of Fields, Properties, Methods, and more in Managed DLLs.

### Technical Jargon
A BepInEx 5.4.21 patcher that scans all installed plugins for assembly attributes that inform us of any desired Fields, Properties, Methods, and more to inject on the `/Game_Data/Managed/` DLLs.

### Usage & Documentation
The wiki contains a [Getting Started](https://github.com/wwwDayDream/CessilCellsCeaChells/wiki) section as well as usages of assembly attributes.

## Features
- Injecting Public Instance Properties on any type in `/Game_Data/Managed/*.dll`
- Injecting Private Instance Fields on any type in `/Game_Data/Managed/*.dll`
- Injecting Public Methods on any type in `/Game_Data/Managed/*.dll` with any parameters and/or return type.

### Version Compatability
[BepInEx](https://github.com/BepInEx/BepInEx/) - v5.4.x

### Current Thunderstore Communities
- [Content Warning](https://thunderstore.io/c/content-warning/p/www_Day_Dream/CessilCellsCeaChells/)
- [Lethal Company](https://thunderstore.io/c/lethal-company/p/www_Day_Dream/CessilCellsCeaChells/)
- [Risk of Rain 2](https://thunderstore.io/package/www_Day_Dream/CessilCellsCeaChells/)

### Publishing To New Thunderstore Communities
- Create an issue for the specific community OR
- Create a pull request with the implementation into the [TCLI TOML](https://github.com/wwwDayDream/CessilCellsCeaChells/blob/master/CessilCellsCeaChells/ts-assets/thunderstore.toml).