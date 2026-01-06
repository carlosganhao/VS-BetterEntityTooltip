using System;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace BetterEntityTooltip.Patches;

[HarmonyPatch(typeof(EntityBehaviorMultiply))]
class EntityBehaviorMultiplyPatch
{
    private static readonly string portionsEatenAttributeName = "portionsEatenForMultiply";
    private static readonly string pregnancyDaysAttributeName = "pregnancyDays";
    private static MethodInfo PortionsEatenForMultiplyGetterInfo = AccessTools.PropertyGetter(typeof(EntityBehaviorMultiply), "PortionsEatenForMultiply");
    private static MethodInfo PregnancyDaysGetterInfo = AccessTools.PropertyGetter(typeof(EntityBehaviorMultiply), "PregnancyDays");

    [HarmonyPostfix]
    [HarmonyPatch(nameof(EntityBehaviorMultiply.Initialize))]
    static void InitializePostfix(EntityProperties properties, JsonObject attributes, ref TreeAttribute ___multiplyTree, Entity ___entity, EntityBehaviorMultiply __instance)
    {
        var getPortionsEatenForMultiply = () => (float)PortionsEatenForMultiplyGetterInfo.Invoke(__instance, null);
        var getPregnancyDays = () => (float)PregnancyDaysGetterInfo.Invoke(__instance, null);

        // PortionsEaten and PregnancyDays are properties of the server side behavior
        // If we want the player to see them we have to provide them to the client
        // Here we are using the already existing tree attribute to do this
        if (___entity.World.Side == EnumAppSide.Server)
        {
            if (!___multiplyTree.HasAttribute(portionsEatenAttributeName))
            {
                ___multiplyTree.SetFloat(portionsEatenAttributeName, getPortionsEatenForMultiply());
            }

            if (!___multiplyTree.HasAttribute(pregnancyDaysAttributeName))
            {
                ___multiplyTree.SetFloat(pregnancyDaysAttributeName, getPregnancyDays());
            }
        }
    }

    private static MethodInfo IsPregnantGetterInfo = AccessTools.PropertyGetter(typeof(EntityBehaviorMultiply), "IsPregnant");
    private static MethodInfo TotalDaysPregnancyStartGetterInfo = AccessTools.PropertyGetter(typeof(EntityBehaviorMultiply), "TotalDaysPregnancyStart");
    private static MethodInfo TotalDaysCooldownUntilGetterInfo = AccessTools.PropertyGetter(typeof(EntityBehaviorMultiply), "TotalDaysCooldownUntil");

    [HarmonyPrefix]
    [HarmonyPatch(nameof(EntityBehaviorMultiply.GetInfoText))]
    static bool CustomGetInfoText(StringBuilder infotext, ref ITreeAttribute ___multiplyTree, Entity ___entity, EntityBehaviorMultiply __instance)
    {
        var getIsPregnant = () => (bool)IsPregnantGetterInfo.Invoke(__instance, null);
        var getTotalDaysPregnancyStart = () => (double)TotalDaysPregnancyStartGetterInfo.Invoke(__instance, null);
        var getTotalDaysCooldownUntil = () => (double)TotalDaysCooldownUntilGetterInfo.Invoke(__instance, null);

        ___multiplyTree = ___entity.WatchedAttributes.GetTreeAttribute("multiply");

        if (getIsPregnant())
        {
            var daysLeftUntilBirth = (double)___multiplyTree.GetFloat(pregnancyDaysAttributeName, -1) - (___entity.World.Calendar.TotalDays - getTotalDaysPregnancyStart());

            infotext.AppendLine(Lang.Get("Is pregnant ({0} days until birth)", Math.Max(1, Math.Ceiling(daysLeftUntilBirth))));
        }
        else
        {
            if (___entity.Alive)
            {
                ITreeAttribute tree = ___entity.WatchedAttributes.GetTreeAttribute("hunger");
                if (tree != null)
                {
                    float saturation = tree.GetFloat("saturation", 0);
                    infotext.AppendLine(Lang.Get("Portions eaten: {0}/{1}", saturation, ___multiplyTree.GetFloat(portionsEatenAttributeName, -1)));
                }

                double daysLeft = getTotalDaysCooldownUntil() - ___entity.World.Calendar.TotalDays;

                if (daysLeft > 0)
                {
                    if (daysLeft > 1)
                    {
                        infotext.AppendLine(Lang.Get("{0} days left before ready to mate", Math.Max(2, Math.Ceiling(daysLeft))));
                    }
                    else
                    {
                        infotext.AppendLine(Lang.Get("Less than 1 day before ready to mate"));
                    }

                }
                else
                {
                    infotext.AppendLine(Lang.Get("Ready to mate"));
                }
            }
        }

        return false;
    }
}