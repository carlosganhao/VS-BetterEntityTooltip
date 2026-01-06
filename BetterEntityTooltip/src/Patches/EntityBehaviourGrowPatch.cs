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

[HarmonyPatch(typeof(EntityBehaviorGrow))]
class EntityBehaviorGrowPatch
{
    private static readonly string hoursToGrowAttributeName = "hoursToGrow";
    private static MethodInfo HoursToGrowGetterInfo = AccessTools.PropertyGetter(typeof(EntityBehaviorGrow), "HoursToGrow");

    [HarmonyPostfix]
    [HarmonyPatch(nameof(EntityBehaviorGrow.Initialize))]
    static void InitializePostfix(EntityProperties properties, JsonObject typeAttributes, ref TreeAttribute ___growTree, Entity ___entity, EntityBehaviorMultiply __instance)
    {
        var getHoursToGrow = () => (float)HoursToGrowGetterInfo.Invoke(__instance, null);

        // HoursToGrow is a propertie of the server side behavior
        // If we want the player to see them we have to provide them to the client
        // Here we are using the already existing tree attribute to do this
        if (___entity.World.Side == EnumAppSide.Server)
        {
            if (!___growTree.HasAttribute(hoursToGrowAttributeName))
            {
                ___growTree.SetFloat(hoursToGrowAttributeName, getHoursToGrow());
            }
        }
    }

    // By default the Grow behaviour doesn't even run on the client
    // Therefore we cant even patch its own GetInfoText
    // So we do a passthrough patch on the Entity's GetInfoText to retrieve our Grow Attribute info
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Entity))]
    [HarmonyPatch(nameof(Entity.GetInfoText))]
    static string CustomGetInfoText(string resultText, SyncedTreeAttribute ___WatchedAttributes, Entity __instance)
    {
        var ___growTree = ___WatchedAttributes.GetTreeAttribute("grow");
        if(___growTree == null) return resultText;

        var infoText = new StringBuilder();
        infoText.Append(resultText);

        var hoursLeftUntilGrowth = (double)___growTree.GetFloat(hoursToGrowAttributeName, -1) - (__instance.World.Calendar.TotalHours - ___growTree.GetDouble("timeSpawned", -1));

        infoText.AppendLine(Lang.Get("Will grow in {0} days", Math.Max(1, Math.Ceiling(hoursLeftUntilGrowth / __instance.World.Calendar.HoursPerDay))));

        return infoText.ToString();
    }
}