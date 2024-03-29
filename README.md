# StardewHack
A bunch of Stardew Valley mods that heavily rely on IL code modification. For this purpose it uses [Harmony](https://github.com/pardeike/Harmony/wiki). 

Android is not supported. In particular Wear More Rings and Bigger Backpack will not work, due to android having an entirely different inventory screen.

## Overview
* [Always Scroll Map](/AlwaysScrollMap):                     Makes the map scroll past the edge of the map.
* [Bigger Backpack](/BiggerBackpack):                        Adds a backpack upgrade for up to 48 inventory slots.
* [Craft Counter](/CraftCounter):                            Adds a counter to the description of crafting recipes telling how often it has been crafted.
* [Flexible Arms](/FlexibleArms):                            Makes it easier to aim your tools with a mouse.
* [Fix Animal Tool Animations](/FixAnimalTools):             When using the shears or milk pail, the animation no longer plays when no animal is nearby.
* [Grass Growth](/GrassGrowth):                              Allows long grass to spread everywhere on your farm.
* [Movement Speed](/MovementSpeed):                          Changes the player's movement speed and charging time of the hoe and watering can.
* [Tilled Soil Decay](/TilledSoilDecay):                     Delays decay of watered tilled soil.
* [Tree Spread](/TreeSpread):                                Prevents trees from spreading on your farm.
* [Wear More Rings](/WearMoreRings):                         Adds 4 additional ring slots to your inventory.
* [Yet Another Harvest With Scythe Mod](/HarvestWithScythe): Allows you to harvest all crops and forage using the scythe. They can also still be plucked.

## Compiling

Open `StardewHack2.sln` in Visual Studio 2022 (or whatever development tool you are using) and hope it compiles. :)

## Translating

Starting with version 7.0 my mods have support for translation files. If you like to help out, I've combined all the translation files into a single file: 
[default.json](/translations/default.json). To get your translation included in the next release, create a new issue and attach your translated file.
