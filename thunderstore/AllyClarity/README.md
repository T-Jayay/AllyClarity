# Ally Clarity

Makes your allies easy to tell apart from enemies: allied summons get an outline, pinging an ally marks it as an ally instead of an enemy, and an ally's Genesis Loop explosion is no longer the Wandering Vagrant's blue. You and your summons get one color, your teammates and their summons another.

## Features
- **Ally outlines.** Drones, turrets, Empathy Cores, Col. Droneman and every other allied summon get an outline in the color of whose they are: green for yours, light blue for your teammates' and for allies that nobody owns. Players aren't outlined. Summons that are invisible or cloaked have no outline, and a drone that the game is already highlighting (Drone Tech's command highlight) keeps its own.
- **Ally pings.** In the normal game, pinging any character marks it as an enemy: the red combat icon, a red outline, and "wants to attack" in the chat. With this mod, pinging an ally (a teammate or any allied summon) shows the normal ping instead. Its outline and the ping text are in the ally's color, and the chat line says "*name* pinged an ally: *target*." in that color. Pings from other players are changed the same way on your screen.
- **Ally Genesis Loop.** The Genesis Loop item reuses the Wandering Vagrant's nova, so in the normal game an ally's charging sphere and explosion are the same blue as an enemy's. With this mod, the Genesis Loop of every ally (you, your teammates and allied summons) is in your own color (green by default) instead: the charging sphere, the area it will hit and the explosion. The An ally's Genesis Loop also doesn't tint your screen cyan when you're close to it, as the Vagrant's does. Genesis Loops of enemies and the Wandering Vagrant itself stay blue.

The outlines are drawn with the game's own outline renderer, the one it uses for interactables and pings; the mod adds nothing to the scene.

## Settings
In `BepInEx/config/revor.AllyClarity.cfg`, or in game under Settings → Mod Options → **AllyClarity** when [Risk of Options](https://thunderstore.io/c/riskofrain2/p/Rune580/Risk_Of_Options/) is installed. Changes apply immediately.
- `OwnColor` (default green): you, your summons and the pings on them, and every ally's Genesis Loop.
- `TeammateColor` (default light blue): the other players, their summons, allies that nobody owns, and the pings on them. Not used for Genesis Loops, which are always in `OwnColor`.
- `OutlineAllies` (default `true`): outline allied summons.
- `AllyPings` (default `true`): pings on allies show an ally instead of an enemy.
- `AllyGenesisLoop` (default `true`): allies' Genesis Loop in `OwnColor` instead of the Wandering Vagrant's blue.

## Multiplayer
Client-side: everything is local, so only you see your changes and nobody else needs the mod, not even the host. You can join any lobby with or without it. Other players still see their own pings and your pings as their game shows them. A teammate's Genesis Loop is changed on your screen as well, whether or not they have the mod.

With split-screen, each player's outlines use that player's own colors; a ping is shared by the screens, so its colors follow the first player.

Source and bug reports: [github.com/T-Jayay/AllyClarity](https://github.com/T-Jayay/AllyClarity).
