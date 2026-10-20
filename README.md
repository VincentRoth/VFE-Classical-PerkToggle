# VFE Classical – Senator Perk Toggle

## What it does

Once you have gained the favour of **all senators in a Republic faction** (making it _permanent_), every senator perk icon displayed in that faction's senator view becomes clickable:

| State | Background | Click action |
|-------|-----------|-------------|
| Active | Normal faction-coloured background | Disables the perk (`RemovePerk`) |
| Disabled | Grey locked background | Re-enables the perk (`AddPerk`) |

State is stored **directly in `GameComponent_PerkManager.ActivePerks`** via the official `AddPerk` / `RemovePerk` methods, so every perk effect (Harmony patches, stat modifiers, tick workers) is correctly applied and removed in real time, and the state persists across saves.

The final per-faction perk (the one unlocked when all senators are favoured) is also toggleable.

Factions that are **not yet permanent** show their perks exactly as normal — no change.

## Requirement

Only one: **[Vanilla Factions Expanded – Classical](https://steamcommunity.com/sharedfiles/filedetails/?id=2787850474)**. Load this mod after it.

---

## Building from source

The source (`Source/PerkToggle.cs`) is shared; there is one project per game version, and each must be built against the matching installed RimWorld.

- **1.6**: `Source/VFEC_PerkToggle.csproj` (`net472`) → `1.6/Assemblies/`
- **1.7**: `Source/1.7/VFEC_PerkToggle.csproj` (`net10.0`, references `Assembly-CSharp`, `UnityStubs`, Harmony and VEF 1.7; VFEC is the 1.6 DLL, which VFE Classical also loads for 1.7) → `1.7/Assemblies/`

1. Open the project for your version in Visual Studio or Rider (or `dotnet build -c Release`).
2. Edit the `<RimWorldPath>` and `<RimWorldModPath>` properties at the top of the `.csproj` to match your installation paths.
3. Build — the `.dll` is output directly to the matching `Assemblies/` folder.

### Finding the VFEC assembly name

Decompile `1.6/Assemblies/VFEC.dll` (from the VFE Classical workshop folder) with **dnSpy** or **ILSpy** and confirm:
- Namespace: `VFEC.Perks`, `VFEC.Senators`
- Class: `Dialog_SenatorInfo` with private method `DoPerkInfo(Rect, PerkDef, bool)`

If the compiled assembly name differs (e.g. `VFEClassical.dll`), update the `<Reference>` in the `.csproj` accordingly.

---

## How the patch works

`Dialog_SenatorInfo.DoPerkInfo` is a **private instance method** that draws a single perk icon. We patch it with a **Harmony prefix**:

- If the dialog's `Faction` is not yet permanent → return `true` (let original run).
- If the faction is permanent → we draw the icon ourselves (background + perk icon + hover highlight + tooltip), handle the click, and return `false` to skip the original.

No XML or Def changes are needed.
