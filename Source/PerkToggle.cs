using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using VFEC.Perks;
using VFEC.Senators;

namespace VFEC_PerkToggle
{
    /// <summary>
    /// Entry point — applies all Harmony patches via PatchAll.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class PerkToggleMod
    {
        private static readonly Harmony Harm = new Harmony("dibolic.vfec.perktoggle");

        static PerkToggleMod()
        {
            // We patch Dialog_SenatorInfo.DoPerkInfo manually because it is a
            // private instance method that we need to fully replace when toggle
            // interaction is available.
            Harm.Patch(
                AccessTools.Method(typeof(Dialog_SenatorInfo), "DoPerkInfo"),
                prefix: new HarmonyMethod(typeof(Patch_Dialog_SenatorInfo_DoPerkInfo), nameof(Patch_Dialog_SenatorInfo_DoPerkInfo.Prefix))
            );

            Log.Message("[VFEC PerkToggle] Patches applied.");
        }
    }

    /// <summary>
    /// Patches Dialog_SenatorInfo.DoPerkInfo(Rect inRect, PerkDef perk, bool locked).
    ///
    /// Original signature (private instance):
    ///   private void DoPerkInfo(Rect inRect, PerkDef perk, bool locked)
    ///
    /// Logic:
    ///   - If the dialog's Faction is NOT yet permanent (i.e. not all senators favored),
    ///     let the original draw run unchanged (return true).
    ///   - If the faction IS permanent, we take over drawing completely:
    ///       * Active perks  → draw with the normal unlocked background + click to disable.
    ///       * Inactive perks → draw with the locked (faded) background + click to enable.
    ///       * We skip the original draw (return false).
    /// </summary>
    [HarmonyPatch]
    public static class Patch_Dialog_SenatorInfo_DoPerkInfo
    {
        /// <summary>
        /// Harmony prefix on Dialog_SenatorInfo.DoPerkInfo.
        /// __instance gives us the dialog so we can read its Faction field.
        /// Returns false to skip the original when we handle the draw ourselves.
        /// </summary>
        public static bool Prefix(
            Dialog_SenatorInfo __instance,
            Rect inRect,
            PerkDef perk,
            bool locked)
        {
            // Read the Faction field from the dialog via Harmony's field-ref accessor.
            var faction = Traverse.Create(__instance).Field<Faction>("Faction").Value;

            // Gate: only allow toggling when this faction is permanent (all senators favored).
            if (faction == null
                || WorldComponent_Senators.Instance == null
                || !WorldComponent_Senators.Instance.Permanent.TryGetValue(faction, out var isPermanent)
                || !isPermanent)
            {
                // Not yet permanent — run the original draw unchanged.
                return true;
            }

            // Faction is permanent: we handle the draw ourselves.
            // Read perkBG_Unlocked from the dialog instance (private field).
            var perkBG_Unlocked = Traverse.Create(__instance).Field<Texture2D>("perkBG_Unlocked").Value;

            var perkManager = GameComponent_PerkManager.Instance;
            bool isActive = perkManager.ActivePerks.Contains(perk);

            // Choose background: active = faction-coloured unlocked BG, inactive = grey locked BG.
            Texture2D bg = isActive ? perkBG_Unlocked : SenatorUIUtility.PerkBG_Locked;

            // Draw background + perk icon exactly as the original does.
            Widgets.DrawTextureFitted(inRect, bg, 1f);
            Widgets.DrawTextureFitted(inRect.ContractedBy(10f), perk.Icon, 1f);

            // Draw a hover highlight so the player knows the icon is interactive.
            if (Mouse.IsOver(inRect))
            {
                Widgets.DrawHighlight(inRect);

                string tipKey = isActive ? "VFECPT_Tooltip_Disable" : "VFECPT_Tooltip_Enable";
                TooltipHandler.TipRegion(inRect, tipKey.Translate());
            }
            else
            {
                // Always show the standard description tooltip when not adding the toggle tip.
                TooltipHandler.TipRegion(inRect, $"{perk.LabelCap}\n\n{perk.description}");
            }

            // Handle click — toggle perk in/out of ActivePerks.
            if (Widgets.ButtonInvisible(inRect))
            {
                if (isActive)
                {
                    perkManager.RemovePerk(perk);
                    Messages.Message(
                        "VFECPT_PerkDisabled".Translate(perk.LabelCap),
                        MessageTypeDefOf.NeutralEvent,
                        historical: false);
                }
                else
                {
                    perkManager.AddPerk(perk);
                    Messages.Message(
                        "VFECPT_PerkEnabled".Translate(perk.LabelCap),
                        MessageTypeDefOf.PositiveEvent,
                        historical: false);
                }
            }

            // Skip the original DoPerkInfo — we've drawn everything ourselves.
            return false;
        }
    }
}
