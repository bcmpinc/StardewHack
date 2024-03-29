# Tree Spread

## Description
Prevents trees from automatically dropping their seeds and spreading on your farm. To compensate, trees will have a higher chance of having a seed. 
Furthermore trees on your farm will retain their seed during the night.

## Config
*Note: run Stardew Valley once with this mod enabled to generate the `config.json` file.*

* `SeedChance`: Chance that a tree will have a seed. Normally this is 0.05 (=5%). Default = 0.15 (=15%).
* `OnlyPreventTapped`: Whether only tapped trees are prevented from dropping seeds. Default = false.
* `RetainSeed`: Whether the tree should keep its seed during the night, to compensate for trees not spreading. Vanilla SDV removes seeds during the night. Default = true.

## Dependencies
This mod requires the following mods to be installed:

* [SMAPI](https://www.nexusmods.com/stardewvalley/mods/2400)
* [StardewHack](https://www.nexusmods.com/stardewvalley/mods/3213)

## Known bugs
Please report bugs on [GitHub](https://github.com/bcmpinc/StardewHack/issues).

## Changes
#### 7.0:
* Updated for Stardew Valley 1.6
* Localization support.
* Russian & Ukrainian translation (partial)

#### 6.0:
* Update [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) bindings.

#### 4.2:
* Fix issue with `OnlyPreventTapped` being applied when set to false, causing trees to spread when they shouldn't.

#### 4.0:
* Added integration for [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098).
* Added retain seed config option.
* Add 64-bit support

#### 3.1:
* Updated for Stardew Valley 1.5
