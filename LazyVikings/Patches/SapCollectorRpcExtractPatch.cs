using HarmonyLib;

namespace LazyVikings.Patches;

[HarmonyPatch(typeof(SapCollector), nameof(SapCollector.RPC_Extract))]
public static class SapCollectorRpcExtractPatch
{
    
}