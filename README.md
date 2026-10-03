# AllyClarity

A Risk of Rain 2 (BepInEx) mod that makes allies easy to tell apart from enemies: allied summons get an outline, and pinging an ally marks it as an ally instead of an enemy, in one color for the player and their own summons and one for their teammates. An ally's Genesis Loop is shown in the player's color instead of the Wandering Vagrant's blue, which the item reuses.

- Store page: [revoreverse/AllyClarity](https://thunderstore.io/c/riskofrain2/p/revoreverse/AllyClarity/). Its source, [thunderstore/AllyClarity/README.md](thunderstore/AllyClarity/README.md), describes every feature and setting for players; this file is for development.
- [Modding guide](https://github.com/T-Jayay/DroneImprovements/blob/main/docs/MODDING_GUIDE.md) (in the DroneImprovements repo): how this mod, [DroneImprovements](https://github.com/T-Jayay/DroneImprovements), [SprintImprovements](https://github.com/T-Jayay/SprintImprovements) and [MarkAllSeen](https://github.com/T-Jayay/MarkAllSeen) are set up, built, tested and published.
- License: [the Unlicense](LICENSE) (public domain).

## Building

You need the [.NET SDK](https://dotnet.microsoft.com/download) 8 or later. The game, BepInEx, Risk of Options and the MonoMod hook types (`MMHOOK.RoR2`) are referenced from NuGet (the game's publicized reference assemblies, `RiskOfRain2.GameLibs`, come from the [BepInEx feed](https://nuget.bepinex.dev/)), so the first build needs internet access but not the game.

```
dotnet build AllyClarity.sln -c Release
```

Each build copies `AllyClarity.dll` and its PDB into a mod manager profile for testing, as `<ProfileDir>\BepInEx\plugins\AllyClarity\`, if that profile exists. The test profile needs BepInExPack and HookGenPatcher (which generates `MMHOOK_RoR2.dll` when the game starts), and Risk of Options for the in-game settings (players can use the mod without it). Set these properties on the command line or as environment variables:

| Property | Default | Example |
|---|---|---|
| `ProfileDir` | Thunderstore Mod Manager's `Test` profile: `%APPDATA%\Thunderstore Mod Manager\DataFolder\RiskOfRain2\profiles\Test` | an r2modman profile named `Dev`: `-p:ProfileDir="C:\Users\<you>\AppData\Roaming\r2modmanPlus-local\RiskOfRain2\profiles\Dev"` |
| `DeployToProfile` | `true` | `-p:DeployToProfile=false` builds without deploying |

Close the game before building: it keeps the plugin locked, so the copy fails. The build takes the version from `thunderstore/AllyClarity/manifest.json` and embeds `thunderstore/AllyClarity/icon.png` for the Risk of Options mod list.

## Releasing

1. Set the new version in `thunderstore/AllyClarity/manifest.json` (`version_number`) and in `PluginVersion` in `src/AllyClarity/AllyClarityPlugin.cs`, and list the changes under `## <version>` in `thunderstore/AllyClarity/CHANGELOG.md`. An uploaded version can't be changed, so any change, even to the manifest alone, needs a new version.
2. Commit.
3. `python tools/package.py` (Python 3.8 or later) checks the package against Thunderstore's rules and the version against `PluginVersion` and the changelog, builds the mod and writes `dist/AllyClarity-<version>.zip`. It refuses to package uncommitted changes or to overwrite an existing zip; `--force` skips both checks, for test builds only.
4. Upload the zip at https://thunderstore.io/package/create/ with the team **revoreverse** and the community Risk of Rain 2, and tick the **AI Generated** category.

The full checklist, including the first push to GitHub, is under Publishing in the [modding guide](https://github.com/T-Jayay/DroneImprovements/blob/main/docs/MODDING_GUIDE.md#publishing).

## How it works

- `AllyClarityPlugin.cs` sets everything up: the settings, then one hooks class per feature in `Hooks/`, each with an `Init()` called from `Awake`. Add a feature as another hooks class.
- `Hooks/OutlineHooks.cs` (`OutlineAllies`): the game draws outlines with `OutlineHighlight`, a screen-space effect on each camera that calls the static `OutlineHighlight.onPreRenderOutlineHighlight` every time it renders; the mod subscribes once and adds the renderers of every allied summon with `AddHighlight`, as the game's pings do. The effect only renders while something has asked for it (`AddPreRenderHighlightRequest`, which the game's `InteractionDriver` does for a looked-at interactable), so the plugin asks for as long as the setting is on. The handler walks the game's list of `CharacterModel` instances, each of which knows its body, and uses the camera's local user as the viewer, so split-screen players each get their own colors.
- `Hooks/PingHooks.cs` (`AllyPings`): `PingIndicator.RebuildPing` treats every pinged character as an enemy. An `On.` hook calls the original and then turns the ping into a default ping with the ally's colors; one small IL hook swaps the chat line's `PLAYER_PING_ENEMY` text, which is added in the middle of the method (if a game update changes that code, `TryGotoNext` fails, the log says so and the method stays unchanged, as the R2Wiki's IL Hook page recommends).
- `Hooks/GenesisLoopHooks.cs` (`AllyGenesisLoop`): the Genesis Loop item reuses the Wandering Vagrant's nova effects, so an ally's charge sphere, area indicator and explosion are the boss's blue and look like an enemy's. `EntityStates.VagrantNovaItem.ChargeState.OnEnter` runs on every machine (the item's state machine is networked) and makes the sphere and the area indicator as plain, single-use instances of the boss's prefabs; an `On.` hook recolors them after the original. The explosion is spawned by the server through the `EffectManager`, transmitted, with the holder's body as the effect data's root object (the boss spawns the same prefab on each machine, with itself as the root object, so the root object's team tells whose it is). The prefab is pooled and shared by the boss, enemies and allies, so one `On.` hook on `EffectComponent.Reset`, where the game applies the effect data to a new or reused instance on every machine, recolors it for an ally and restores it for anyone else; it first compares `effectIndex`, because `Reset` runs for every effect. That index is looked up once in `RoR2Application.onLoad`, which comes after the `EffectCatalog` has numbered the prefabs and after `EntityStateCatalog.Init` has filled `FireMegaNova.novaEffectPrefab` from its entity state configuration. The color is always `OwnColor`, whoever's ally it is; `TeammateColor` is only for outlines and pings.
- `NovaRecolor.cs` and `ColorShift.cs`: the recoloring. `NovaRecolor` is added to an effect instance the first time it is recolored (the instance is vanilla then) and stores its vanilla particle start colors and colors over lifetime, light colors (including the inactive template light that a particle system's Lights module copies) and renderer materials; recoloring always starts from those, so it never stacks, and restoring writes them back. `ColorShift` rotates a color's hue from the nova's blue (207 degrees on average) to the target's and scales its saturation and value by the target's, so white and grey stay neutral. The materials, ramp textures and prefabs are shared with the boss, other enemies and other lightning effects and are never changed: a material is cloned for each color with a hue-rotated copy of its ramp (`_RemapTex`, read through a render target because the game's textures aren't readable) and tint (`_TintColor`), created on first use and kept for the session. A clone rather than a `MaterialPropertyBlock`, which would have to reach a particle renderer's second material slot (its trail material, which carries the blue of the trails) and would be replaced by the area indicator's own `AnimateShaderAlpha` block.
- The cyan screen tint near the charge and the explosion (a post-processing volume in the prefabs, the boss's color grade) is switched off for allies rather than recolored: Unity's post-processing assembly isn't in RiskOfRain2.GameLibs, so `NovaRecolor` finds the volume by its type's name and disables it, and restores it for anyone else. Not recolored: the item's own spark particles on its holder (`ChargeSparks`), which stay blue.
- `AllyUtil.cs` decides who is an ally (a body on the player team) and whose (the viewer's own, or a summon whose `minionOwnership.ownerMaster` is the viewer's master; otherwise a teammate's).
- Client-side: everything is local visuals and UI. It adds no content and no network messages, so it isn't on the game's network mod list.
- The hook types come from NuGet `MMHOOK.RoR2` (compile only); players get `MMHOOK_RoR2.dll` from HookGenPatcher, a manifest dependency.
- Risk of Options is optional (a soft dependency): `RiskOfOptionsCompat.cs` is only used when it is installed.

## Testing

Solo testing covers the outlines and the pings on your own summons, and your own Genesis Loop (give yourself the item and lose health): buy a few drones and a turret, and ping them. A teammate's color needs a second player; DroneImprovements' DevTools plugin can spawn a stand-in player on the player team. Before a release, also check:
- as a client in a real lobby: a teammate's pings on you, your drones and their drones, and the colors of their summons;
- split-screen, if possible: each player's outlines in their own colors;
- with Drone Tech's command highlight on a drone, with cloaked allies, and without Risk of Options installed;
- Genesis Loop (item `NovaOnLowHealth`): your own, a teammate's as a client in a real lobby and an ally's summon (charge sphere, area indicator and explosion in `OwnColor`); an enemy's and the Wandering Vagrant's (blue as in the game); with `AllyGenesisLoop` off; and an ally's explosion straight after an enemy's or the boss's, and the other way round, because the effect instance is reused (it must change back each time).

## Icon

`tools/make_icon.py` (needs Pillow) draws the store icon `thunderstore/AllyClarity/icon.png`. Its helpers are copied from DroneImprovements' `tools/make_icons.py`; keep the copies identical. With Pillow 12.3.0 it reproduces the committed PNG byte for byte.
