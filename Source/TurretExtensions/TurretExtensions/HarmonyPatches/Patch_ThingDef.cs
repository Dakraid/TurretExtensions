﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
using HarmonyLib;
using UnityEngine;

namespace TurretExtensions
{

    public static class Patch_ThingDef
    {

        [HarmonyPatch(typeof(ThingDef), nameof(ThingDef.SpecialDisplayStats))]
        public static class SpecialDisplayStats
        {

            public static void Postfix(ThingDef __instance, StatRequest req, ref IEnumerable<StatDrawEntry> __result)
            {
                // Add mortar shell stats to the list of stat draw entries
                if (__instance.IsShell)
                {
                    Thing shellThing = ThingMaker.MakeThing(__instance.projectileWhenLoaded);
                    ProjectileProperties shellProps = __instance.projectileWhenLoaded.projectile;
                    int shellDamage = shellProps.GetDamageAmount(shellThing);
                    float shellArmorPenetration = shellProps.GetArmorPenetration(shellThing);
                    float shellStoppingPower = shellProps.StoppingPower;
                    string shellDamageDef = shellProps.damageDef.label.CapitalizeFirst();
                    float shellExplosionRadius = shellProps.explosionRadius;

                    __result = __result.AddItem(new StatDrawEntry(StatCategoryDefOf.TurretAmmo, "Damage".Translate(), shellDamage.ToString(), "Stat_Thing_Damage_Desc".Translate(), 20));
                    __result = __result.AddItem(new StatDrawEntry(StatCategoryDefOf.TurretAmmo, "TurretExtensions.ShellDamageType".Translate(), shellDamageDef, "TurretExtensions.ShellDamageType_Desc".Translate(), 19));
                    __result = __result.AddItem(new StatDrawEntry(StatCategoryDefOf.TurretAmmo, "ArmorPenetration".Translate(), shellArmorPenetration.ToStringPercent(), "ArmorPenetrationExplanation".Translate(), 18));
                    __result = __result.AddItem(new StatDrawEntry(StatCategoryDefOf.TurretAmmo, "StoppingPower".Translate(), shellStoppingPower.ToString(), "StoppingPowerExplanation".Translate(), 17));

                    if (shellExplosionRadius > 0)
                        __result = __result.AddItem(new StatDrawEntry(StatCategoryDefOf.TurretAmmo, "TurretExtensions.ShellExplosionRadius".Translate(), shellExplosionRadius.ToString(), "TurretExtensions.ShellExplosionRadius_Desc".Translate(), 16));
                }

                // Minimum range
                if (__instance.Verbs.FirstOrDefault(v => v.isPrimary) is VerbProperties verb)
                {
                    var verbStatCategory = (__instance.category != ThingCategory.Pawn) ? RimWorld.StatCategoryDefOf.Weapon : RimWorld.StatCategoryDefOf.PawnCombat;
                    if (verb.LaunchesProjectile)
                    {
                        if (verb.minRange > default(float))
                            __result = __result.AddItem(new StatDrawEntry(verbStatCategory, "MinimumRange".Translate(), verb.minRange.ToString("F0"), "TurretExtensions.MinimumRange_Desc".Translate(), 10));
                    }
                }

                // Add turret weapons stats to the list of SpecialDisplayStats
                var buildingProps = __instance.building;
                if (buildingProps != null && buildingProps.IsTurret)
                {
                    var gunStatList = new List<StatDrawEntry>();
                    //if (req.HasThing)
                    //{
                    //    var gun = ((Building_TurretGun)req.Thing).gun;
                    //    gunStatList.AddRange(gun.def.SpecialDisplayStats(StatRequest.For(gun)));
                    //    gunStatList.AddRange(NonPublicMethods.StatsReportUtility_StatsToDraw_thing(gun));
                    //}
                    //else
                    //{
                    //    var defaultStuff = GenStuff.DefaultStuffFor(buildingProps.turretGunDef);
                    //    gunStatList.AddRange(buildingProps.turretGunDef.SpecialDisplayStats(StatRequest.For(buildingProps.turretGunDef, defaultStuff)));
                    //    gunStatList.AddRange(NonPublicMethods.StatsReportUtility_StatsToDraw_def_stuff(buildingProps.turretGunDef, defaultStuff));
                    //}

                    //// Replace cooldown
                    //var cooldownEntry = gunStatList.FirstOrDefault(s => s.stat == StatDefOf.RangedWeapon_Cooldown);
                    //if (cooldownEntry != null)
                    //    cooldownEntry = new StatDrawEntry(cooldownEntry.category, cooldownEntry.LabelCap, TurretCooldown(req, buildingProps).ToStringByStyle(cooldownEntry.stat.toStringStyle),
                    //        cooldownEntry.DisplayPriorityWithinCategory, cooldownEntry.overrideReportText);
                    //else
                    //{
                    //    var cooldownStat = StatDefOf.RangedWeapon_Cooldown;
                    //    gunStatList.Add(new StatDrawEntry(StatCategoryDefOf.Turret, cooldownStat, TurretCooldown(req, buildingProps), StatRequest.ForEmpty(), cooldownStat.toStringNumberSense));
                    //}

                    //// Replace warmup
                    //var warmupEntry = gunStatList.FirstOrDefault(s => s.LabelCap == "WarmupTime".Translate().CapitalizeFirst());
                    //if (warmupEntry != null)
                    //    warmupEntry = new StatDrawEntry(warmupEntry.category, warmupEntry.LabelCap, $"{TurretWarmup(req, buildingProps).ToString("0.##")} s",
                    //        warmupEntry.DisplayPriorityWithinCategory, warmupEntry.overrideReportText);
                    //else
                    //    gunStatList.Add(new StatDrawEntry(StatCategoryDefOf.Turret, "WarmupTime".Translate(), $"{TurretWarmup(req, buildingProps).ToString("0.##")} s", 40));

                    // Add upgradability
                    if (req.Def is ThingDef tDef)
                    {
                        string upgradableString;
                        CompProperties_Upgradable upgradableCompProps;
                        if (req.Thing != null && req.Thing.IsUpgradable(out CompUpgradable upgradableComp))
                        {
                            upgradableString = (upgradableComp.upgraded ? "TurretExtensions.NoAlreadyUpgraded" : "TurretExtensions.YesClickForDetails").Translate();
                            upgradableCompProps = upgradableComp.Props;
                        }
                        else
                            upgradableString = (tDef.IsUpgradable(out upgradableCompProps) ? "TurretExtensions.YesClickForDetails" : "No").Translate();

                        var upgradeHyperlinks = new List<Dialog_InfoCard.Hyperlink>();
                        if (upgradableCompProps.turretGunDef != null)
                            upgradeHyperlinks.Add(new Dialog_InfoCard.Hyperlink(upgradableCompProps.turretGunDef));

                        gunStatList.Add(new StatDrawEntry(RimWorld.StatCategoryDefOf.BasicsNonPawn, "TurretExtensions.Upgradable".Translate(), upgradableString,
                            TurretExtensionsUtility.UpgradeReadoutReportText(req), 999, hyperlinks: upgradeHyperlinks));
                    }

                    // Remove entries that shouldn't be shown and change 'Weapon' categories to 'Turret' categories
                    //for (int i = 0; i < gunStatList.Count; i++)
                    //{
                    //    var curEntry = gunStatList[i];
                    //    if ((curEntry.stat != null && !curEntry.stat.showNonAbstract) || (curEntry.category != RimWorld.StatCategoryDefOf.Weapon && curEntry.category != StatCategoryDefOf.Turret))
                    //    {
                    //        gunStatList.Remove(curEntry);
                    //        i--;
                    //    }
                    //    else
                    //        curEntry.category = StatCategoryDefOf.Turret;
                    //}
                    __result = __result.Concat(gunStatList);
                }

            }

            private static float TurretCooldown(StatRequest req, BuildingProperties buildingProps)
            {
                if (req.Thing is Building_TurretGun gunTurret)
                {
                    if (gunTurret.IsUpgraded(out CompUpgradable upgradableComp))
                        return NonPublicMethods.Building_TurretGun_BurstCooldownTime(gunTurret) * upgradableComp.Props.turretBurstCooldownTimeFactor;
                    return NonPublicMethods.Building_TurretGun_BurstCooldownTime(gunTurret);
                }
                else if (buildingProps.turretBurstCooldownTime > 0)
                    return buildingProps.turretBurstCooldownTime;
                else
                    return buildingProps.turretGunDef.GetStatValueAbstract(StatDefOf.RangedWeapon_Cooldown);
            }

            private static float TurretWarmup(StatRequest req, BuildingProperties buildingProps)
            {
                if (req.Thing != null && req.Thing.IsUpgraded(out CompUpgradable upgradableComp))
                    return buildingProps.turretBurstWarmupTime * upgradableComp.Props.turretBurstWarmupTimeFactor;
                else
                    return buildingProps.turretBurstWarmupTime;
            }

        }

    }

}