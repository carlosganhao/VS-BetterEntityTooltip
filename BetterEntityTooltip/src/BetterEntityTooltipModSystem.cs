using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using HarmonyLib;

namespace BetterEntityTooltip;

public class BetterEntityTooltipModSystem : ModSystem
{
    private string patchId = "betterentitytooltip";
    private Harmony harmonyInstance;

    public override void StartPre(ICoreAPI api)
    {
        base.StartPre(api);

        if (!Harmony.HasAnyPatches(patchId))
        {
            harmonyInstance = new Harmony(patchId);
            harmonyInstance.PatchAll();
        }
    }

    public override void Dispose()
    {
        harmonyInstance?.UnpatchAll(patchId);
        base.Dispose();
    }
}
