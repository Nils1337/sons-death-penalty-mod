﻿using System.Reflection;
using Construction;
using Endnight.Extensions;
using HarmonyLib;
using Microsoft.VisualBasic.CompilerServices;
using RedLoader;
using Sons.Cutscenes;
using SonsSdk;
using UnityEngine;
using Sons.Events;
using Sons.Inventory;
using Sons.Items.Core;
using TheForest.Items.Inventory;
using TheForest.Player.Actions;
using TheForest.Utils;
using Object = UnityEngine.Object;

namespace MyMod;

public class MyMod : SonsMod
{
    private static int[] _itemsToBeRemoved = new int[] {
        ItemTools.Identifiers.I9mmAmmo,
        ItemTools.Identifiers.Stick,
        ItemTools.Identifiers.Stone
    };
    
    public MyMod()
    {
        // Uncomment any of these if you need a method to run on a specific update loop.
        //OnUpdateCallback = MyUpdateMethod;
        //OnLateUpdateCallback = MyLateUpdateMethod;
        //OnFixedUpdateCallback = MyFixedUpdateMethod;
        //OnGUICallback = MyGUIMethod;

        // Uncomment this to automatically apply harmony patches in your assembly.
        HarmonyPatchAll = true;
    }

    [HarmonyPatch(typeof(Vitals), methodName: "TriggerDeath")]
    class VitalsTriggerDeathPatch
    {
        private static void Prefix()
        {
            RLog.Msg("Death Triggered");
        }
    }

    [HarmonyPatch(typeof(Vitals), methodName: "TriggerKnockedOut")]
    class VitalsTriggerKnockedOutPatch
    {
        private static void Prefix()
        {
            RLog.Msg("Knockout Triggered");
        }
    }

    [HarmonyPatch(typeof(Vitals), methodName: "SetHealth", new Type[] { typeof(float) })]
    class VitalsSetHealthPatch
    {
        private static void Prefix(float value)
        {
            RLog.Msg("Health set to " + value);
        }
    }

    // public static bool PlayerDeathCutsceneBasePatch(PlayerDeathCutsceneMarker marker)
    // {
    //     RLog.Msg("Placement of Dropped Inventory Bag suppressed");
    //     return false;
    // }

    public static void FillDroppedInventoryPatch(ref Il2CppSystem.Collections.Generic.IReadOnlyDictionary<int, ItemInstanceManager.Items> itemsMap)
    {
        
        var overwrittenMap = new Il2CppSystem.Collections.Generic.Dictionary<int, ItemInstanceManager.Items>();

        var enumerator = LocalPlayer.Inventory._itemInstanceManager._itemsMap.GetEnumerator();

        while (enumerator.MoveNext())
        {
            Il2CppSystem.Collections.Generic.KeyValuePair<int, ItemInstanceManager.Items> current = enumerator._current;
            
            if (!_itemsToBeRemoved.Contains(current.Key))
            {
                overwrittenMap.Add(current.Key, current.Value);
            }
        }
        
        itemsMap = new Il2CppSystem.Collections.Generic.IReadOnlyDictionary<int, ItemInstanceManager.Items>(overwrittenMap.Pointer);
        
        RLog.Msg("Dropped Inventory Items overwritten");
    }

    private static void OnPlayerDied(object o)
    {
        LocalPlayer.Vitals.SetStrength(0);
        LocalPlayer.Vitals.SetStrengthLevel(LocalPlayer.Vitals._currentStrengthLevel.ToString());
        LocalPlayer.Vitals.SetFullness(25);
        LocalPlayer.Vitals.SetHydration(25);
        LocalPlayer.Vitals.SetRest(25);
        RLog.Msg("Player died. Vitals Reset.");
    }

    private static void OnItemPickedUp
        (object o)
    {
        RLog.Msg("Item picked up");
    }


    protected override void OnInitializeMod()
    {
        // Do your early mod initialization which doesn't involve game or sdk references here
        Config.Init();
    }

    protected override void OnSdkInitialized()
    {
        // Do your mod initialization which involves game or sdk references here
        // This is for stuff like UI creation, event registration etc.
        MyModUi.Create();

        EventRegistry.Register(GameEvent.LocalPlayerDied, (EventRegistry.SubscriberCallback)OnPlayerDied);
        EventRegistry.Register(GameEvent.ItemPickedUp, (EventRegistry.SubscriberCallback)OnItemPickedUp);

        // Add in-game settings ui for your mod.
        // SettingsRegistry.CreateSettings(this, null, typeof(Config));
    }

    protected override void OnGameStart()
    {
        // This is called once the player spawns in the world and gains control.

        // var playerInventory = Object.FindObjectOfType<PlayerInventory>();
        // playerInventory._vitalItemsThatCantBeLost.Add(ItemDatabase.BackpackId);
        //
        // foreach (var itemId in playerInventory._vitalItemsThatCantBeLost)
        // {
        //     var itemData = ItemDatabaseManager.ItemById(itemId);
        //     itemData._dropsOnDeath = false;
        // }

        // var original =
        //     typeof(PlayerDeathCutsceneBase).GetMethod(nameof(PlayerDeathCutsceneBase
        //         .CreateAndPlaceDroppedInventoryBag));
        // var prefix = typeof(MyMod).GetMethod(nameof(PlayerDeathCutsceneBasePatch));
        //
        // PatchMethod(original, prefix);

        var original =
            typeof(PlayerRetrieveDroppedInventoryAction).GetMethod(nameof(PlayerRetrieveDroppedInventoryAction
                .AddInventoryItems));
        var prefix = typeof(MyMod).GetMethod(nameof(FillDroppedInventoryPatch));

        PatchMethod(original, prefix);
            
        RLog.Msg("Mod initialized");

    }

    private void PatchMethod(MethodInfo original, MethodInfo prefix)
    {
        if (original == null)
        {
            RLog.Msg("Could not patch CreateAndPlaceDroppedInventoryBag: original is null");
            return;
        }

        if (prefix == null)
        {
            RLog.Msg("Could not patch CreateAndPlaceDroppedInventoryBag: prefix is null");
            return;
        }

        try
        {
            HarmonyInstance.Patch(original, prefix: new HarmonyMethod(prefix));
        }
        catch (Exception e)
        {
            RLog.Msg("Could not patch CreateAndPlaceDroppedInventoryBag: " + e.Message + "\n" + e.StackTrace);
        }
    }
}