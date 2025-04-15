﻿using MelonLoader;
using UnityEngine;
using Il2CppInterop;
using Il2CppInterop.Runtime.Injection; 
using System.Collections;
using Il2Cpp;
using MelonLoader.TinyJSON;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Il2CppTLD.Gear;
using UnityEngine.Playables;
using System.Runtime.Intrinsics.X86;
using Il2CppTLD.Logging;
using Il2CppNodeCanvas.Tasks.Actions;
using HarmonyLib;



// Digitalzombie — 09/25/2024 5:03 AM  https://discord.com/channels/322211727192358914/347067457564966913/1288441205679722506
// what you need to do is basically:
//  - change the PingComponent to include your projectiles (enum ... new one or include it with the animals)
//  - add the pingcomponent to every projectile (look at how its added to animals in the Harmony.cs)
//  - make sure the projectile is only displayed if its "somewhere" but not in the players inventory

// Digitalzombie — 12/21/2024
// there is an assetbundle integrated in the dll, thats contining the sprites. thats missing on the repo. it needs to be created in the editor and exported. can be loaded as a single file, but embedding it in the dll is a bit cleaner 
// the AiAwakePatch is the wrong one to modify. you would need to add a new one thats patching the Awake method of gearitems and checking it its your desired projectile
// take a look at the candlelight mod, how I add the candle component there in the awake patch 
// please don't scan the entire scene on load for items. thats baaaaad for performance. always try to find better ways to ... well find your objects
// look at the MotionTracker.cs to see how I load embedded assetbundles and if you look in the VS project its in the resources folder
// but thats the compiled version. I would either need to upload the raw files to github and you would need to make a new bundle with all
// or you can make your own new bundle, and load it in addition
// the assetbundle  creating is vanilla unity. a bit easier than the addressables for MC

// Digitalzombie — 1/3/2025 at 5:27 PM
// you did add the arrows to the animal enum, but aren't using it yet. try copying what I do to the animals 
// right now you only get the position of the arrows in the console in the same frame they get instantiated
// they might not be moved to the right position at that point in time yet. probably just too early. thats why your coordinates are all over the place 
// the Pingcomponent that gets added to the animals  updates the position regulary and translates it to the UI
// the position on the UI gets determined just by distance and angle to lookingdirection
// https://discord.com/channels/322211727192358914/347067457564966913/1320148432492691456

namespace MotionTracker
{
    // Let's talk Unity events.  https://gamedevbeginner.com/start-vs-awake-in-unity/
    // I think this is referring to events for the base MonoBehaviour object.
    // Awake, OnEnable, Start, FixedUpdate, Update, LateUpdate,OnDisable, and OnDestroy.
    // From the GearItem object inspector, I don't think there is an OnDisable, Start, or FixedUpdate event.
    // But I think there ARE OnDestroy, Awake, CacheComponents, and ManualUpdate events.

    // This is the hooked GearItem Awake method.  Awake is called for each GearItem early and isn't of a lot of value for us as we need more information
    // for each Arrow GearItem (i.e. Is it in a container or player's inventory) that is not populated at this point.
    // The better place for us is the ManualUpdate method (see below).
    //[HarmonyLib.HarmonyPatch(typeof(GearItem), "Awake")]
    //public class GearItemAwakePatch
    //{
    //    public static void Postfix(ref GearItem __instance)
    //    {
    //        // MelonLogger.Msg("[MotionTracker].Harmony.Postfix.50 See " + __instance.name);  // This could be a lot of log data!

    //        if (__instance.gameObject.name.Contains("Arrow"))
    //        {
    //            // Add the Pingcomponent to the Arrow.  The pingComponent updates the position regulary and translates it to the UI

    //            //MelonLogger.Msg("[MotionTracker].Harmony.Postfix.61 See some kind of Arrow (" + __instance.name + ":" + __instance.m_InstanceID + ") and adding PingComponent to object.");
    //            //__instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Arrow);
    //        }   // Arrow
    //    }
    //}

    // Logging of dropping and picking back up a couple of different arrows.  The fire hardened arrow came from a stack in inventory.  The manufactured
    // arrow was a single arrow not in a stack.  Note the slight differences in code execution.

    // Drop a fire hardened arrow from inventory that was in a stack
    //[06:44:31.783] [MotionTracker].Harmony.GearItemCacheComponentsPatch.Postfix.74  (Fire Hardened Arrow:71276) Inventory=False, Container=False) CacheComponents event.
    //[06:44:34.031][MotionTracker].Harmony.GearItemManualUpdatePatch.Postfix.143  See some kind of Arrow(Fire Hardened Arrow:-427276) and adding PingComponent to object.
    //[06:44:34.035][MotionTracker][MotionTracker].PingComponents.Initialize.205: Initialize pingComponent.name = (GEAR_ArrowHardened)

    // Radar shows arrow and updates periodically
    //[06:44:45.290][MotionTracker][MotionTracker].PingComponents.GetDistanceToPlayer.396: Arrow (GEAR_ArrowHardened:-427276) position is ((-357.72, 31.81, -524.69)) and distance is (-0.02, -0.08, -0.03)
    //[06:44:50.199][MotionTracker][MotionTracker].PingComponents.GetDistanceToPlayer.396: Arrow(GEAR_ArrowHardened:-427276) position is ((-357.72, 31.81, -524.69)) and distance is (-0.02, -0.08, -0.03)
    //[06:44:55.213][MotionTracker][MotionTracker].PingComponents.GetDistanceToPlayer.396: Arrow(GEAR_ArrowHardened:-427276) position is ((-357.72, 31.81, -524.69)) and distance is (-0.02, -0.08, -0.03)
    //[06:45:00.206][MotionTracker][MotionTracker].PingComponents.GetDistanceToPlayer.396: Arrow(GEAR_ArrowHardened:-427276) position is ((-357.72, 31.81, -524.69)) and distance is (-0.02, -0.08, -0.03)
    //[06:45:35.229][MotionTracker][MotionTracker].PingComponents.GetDistanceToPlayer.396: Arrow(GEAR_ArrowHardened:-427276) position is ((-357.72, 31.81, -524.69)) and distance is (-0.02, -0.08, -0.03)

