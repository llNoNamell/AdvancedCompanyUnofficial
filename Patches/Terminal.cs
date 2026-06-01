using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using AdvancedCompany.Network;
using AdvancedCompany.Network.Messages;
using AdvancedCompany.Terminal;
using AdvancedCompany.Config;
using UnityEngine.Assertions.Must;
using UnityEngine.UI;
using System.Runtime.CompilerServices;
using System.Reflection.Emit;

namespace AdvancedCompany.Patches
{
    [HarmonyPatch]
    internal class Terminal
    {

        // DawnLib compatibility:
        // AC no longer transpiles Terminal.TextPostProcess's buyableItemsList loop.
        // DawnLib owns terminal shop visibility. AC keeps LoadNewNodeIfAffordable
        // as a final purchase guard below.
        //
        // Original AC transpiler disabled because it conflicts with DawnLib's
        // Terminal.TextPostProcess IL hook around buyableItemsList.
        static IEnumerable<CodeInstruction> PatchTextPostProcess_Disabled(IEnumerable<CodeInstruction> instructions)
        {
            Plugin.Log.LogDebug("Skipping AC Terminal.TextPostProcess transpiler; DawnLib handles terminal shop display.");
            return instructions;
        }


        [HarmonyPatch(typeof(global::Terminal), "LoadNewNodeIfAffordable")]
        [HarmonyPrefix]
        private static bool LoadNewNodeIfAffordable(global::Terminal __instance, TerminalNode node)
        {
            if (node.buyItemIndex >= 0)
            {
                if (__instance.buyableItemsList.Length > node.buyItemIndex)
                {
                    var item = __instance.buyableItemsList[node.buyItemIndex];
                    var config = ServerConfiguration.Instance.Items.GetByItemName(item.itemName);
                    if (config != null && !config.Active)
                    {
                        __instance.LoadNewNode(__instance.terminalNodes.specialNodes[16]);
                        return false;
                    }
                }
            }
            if (node.shipUnlockableID >= 0)
            {
                if (global::StartOfRound.Instance.unlockablesList.unlockables.Count > node.shipUnlockableID)
                {
                    var unlockable = global::StartOfRound.Instance.unlockablesList.unlockables[node.shipUnlockableID];
                    var config = ServerConfiguration.Instance.Items.GetByUnlockableName(unlockable.unlockableName);
                    if (config != null && !config.Active)
                    {
                        __instance.LoadNewNode(__instance.terminalNodes.specialNodes[16]);
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
