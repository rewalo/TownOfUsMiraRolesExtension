# Town of Us: Mira Roles Extension

An extension mod for [Town of Us: Mira](https://github.com/AU-Avengers/TOU-Mira) that adds new roles and modifiers.

## Features

This extension mod:
1. **Renames** the existing Trapper role (ground traps that reveal roles) to **Revealer**
2. **Adds** a new **Trapper** role (vent traps that immobilize players)
3. **Adds** the **Clueless** modifier

### Roles
- **Revealer** (Crewmate, renamed from Trapper): Place traps around the map to reveal roles of players who stay in them long enough.
- **Trapper** (Crewmate, new): Place traps on vents that immobilize players who use them.

### Modifiers
- **Clueless** (Universal): Removes all task guidance (task list, task arrows/markers, and map task locations). Tasks still function normally and contribute to the task bar.

## Installation

1. Ensure you have [Town of Us: Mira](https://github.com/AU-Avengers/TOU-Mira) installed.
2. Build this project or download a release.
3. Place the compiled DLL in your `BepInEx/plugins/` folder.

## Building

1. Clone this repository.
2. Restore NuGet packages.
3. Build the solution in Visual Studio or using `dotnet build`.

## Requirements

- .NET 6.0
- Town of Us: Mira 1.5.0 or later
- MiraAPI 0.3.6 or later
- Reactor 2.5.0 or later

## License

This software is distributed under the GNU GPLv3 License.

## Copyright

This mod is not affiliated with Among Us or Innersloth LLC, and the content contained therein is not endorsed or otherwise sponsored by Innersloth LLC. Portions of the materials contained herein are property of Innersloth LLC.

© Innersloth LLC.