    // Pickup fire hardened arrow
    //[06:45:37.076][MotionTracker].Harmony.GearItemDestroyPatch.Postfix.165  (Fire Hardened Arrow:-427276) OnDestroy event.
    //[06:45:37.089][MotionTracker][MotionTracker].PingComponents.ManualDelete.146: pingComponent.name = (GEAR_ArrowHardened)

    // Drop a manufactured arrow from inventory that was NOT in a stack
    //[06:50:51.891] [MotionTracker].Harmony.GearItemManualUpdatePatch.Postfix.143  See some kind of Arrow(Manufactured Arrow:-19670586) and adding PingComponent to object.
    //[06:50:51.895][MotionTracker][MotionTracker].PingComponents.Initialize.205: Initialize pingComponent.name = (GEAR_ArrowManufactured)

    // Radar shows arrow and updates periodically
    //[06:51:12.416][MotionTracker][MotionTracker].PingComponents.GetDistanceToPlayer.396: Arrow (GEAR_ArrowManufactured:-19670586) position is ((-357.79, 31.81, -524.60)) and distance is (-0.09, -0.08, 0.06)
    //[06:51:17.419][MotionTracker][MotionTracker].PingComponents.GetDistanceToPlayer.396: Arrow(GEAR_ArrowManufactured:-19670586) position is ((-357.79, 31.81, -524.60)) and distance is (-0.09, -0.08, 0.06)
    //[06:51:47.261][MotionTracker][MotionTracker].PingComponents.GetDistanceToPlayer.396: Arrow(GEAR_ArrowManufactured:-19670586) position is ((-357.79, 31.81, -524.60)) and distance is (-0.09, -0.08, 0.06)

    // Pickup manufactured arrow
    //[06:51:55.227][MotionTracker][MotionTracker].PingComponents.ManualDelete.146: pingComponent.name = (GEAR_ArrowManufactured)

    public class MyLogger
    {
        // public static void LogMessage(string message, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string? caller = null)
        public static void LogMessage(string message, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string? caller = null, [CallerFilePath] string? filepath = null)

        {
#if DEBUG
            MelonLogger.Msg(Path.GetFileName(filepath) + ":" + caller + "." + lineNumber + ": " + message);
#endif
        }
    }

    // Lost and Found box

