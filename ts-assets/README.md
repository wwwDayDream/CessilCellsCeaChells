## CessilCellsCeaChells

Allows installed plugins to request creation of Fields, Properties, Methods, and more in Managed DLLs.

### Technical Jargon
A BepInEx 5.4.21 patcher that scans all installed plugins for assembly attributes that inform us of any desired Fields, Properties, Methods, and more to inject on the `/Game_Data/Managed/` DLLs.

### Usage & Documentation
The wiki contains a [Getting Started](https://github.com/wwwDayDream/CessilCellsCeaChells/wiki) section as well as usages of assembly attributes.

## Features
- Injecting Private Instance Fields on any type in `/Game_Data/Managed/*.dll`.
- Injecting Public Instance Properties on any type in `/Game_Data/Managed/*.dll`.
- Injecting Public Methods on any type in `/Game_Data/Managed/*.dll` with any parameters, defaults, and/or return type.
- Injecting Enum Entries onto any enum in an ordered fashion.

### Version Compatability
[BepInEx](https://github.com/BepInEx/BepInEx/) - v5.4.x