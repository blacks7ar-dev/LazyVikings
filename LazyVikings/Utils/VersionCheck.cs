using System;
using System.Collections.Generic;
using HarmonyLib;

namespace LazyVikings.Utils;

[HarmonyPatch]
internal class VersionCheck
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection))]
    private static void OnNewConnection_Prefix(ZNetPeer peer, ref ZNet __instance)
    {
        Logging.LogDebug("Registering version RPC Handler.");
        peer.m_rpc.Register($"{Plugin.modName}_VersionCheck",
            new Action<ZRpc, ZPackage>(RpcHandlers.RPC_LazyVikings_Version));
        Logging.LogDebug("Invoking version check");
        ZPackage zPackage = new();
        zPackage.Write(Plugin.modVersion);
        peer.m_rpc.Invoke($"{Plugin.modName}_VersionCheck", zPackage);
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo))]
    private static class ZNetRpcPeerInfoPatch
    {
        private static bool Prefix(ZRpc rpc, ref ZNet __instance)
        {
            if (!__instance.IsServer() || RpcHandlers.ValidatedPeers.Contains(rpc)) return true;
            Logging.LogWarning(
                $"Peer ({rpc.m_socket.GetHostName()}) never sent version or couldn't due to previous disconnect, disconnecting.");
            rpc.Invoke("Error", 3);
            return false;
        }

        private static void Postfix()
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.instance.GetServerPeerID(), "RequestAdminSync",
                new ZPackage());
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.ShowConnectError))]
    private static void ShowConnectionError_Postfix(FejdStartup __instance)
    {
        if (!__instance.m_connectionFailedPanel.activeSelf) return;
        __instance.m_connectionFailedError.autoSizeTextContainer = true;
        // __instance.m_connectionFailedError.resizeTextMaxSize = 25;
        // __instance.m_connectionFailedError.resizeTextMinSize = 15;
        __instance.m_connectionFailedError.text += $"\n{Plugin.connectionError}";
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Disconnect))]
    private static void ZNetDisconnect_Prefix(ZNetPeer peer, ref ZNet __instance)
    {
        if (!__instance.IsServer()) return;
        Logging.LogInfo(
            $"Peer ({peer.m_rpc.m_socket.GetHostName()}) disconnected, removing from validated list.");
        _ = RpcHandlers.ValidatedPeers.Remove(peer.m_rpc);
    }

    private static class RpcHandlers
    {
        public static readonly List<ZRpc> ValidatedPeers = new();

        public static void RPC_LazyVikings_Version(ZRpc rpc, ZPackage pkg)
        {
            string? version = pkg.ReadString();
            Logging.LogInfo($"Version check, local: {Plugin.modVersion}, remote: {version}");
            if (version != Plugin.modVersion)
            {
                Plugin.connectionError =
                    $"{Plugin.modName} Installed: {Plugin.modVersion}\n Needed: {version}";
                if (!ZNet.instance.IsServer()) return;
                Logging.LogWarning(
                    $"Peer ({rpc.m_socket.GetHostName()}) has incompatible version, disconnecting.");
                rpc.Invoke("Error", 3);
            }
            else
            {
                if (!ZNet.instance.IsServer())
                {
                    Logging.LogInfo("Received same version from server!");
                }
                else
                {
                    Logging.LogInfo($"Adding peer ({rpc.m_socket.GetHostName()}) to validated list.");
                    ValidatedPeers.Add(rpc);
                }
            }
        }
    }
}