    // public unsafe void Awake()
    [HarmonyLib.HarmonyPatch(typeof(Container), "Awake")]
    public class ContainerAwakePatch
    {
        public static void Postfix(ref Container __instance)
        {
            if (__instance.name.Contains("CONTAINER_InaccessibleGear"))
            {
#if DEBUG
                // This is the Awake event that is called for each InaccessibleGearContainer.
                // MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") CompleteSpawnFromCONSOLE event.");
                MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") Container Awake event.");
#endif
            }
        }
    }

    // public unsafe void Start()
    [HarmonyLib.HarmonyPatch(typeof(Container), "Start")]
    public class ContainerStartPatch
    {
        public static void Postfix(ref Container __instance)
        {
            if (__instance.name.Contains("CONTAINER_InaccessibleGear"))
            {
#if DEBUG
                // This is the Start event that is called for each InaccessibleGearContainer.
                // MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") CompleteSpawnFromCONSOLE event.");
                MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") Container Start event.");
#endif
            }
        }
    }

    // public unsafe void OnEnable()
    [HarmonyLib.HarmonyPatch(typeof(Container), "OnEnable")]
    public class ContainerOnEnablePatch
    {
        public static void Postfix(ref Container __instance)
        {
            if (__instance.name.Contains("CONTAINER_InaccessibleGear"))
            {
#if DEBUG
                // This is the OnEnable event that is called for each InaccessibleGearContainer.
                // MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") CompleteSpawnFromCONSOLE event.");
                MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") Container OnEnable event.");
#endif
            }
        }
    }

    // public unsafe void OnDisable()
    [HarmonyLib.HarmonyPatch(typeof(Container), "OnDisable")]
    public class ContainerOnDisablePatch
    {
        public static void Postfix(ref Container __instance)
        {
            if (__instance.name.Contains("CONTAINER_InaccessibleGear"))
            {
#if DEBUG
                // This is the OnDisable event that is called for each InaccessibleGearContainer.
                // MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") CompleteSpawnFromCONSOLE event.");
                MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") Container OnDisable event.");
#endif
                // If the PingComponent exists, delete it.
                if (__instance.gameObject.GetComponent<PingComponent>())
                {
#if DEBUG
                    MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") PingComponent exists for Lost and Found Box container.  Delete pingComponent to remove from radar.");
#endif
                    PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
                }

            }
        }
    }

    // public unsafe void OnDestroy()
    [HarmonyLib.HarmonyPatch(typeof(Container), "OnDestroy")]
    public class ContainerOnDestroyPatch
    {
        public static void Postfix(ref Container __instance)
        {
            if (__instance.name.Contains("CONTAINER_InaccessibleGear"))
            {
#if DEBUG
                // This is the OnDestroy event that is called for each InaccessibleGearContainer.
                // MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") CompleteSpawnFromCONSOLE event.");
                MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") Container OnDestroy event.");
#endif
                // If the PingComponent exists, delete it.  
                if (__instance.gameObject.GetComponent<PingComponent>())
                {
#if DEBUG
                    MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") PingComponent exists for Lost and Found Box container.  Delete pingComponent to remove from radar.");
#endif
                    PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
                }

            }
        }
    }

    // public unsafe void UpdateContainer()
    [HarmonyLib.HarmonyPatch(typeof(Container), "UpdateContainer")]
    public class ContainerUpdateContainerPatch
    {
        public static void Postfix(ref Container __instance)
        {
            if (__instance.name.Contains("CONTAINER_InaccessibleGear"))
            {
#if DEBUG
                // This is the UpdateContainer event that is called for each InaccessibleGearContainer.
                // MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") CompleteSpawnFromCONSOLE event.");
                // MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") Container UpdateContainer event.");
#endif
                // The Lost and Found Box is active and updating.  Need to add the pingComponent (if not present) to the Lost and Found Box so it shows up on the radar
                if (!__instance.gameObject.GetComponent<PingComponent>())
                {
#if DEBUG
                    MyLogger.LogMessage("See Lost and Found Box container (" + __instance.name + ":" + __instance.GetInstanceID() + ") at " + __instance.transform.position + 
                        " with " + __instance.m_Items.Count + " items and adding PingComponent to object to display on radar.");
#endif
                    __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.LostAndFoundBox);   // Add the PingComponent for the LostAndFoundBox
                }
            }
        }
    }

    //    //     public unsafe void Initialize()
    //    [HarmonyLib.HarmonyPatch(typeof(InaccessibleGearContainer), "Initialize")]
    //    public class InaccessibleGearContainerInitializePatch
    //    {
    //        public static void Postfix(ref InaccessibleGearContainer __instance)
    //        {
    //            // if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal"))
    //            if (true)
    //            {
    //#if DEBUG
    //                // This is the UpdateVisibilityForAll event that is called for each InaccessibleGearContainer.
    //                // MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") CompleteSpawnFromCONSOLE event.");
    //                MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") InaccessibleGearContainer Initialize event.");
    //#endif
    //            }
    //        }
    //    }

    //    //     public unsafe static void UpdateVisibilityForAll()
    //    [HarmonyLib.HarmonyPatch(typeof(InaccessibleGearContainer), "UpdateVisibilityForAll")]
    //    public class InaccessibleGearContainerUpdateVisibilityForAllPatch
    //    {
    //        public static void Postfix(ref InaccessibleGearContainer __instance)
    //        {
    //            // if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal"))
    //            if (true)
    //            {
    //#if DEBUG
    //                // This is the UpdateVisibilityForAll event that is called for each InaccessibleGearContainer.
    //                // MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") CompleteSpawnFromCONSOLE event.");
    //                MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") InaccessibleGearContainer UpdateVisibilityForAll event.");
    //#endif
    //            }
    //        }
    //    }

    //    //     public unsafe static void InitializeAllForCurrentScene()
    //    [HarmonyLib.HarmonyPatch(typeof(InaccessibleGearContainer), "InitializeAllForCurrentScene")]
    //    public class InaccessibleGearContainerInitializeAllForCurrentScenePatch
    //    {
    //        public static void Postfix(ref InaccessibleGearContainer __instance)
    //        {
    //            // if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal"))
    //            if (true)
    //            {
    //#if DEBUG
    //                // This is the InitializeAllForCurrentScene event that is called for each InaccessibleGearContainer.
    //                // MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") CompleteSpawnFromCONSOLE event.");
    //                MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") InaccessibleGearContainer InitializeAllForCurrentScene event.");
    //#endif
    //            }
    //        }
    //    }

    //    [HarmonyLib.HarmonyPatch(typeof(RadialObjectSpawner), "Awake")]
    //    public class RadialObjectSpawnerAwakePatch
    //    {
    //        public static void Postfix(ref RadialObjectSpawner __instance)
    //        {
    //            // if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal"))
    //            if (true)
    //            {
    //#if DEBUG
    //                // This is the Awake event that is called for each RadialObjectSpawner.
    //                // MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") CompleteSpawnFromCONSOLE event.");
    //                MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") RadialObjectSpawner Awake event.");
    //#endif
    //            }
    //        }
    //    }

    //    [HarmonyLib.HarmonyPatch(typeof(RadialObjectSpawner), "Start")]
    //    public class RadialObjectSpawnerStartPatch
    //    {
    //        public static void Postfix(ref RadialObjectSpawner __instance)
    //        {
    //            // if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal"))
    //            if (true)
    //            {
    //#if DEBUG
    //                // This is the Start event that is called for each RadialObjectSpawner.
    //                // MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") CompleteSpawnFromCONSOLE event.");
    //                MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") RadialObjectSpawner Start event.");
    //#endif
    //            }
    //        }
    //    }

    //    [HarmonyLib.HarmonyPatch(typeof(RadialObjectSpawner), "OnDestroy")]
    //    public class RadialObjectSpawnerOnDestroyPatch
    //    {
    //        public static void Postfix(ref RadialObjectSpawner __instance)
    //        {
    //            // if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal"))
    //            if (true)
    //            {
    //#if DEBUG
    //                // This is the OnDestroy event that is called for each RadialObjectSpawner.
    //                // MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") CompleteSpawnFromCONSOLE event.");
    //                MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") RadialObjectSpawner OnDestroy event.");
    //#endif
    //            }
    //        }
    //    }

    //    // public unsafe void ReleaseSpawnedObjectsToPool()
    //    [HarmonyLib.HarmonyPatch(typeof(RadialObjectSpawner), "ReleaseSpawnedObjectsToPool")]
    //    public class RadialObjectReleaseSpawnedObjectsToPoolPatch
    //    {
    //        public static void Postfix(ref RadialObjectSpawner __instance)
    //        {
    //            // if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal"))
    //            if (true)
    //            {
    //#if DEBUG
    //                // This is the OnDestroy event that is called for each RadialObjectSpawner.
    //                // MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") CompleteSpawnFromCONSOLE event.");
    //                MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") RadialObjectSpawner ReleaseSpawnedObjectsToPool event.");
    //#endif
    //            }
    //        }
    //    }

    //    // public unsafe void SpawnAttemptAllNoVisChecks()
    //    [HarmonyLib.HarmonyPatch(typeof(RadialObjectSpawner), "SpawnAttemptAllNoVisChecks")]
    //    public class RadialObjectSpawnAttemptAllNoVisChecksPatch
    //    {
    //        public static void Postfix(ref RadialObjectSpawner __instance)
    //        {
    //            // if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal"))
    //            if (true)
    //            {
    //#if DEBUG
    //                // This is the OnDestroy event that is called for each RadialObjectSpawner.
    //                // MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") CompleteSpawnFromCONSOLE event.");
    //                MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") RadialObjectSpawner SpawnAttemptAllNoVisChecks event.");
    //#endif
    //            }
    //        }
    //    }
    //    // RadialSpawnManager is the object that manages the radial spawns of objects.  For example, it manages the spawning of Coal.  The RadialObjectSpawner may be where each object is spawned.
    //    [HarmonyLib.HarmonyPatch(typeof(RadialSpawnManager), "Start")]
    //    public class RadialSpawnManagerStartPatch
    //    {
    //        public static void Postfix(ref RadialSpawnManager __instance)
    //        {
    //            // if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal"))
    //            if (true)
    //            {
    //#if DEBUG
    //                // This is the Update event that is called for each RadialSpawnManager.
    //                // MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") CompleteSpawnFromCONSOLE event.");
    //                MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") RadialSpawnManager Start event.");
    //#endif
    //            }
    //        }
    //    }

    //    //    public unsafe void Update()
    //    [HarmonyLib.HarmonyPatch(typeof(RadialSpawnManager), "Update")]
    //    public class RadialSpawnManagerUpdatePatch
    //    {
    //        public static void Postfix(ref RadialSpawnManager __instance)
    //        {
    //            // if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal"))
    //            if (true)
    //            {
    //#if DEBUG
    //                // This is the Update event that is called for each RadialSpawnManager.
    //                // MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") CompleteSpawnFromCONSOLE event.");
    //                MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") RadialSpawnManager Update event.");
    //#endif
    //                RadialObjectSpawner[] ros = __instance.GetComponentsInChildren<RadialObjectSpawner>(true);
    //                foreach (RadialObjectSpawner ro in ros)
    //                {
    //                    MyLogger.LogMessage("  (" + __instance.name + ":" + __instance.GetInstanceID() + ") RadialSpawnManager Update event.  RadialObjectSpawner=" + ro.name);
    //                    //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") RadialSpawnManager Update event.  RadialObjectSpawner=" + ro.name + " m_InsideContainer=" + ro.m_InsideContainer);
    //                    //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") RadialSpawnManager Update event.  RadialObjectSpawner=" + ro.name + " m_InPlayerInventory=" + ro.m_InPlayerInventory);
    //                }
    //            }
    //        }
    //    }

    //    // public unsafe void UpdateRadialSpawnObjectsActiveList()
    //    [HarmonyLib.HarmonyPatch(typeof(RadialSpawnManager), "UpdateRadialSpawnObjectsActiveList")]
    //    public class RadialSpawnManagerUpdateRadialSpawnObjectsActiveListPatch
    //    {
    //        public static void Postfix(ref RadialSpawnManager __instance)
    //        {
    //            // if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal"))
    //            if (true)
    //            {
    //#if DEBUG
    //                // This is the UpdateRadialSpawnObjectsActiveList event that is called for each RadialSpawnManager.
    //                // MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") CompleteSpawnFromCONSOLE event.");
    //                MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") RadialSpawnManager UpdateRadialSpawnObjectsActiveList event.");
    //#endif
    //            }
    //        }
    //    }

    //    [HarmonyLib.HarmonyPatch(typeof(GearItem), "CompleteSpawnFromCONSOLE")]
    //    public class GearItemCompleteSpawnFromCONSOLEPatch
    //    {
    //        public static void Postfix(ref GearItem __instance)
    //        {
    //            if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal"))
    //            {
    //#if DEBUG
    //                // This is the OnWield event that is called for each GearItem.  
    //                MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") CompleteSpawnFromCONSOLE event.");
    //#endif
    //            }
    //        }
    //    }

    //    [HarmonyLib.HarmonyPatch(typeof(GearItem), "ForceGUIDSetup")]
    //    public class GearItemForceGUIDSetupPatch
    //    {
    //        public static void Postfix(ref GearItem __instance)
    //        {
    //            if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal"))
    //            {
    //#if DEBUG
    //                // This is the OnWield event that is called for each GearItem.  
    //                MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") ForceGUIDSetup event.");
    //#endif
    //            }
    //        }
    //    }

    //    //     public unsafe virtual void InitializeInteraction()
    //    [HarmonyLib.HarmonyPatch(typeof(GearItem), "InitializeInteraction")]
    //    public class GearItemInitializeInteractionPatch
    //    {
    //        public static void Postfix(ref GearItem __instance)
    //        {
    //            if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal"))
    //            {
    //#if DEBUG
    //                // This is the OnWield event that is called for each GearItem.  
    //                MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") InitializeInteraction event.");
    //#endif
    //            }
    //        }
    //    }

    //    //     public unsafe virtual void UpdateInteraction()
    //    [HarmonyLib.HarmonyPatch(typeof(GearItem), "UpdateInteraction")]
    //    public class GearItemUpdateInteractionPatch
    //    {
    //        public static void Postfix(ref GearItem __instance)
    //        {
    //            if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal"))
    //            {
    //#if DEBUG
    //                // This is the OnWield event that is called for each GearItem.  
    //                MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") UpdateInteraction event.");
    //#endif
    //            }
    //        }
    //    }

    //    [HarmonyLib.HarmonyPatch(typeof(GearItem), "Awake")]
    //    public class GearItemAwakePatch
    //    {
    //        public static void Postfix(ref GearItem __instance)
    //        {
    //            if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal"))
    //            {
    //#if DEBUG
    //                // This is the Awake event that is called for each GearItem.  
    //                // MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") Awake event.");
    //#endif
    //            }
    //        }
    //    }

    //    [HarmonyLib.HarmonyPatch(typeof(GearItem), "OnBeginUnwield")]
    //    public class GearItemOnBeginUnwieldPatch
    //    {
    //        public static void Postfix(ref GearItem __instance)
    //        {
    //            if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal"))
    //            {
    //#if DEBUG
    //                // This is the OnWield event that is called for each GearItem.  
    //                MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") OnBeginUnwield event.");
    //#endif
    //            }
    //        }
    //    }

    //    [HarmonyLib.HarmonyPatch(typeof(GearItem), "OnUnwieldComplete")]
    //    public class GearItemOnUnwieldCompletePatch
    //    {
    //        public static void Postfix(ref GearItem __instance)
    //        {
    //            if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal"))
    //            {
    //#if DEBUG
    //                // This is the OnWield event that is called for each GearItem.  
    //                MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") OnUnwieldComplete event.");
    //#endif
    //            }
    //        }
    //    }

    //    [HarmonyLib.HarmonyPatch(typeof(GearItem), "ManualStart")]
    //    public class GearItemManualStartPatch
    //    {
    //        public static void Postfix(ref GearItem __instance)
    //        {
    //            if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal"))
    //            {
    //#if DEBUG
    //                // This is the ManualStart event that is called for each GearItem.  
    //                MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") ManualStart event.");
    //#endif
    //            }
    //        }
    //    }

    //    //     public unsafe void CacheComponents()
    //    [HarmonyLib.HarmonyPatch(typeof(GearItem), "CacheComponents")]
    //    public class GearItemCacheComponentsPatch
    //    {
    //        public static void Postfix(ref GearItem __instance)
    //        {
    //            // if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal"))
    //            if (__instance.gameObject.name.Contains("Coal"))
    //            {
    //#if DEBUG
    //                // This is the CacheComponents event that is called for each GearItem.  Lot of data!
    //                MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") CacheComponents event.");
    //#endif
    //            }
    //        }
    //    }

    //    // public unsafe void AddGearToContainer(GearItem containedGearItem, GearItem newItem)
    //    // Never see anything in the log for this event.  Not sure what it does.
    //    [HarmonyLib.HarmonyPatch(typeof(GearItem), "AddGearToContainer")]
    //    public class GearItemAddGearToContainerPatch
    //    {
    //        public static void Postfix(ref GearItem __instance, GearItem newItem)
    //        {
    //            if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal"))
    //            {
    //#if DEBUG
    //                // This is the AddGearToContainer event that is called for each GearItem.  
    //                MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") newItem=" + newItem.name + " AddGearToContainer event.");
    //#endif
    //            }
    //        }
    //    }

    //    //     public unsafe void RemoveGearFromContainer(GearItem gi)
    //    // Never see anything in the log for this event.  Not sure what it does.
    //    [HarmonyLib.HarmonyPatch(typeof(GearItem), "RemoveGearFromContainer")]
    //    public class GearItemRemoveGearFromContainerPatch
    //    {
    //        public static void Postfix(ref GearItem __instance)
    //        {
    //            if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal"))
    //            {
    //#if DEBUG
    //                // This is the RemoveGearFromContainer event that is called for each GearItem.  
    //                MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") RemoveGearFromContainer event.");
    //#endif
    //            }
    //        }
    //    }

    // The Illusion — 3:24 PM
    //There is an event system for this. Though I dont know if those events are actually used or not. Unity Explorer will tell you if they are. Other than that, look at the PlayerManager.AddStackedItem or something along those lines

    //    public unsafe bool TryAddToExistingStackable(GearItem gearToAdd, float normalizedCondition, int numUnits, out GearItem existingGearItem)

    [HarmonyLib.HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.TryAddToExistingStackable), [typeof(GearItem), typeof(float), typeof(int), typeof(GearItem)], [ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out])]
    public class PlayerManagerTryAddToExistingStackable
    {
        public static bool Prefix(GearItem gearToAdd, float normalizedCondition, int numUnits, GearItem existingGearItem)
        {
            // This should probably just be for Coal.  The arrow stuff seems to play nicely on the radar.  But leave Arrow in for now.
            if (gearToAdd.gameObject.name.Contains("Arrow") || gearToAdd.gameObject.name.Contains("Coal") || PingComponent.IsRawFish(gearToAdd))
            {
#if DEBUG
                MyLogger.LogMessage("(" + gearToAdd.DisplayName + ":" + gearToAdd.m_InstanceID + ") TryAddToExistingStackable Prefix(GearItem gearToAdd, float normalizedCondition, int numUnits, out GearItem existingGearItem) event.");
#endif
                // Can we check for a PingComponent component here?
                if (gearToAdd.gameObject.GetComponent<PingComponent>() != null)     // The gear to be added has a PingComponent and it's going into inventory or a container.  So need to delete the PingComponent so it does not display on radar.
                {
#if DEBUG
                    MyLogger.LogMessage("   (" + gearToAdd.gameObject.name + ":" + gearToAdd.gameObject.GetInstanceID() + ") PingComponent exists for Gear item going into inventory/container.  Delete pingComponent to remove from radar.");
#endif
                    PingComponent.ManualDelete(gearToAdd.gameObject.GetComponent<PingComponent>()); // Delete PingComponent so it no longer shows on radar.
                }

                // if (GameManager.IsMainMenuActive()) return true;
                // if (gearToAdd.gameObject.GetComponent<StackableItem>() != null) return true;   // Returning true... so this item can be stacked?
                return true;   // Wild guess.  When set to false, coal does not stack in player inventory.  Let's try true.
            }
            return true;    //  Let's try true.
        }
    }

    //     public unsafe bool TryAddToExistingStackable(GearItem gearToAdd, int numUnits, out GearItem existingGearItem)
    // [HarmonyLib.HarmonyPatch(HarmonyLib.MethodType.Normal, MethodSignature.Equals   Type[] {typeof(GearItem), typeof(int), typeof(GearItem) })]
    //    [HarmonyLib.HarmonyPatch(typeof(PlayerManager), "TryAddToExistingStackable")]
    //    public class PlayerManagerTryAddToExistingStackablePatch
    //    {
    //        public static void Postfix(GearItem __instance, int numUnits, out GearItem existingGearItem)
    //        {
    //            if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal"))
    //            {
    //#if DEBUG
    //                MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") TryAddToExistingStackable Postfix(GearItem __instance, int numUnits, GearItem existingGearItem)  event.");
    //#endif
    //            }
    //            existingGearItem = __instance;  // This is not correct.  We need to return the GearItem that was passed in.
    //        }
    //    }

    //    //         public unsafe GearItem AddItemToPlayerInventory(GearItem gi, bool trackItemLooted = true, bool enableNotificationFlag = false)
    //    [HarmonyLib.HarmonyPatch(typeof(PlayerManager), "AddItemToPlayerInventory")]
    //    public class PlayerManagerAddItemToPlayerInventoryePatch
    //    {
    //        public static void Postfix(GearItem __instance, bool trackItemLooted = true, bool enableNotificationFlag = false)
    //        {
    //            if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal"))
    //            {
    //#if DEBUG
    //                // This is the AddItemToPlayerInventory event that is called for each GearItem.  And it returns a GearItem.  Hmmm...  
    //                MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") AddItemToPlayerInventory event.");
    //#endif
    //            }
    //        }
    //    }


    // We are relying on the ManualUpdate event to catch GearItems needing to be tracked or not (instantiate a pingComponent or delete an existing pingComponent.
    // In testing, sometimes an Arrow or Coal on the radar becomes "stuck" at the origin (center) that never updates.
    // Hypothesis: This happens after picking up an Arrow or Coal from the ground.  It goes into inventory (Arrow GearItem object is deleted but Coal is not) but
    // the associated radar icon is not deleted.  Suspect pingComponent does not exist.  But the radar is not cleared of the arrow that was picked up and went into
    // inventory.

    [HarmonyLib.HarmonyPatch(typeof(GearItem), "ManualUpdate")]
    public class GearItemManualUpdatePatch
    {
        public static void Postfix(ref GearItem __instance)
        {
            if (__instance.gameObject.name.Contains("Arrow") 
                || __instance.gameObject.name.Contains("Coal") 
                //|| __instance.gameObject.name.Contains("RawCohoSalmon")
                || PingComponent.IsRawFish(__instance)
                )
            {
#if DEBUG
                //MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") inventory=" + __instance.m_InPlayerInventory + ", container=" + __instance.m_InsideContainer + ") manualupdate event.");
#endif
                if (__instance.m_InsideContainer)
                {
#if DEBUG
                    //MyLogger.LogMessage(" (" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") is inside a container.");
#endif
                    if (__instance.gameObject)
                    {
#if DEBUG
                        //MyLogger.LogMessage("  (" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") gameObject exists.");
#endif
                        if (__instance.gameObject.GetComponent<PingComponent>())
                        {
#if DEBUG
                            //MyLogger.LogMessage("   (" + __instance.name + ":" + __instance.m_InstanceID + ") PingComponent exists for Gear item in container.  Delete pingComponent to remove from radar.");
#endif
                            PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
                        }
                    }
                }
                else if (__instance.m_InPlayerInventory)
                {
#if DEBUG
                    //MyLogger.LogMessage(" (" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") is in player inventory.");
#endif
                    if (__instance.gameObject)
                    {
#if DEBUG
                        //MyLogger.LogMessage("  (" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") gameObject exists.");
#endif
                        if (__instance.gameObject.GetComponent<PingComponent>())
                        {
#if DEBUG
                            //MyLogger.LogMessage("   (" + __instance.name + ":" + __instance.m_InstanceID + ") PingComponent exists for Gear item in inventory.  Delete pingComponent to remove from radar.");
#endif
                            PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
                        }
                    }
                }
                else
                {
                    if (!__instance.gameObject.GetComponent<PingComponent>())
                    {
                        // Gear item (i.e. Arrow) is not in inventory or container and does not have a PingComponent
#if DEBUG
                        //MyLogger.LogMessage("See some kind of wild Gear item (" + __instance.name + ":" + __instance.m_InstanceID + ") at " + __instance.transform.position + " and adding PingComponent to object to display on radar.");
#endif
                        if (__instance.gameObject.name.Contains("Arrow"))
                        {
                            __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Arrow);   // Add the PingComponent for the arrow
                        }
                        else if (__instance.gameObject.name.Contains("Coal"))
                        {
                            __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Coal);   // Add the PingComponent for the coal
                        }
                        else if (PingComponent.IsRawFish(__instance))  // Need an IsRawFish() bool function. 
                        {
                            __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.RawFish);   // Add the PingComponent for a RawFish
                        }
                        //else if (__instance.gameObject.name.Contains("LostAndFound"))   // Nope.  Not a GearItem.  CONTAINER_InaccessibleGear? Il2Cpp.InaccessibleGearContainer?
                        //{
                        //    __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.LostAndFoundBox);   // Add the PingComponent for the LostAndFoundBox
                        //}
                        __instance.gameObject.GetComponent<PingComponent>().attachedGearItem = __instance;              // Pointer to this GearItem object.
                    }

                }
            }   // Arrow, Coal, or other tracked Gear items.

        }
    }

    // OnDestroy is called when a stacked GearItem is returned to inventory.  It's not called when the item is a single (non-stacked).  Not sure why.
    [HarmonyLib.HarmonyPatch(typeof(GearItem), "OnDestroy")]
    public class GearItemDestroyPatch
    { 
        public static void Postfix(ref GearItem __instance)
        {
#if DEBUG
            if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal") || PingComponent.IsRawFish(__instance))
            {
                MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") OnDestroy event.");
            }
#endif

            if (__instance.gameObject.GetComponent<PingComponent>())
            {
#if DEBUG
                if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal") || PingComponent.IsRawFish(__instance))
                {
                    MyLogger.LogMessage("(" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") PingComponent exists.");
                }
#endif

                PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
            }
            else
            {
#if DEBUG
                if (__instance.gameObject.name.Contains("Arrow") || __instance.gameObject.name.Contains("Coal") || PingComponent.IsRawFish(__instance))
                {
                    //MyLogger.LogMessage("No PingComponent to delete."); // Lot of logged data so we limit this to Arrow and Coal.
                }
#endif
            }
        }
    }
    
    [HarmonyLib.HarmonyPatch(typeof(BaseAi), "Start")]
    public class AiAwakePatch   // This should probably be named AiStartPatch.
    {
        public static void Postfix(ref BaseAi __instance)
        {
#if DEBUG
            // MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") BaseAI Start event.");
#endif

            if (__instance.m_CurrentMode == AiMode.Dead || __instance.m_CurrentMode == AiMode.Disabled || __instance.m_CurrentMode == AiMode.None)
            {
#if DEBUG
                //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") AiMode dead, disabled, or none.  No processing.");
#endif
                return;
            }

            if (__instance.m_AiSubType == AiSubType.Moose)
            {
                __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Moose);
                return;
            }
            else if (__instance.m_AiSubType == AiSubType.Bear)
            {
                __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Bear);
                return;
            }
            else if (__instance.m_AiSubType == AiSubType.Cougar)
            {
                __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Cougar);
                return;
            }
            else if (__instance.m_AiSubType == AiSubType.Wolf && (__instance.gameObject.name.Contains("grey") || __instance.gameObject.name.Contains("grey")))
            {
                __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Timberwolf);
                return;
            }
            else if (__instance.m_AiSubType == AiSubType.Wolf)
            {             
                __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Wolf);
                return;
            }
            else if (__instance.m_AiSubType == AiSubType.Stag && !__instance.gameObject.name.Contains("_Doe"))
            {
                __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Stag);
                return;
            }
            else if (__instance.m_AiSubType == AiSubType.Stag && __instance.gameObject.name.Contains("_Doe"))
            {
                __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Doe);
                return;
            }
            else if (__instance.m_SnowImprintType == SnowImprintType.PtarmiganFootprint)
            {
                __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.PuffyBird);
                return;
            }
            else if (__instance.m_AiSubType == AiSubType.Rabbit)
            {
                __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Rabbit);
                return;
            }
            else if (true)
            {
                return;
            }
        }
    }

    // Crows.  They show up on the radar as expected.  But, sometimes the radar animation for crows stops.  Go into a trailer with active crows on the radar
    // and sometimes the radar crow updates stops.  They should be removed from the radar since there are no crows in the trailer.
    // Also, have active crows on the radar.  Pass time until night.  The crows go away when it's dark.  But the radar shows the non-updating
    // crow artifacts.  UE shows no pingComponents after crows despawn at night.  So, need to figure out how the radar is cleared up when an item despawns.
    // The radar (PingManager) has an iconContainer which contains the radar icons.  The radar icons are Image objects.  The radar icons visibility are updated in the PingManager Update method.
    [HarmonyLib.HarmonyPatch(typeof(Il2Cpp.FlockChild), "Start")]
    public class FlockPatch
    {
        // public static void Postfix(ref BaseAi __instance)
        public static void Postfix(ref FlockChild __instance)
        {
            __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Crow);        // Hmmm... is this the right place to add the PingComponent?
#if DEBUG
            //MyLogger.LogMessage("FlockChild Start event. FlockChild:ID:Position (" + __instance.name + ":" + __instance.GetInstanceID() + ":" + __instance.transform.position + ") " +
            //                    "GameObject:ID:Position (" + __instance.gameObject.name + ":" + __instance.gameObject.GetInstanceID() + ":" + __instance.gameObject.transform.position + ")" );
#endif
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Il2Cpp.FlockChild), "Update")]
    public class FlockUpdatePatch
    {
        // public unsafe virtual void Update()
        public static void Postfix(ref FlockChild __instance)
        {
            //__instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Crow);

#if DEBUG
                //MyLogger.LogMessage("FlockChild Update event. FlockChild:ID:Position (" + __instance.name + ":" + __instance.GetInstanceID() + ":" + __instance.transform.position + ") " +
                //                    "GameObject:ID:Position (" + __instance.gameObject.name + ":" + __instance.gameObject.GetInstanceID() + ":" + __instance.gameObject.transform.position + ")");  // Lot of data!
#endif
            }
        }

    //  Fails Harmony Patching
    //    [HarmonyLib.HarmonyPatch(typeof(Il2Cpp.FlockChild), "System_IDisposable_Dispose")]
    //    public class Flock_System_IDisposable_Dispose_Patch
    //    {
    //        public static void Postfix(ref FlockChild __instance)
    //        {
    //            //__instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Crow);
    //#if DEBUG
    //            MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") FlockChild System_IDisposable_Dispose event.");
    //#endif
    //        }
    //    }

    //  Fails Harmony Patching
    // Nothing 
    //    [HarmonyLib.HarmonyPatch(typeof(Il2Cpp.FlockChild), "MoveNext")]
    //    public class Flock_MoveNext_Patch
    //    {
    //        public static void Postfix(ref FlockChild __instance)
    //        {
    //            //__instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Crow);
    //#if DEBUG
    //            MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") FlockChild MoveNext event.");
    //#endif
    //        }
    //    }

    //  Fails Harmony Patching
    // Nothing
