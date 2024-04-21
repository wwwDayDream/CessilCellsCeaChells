Allows installed plugins to request creation of Fields, Properties, and Methods in Managed DLLs.

### Technical Jargon
A BepInEx 5.4.21 patcher that scans all installed plugins for assembly attributes that inform us of any desired Fields, Properties, and Methods to inject on the `/Game_Data/Managed/` DLLs.

### Usage & Documentation
The wiki contains a [Getting Started](https://github.com/wwwDayDream/CessilCellsCeaChells/wiki) section as well as usages of assembly attributes.

## Features
- Injecting Public Instance Properties on any type in `/Game_Data/Managed/*.dll`
- Injecting Private Instance Fields on any type in `/Game_Data/Managed/*.dll`
- Injecting Public Methods on any type in `/Game_Data/Managed/*.dll` with any parameters and/or return type.

### Version Compatability
[BepInEx](https://github.com/BepInEx/BepInEx/) - v5.4.*