//    [HarmonyLib.HarmonyPatch(typeof(Il2Cpp.FlockChild), "System_Collections_IEnumerator_Reset")]
//    public class Flock_System_Collections_IEnumerator_Reset_Patch
//    {
//        public static void Postfix(ref FlockChild __instance)
//        {
//            //__instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Crow);
//#if DEBUG
//            MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") FlockChild System_Collections_IEnumerator_Reset event.");
//#endif
//        }
//    }


    [HarmonyLib.HarmonyPatch(typeof(Il2Cpp.FlockController), "Start")]
    public class FlockController_Start_Patch
    {
        // public static void Postfix(ref BaseAi __instance)
        // public unsafe virtual void
        public static void Postfix(ref FlockController __instance)
        {
#if DEBUG
            //MyLogger.LogMessage("FlockController:ID (" + __instance.name + ":" + __instance.GetInstanceID() + ") " +
            //                    "GameObject:ID (" + __instance.gameObject.name + ":" + __instance.gameObject.GetInstanceID() + ")" +
            //                    " FlockController Start event.");

#endif
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Il2Cpp.FlockController), "Update")]
    public class FlockController_Update_Patch
    {
        // public static void Postfix(ref BaseAi __instance)
        public static void Postfix(ref FlockController __instance)
        {
            //__instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Crow);
#if DEBUG
            // MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") FlockController Update event.");
#endif
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Il2Cpp.FlockController), "destroyBirds")]
    public class FlockController_destroyBirds_Patch
    {
        // public unsafe virtual void destroyBirds()
        public static void Postfix(ref FlockController __instance)
        {
#if DEBUG
            //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") FlockController destroyBirds event.");
#endif
            PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
        }
    }

//    [HarmonyLib.HarmonyPatch(typeof(Il2Cpp.FlockController), "OnDrawGizmos")]
//    public class FlockController_OnDrawGizmos_Patch
//    {
//        // public static void Postfix(ref BaseAi __instance)
//        public static void Postfix(ref FlockController __instance)
//        {
//#if DEBUG
//            MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") FlockController OnDrawGizmos event.");
//#endif

//        }
//    }


    [HarmonyLib.HarmonyPatch(typeof(BaseAi), "EnterDead")]
    public class DeathPatch
    {
        public static void Postfix(ref BaseAi __instance)
        {
#if DEBUG
            //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") BaseAi EnterDead event.");
#endif
            PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());           
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(BaseAi), "OnDisable")]
    public class DeathPatch2
    {
        public static void Postfix(ref BaseAi __instance)
        {
#if DEBUG
            //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") BaseAi OnDisable event.");
#endif
            PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
        }
    }

    // Despawn not seen yet.  Switch to Prefix to see if it helps show up?
    [HarmonyLib.HarmonyPatch(typeof(BaseAi), "Despawn")]
    public class DeathPatch3
    {
        //     public unsafe void Despawn()

        public static void Prefix(ref BaseAi __instance)
        {
#if DEBUG
            //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") BaseAi Despawn event.");
#endif
            PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(BaseAi), "ProcessDead")]
    public class ProcessDeadPatch
    {
        public static void Postfix(ref BaseAi __instance)
        {
#if DEBUG
            // MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") BaseAi ProcessDead event.");    // Lot of data!
#endif
            PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(BaseAi), "ExitDead")]
    public class ExitDeadPatch
    {
        public static void Postfix(ref BaseAi __instance)
        {
#if DEBUG
            //MyLogger.LogMessage("(" + __instance.name + ":" + __instance.GetInstanceID() + ") BaseAi ExitDead event.");
#endif
            PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
        }
    }


    [HarmonyLib.HarmonyPatch(typeof(Panel_Base), "Enable", new Type[] { typeof(bool)})]
    public class PanelPatch
    {
        public static void Postfix(ref Panel_Base __instance, bool enable)
        {
#if DEBUG
            MyLogger.LogMessage("Panel_Base enabled.");
#endif
            PingManager.inMenu = enable;
        }
    }
   

    [HarmonyLib.HarmonyPatch(typeof(DynamicDecalsManager), "TrySpawnDecalObject", new Type[] { typeof(DecalProjectorInstance) })]
    public class TrySpawnDecalObjectPatch
    {
        public static void Postfix(ref DynamicDecalsManager __instance, ref DecalProjectorInstance decalInstance)
        {
            if (decalInstance.m_DecalProjectorType == DecalProjectorType.SprayPaint)
                {
                    Vector3 position;
                    Quaternion rotation;
                    Vector3 vector;
                    __instance.CalculateDecalTransform(decalInstance, null, out position, out rotation, out vector);

                    GameObject decalContainer = new GameObject("DecalContainer");
                    decalContainer.transform.position = position;
                    decalContainer.transform.rotation = rotation;

                    decalContainer.AddComponent<PingComponent>().Initialize(decalInstance.m_ProjectileType);
                }
        }
    }

   

}
