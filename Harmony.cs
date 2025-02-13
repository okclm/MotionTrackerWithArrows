using MelonLoader;
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

    [HarmonyLib.HarmonyPatch(typeof(GearItem), "CacheComponents")]
    public class GearItemCacheComponentsPatch
    {
        public static void Postfix(ref GearItem __instance)
        {
            if (__instance.gameObject.name.Contains("Arrow"))
            {
                #if DEBUG
                    MelonLogger.Msg("[MotionTracker].Harmony.GearItemCacheComponentsPatch.Postfix.74  (" + __instance.DisplayName + ":" + __instance.m_InstanceID +
                        ") Inventory=" + __instance.m_InsideContainer + ", Container=" + __instance.m_InPlayerInventory + ") CacheComponents event.");
                #endif
            }
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(GearItem), "ManualUpdate")]
    public class GearItemManualUpdatePatch
    {
        public static void Postfix(ref GearItem __instance)
        {
            if (__instance.gameObject.name.Contains("Arrow"))
            {
                #if DEBUG
                    //MelonLogger.Msg("[MotionTracker].Harmony.GearItemManualUpdatePatch.Postfix.89  (" + __instance.DisplayName + ":" + __instance.m_InstanceID +
                    //    ") Inventory=" + __instance.m_InPlayerInventory + ", Container=" + __instance.m_InsideContainer + ") ManualUpdate event.");
                #endif

                if (__instance.m_InsideContainer)
                {
                    #if DEBUG
                        //MelonLogger.Msg(" [MotionTracker].Harmony.GearItemManualUpdatePatch.Postfix.96  (" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") is inside a container.");
                    #endif

                    if (__instance.gameObject)
                    {
                        #if DEBUG
                            //MelonLogger.Msg("  [MotionTracker].Harmony.GearItemManualUpdatePatch.Postfix.102  (" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") gameObject exists.");
                        #endif

                        if (__instance.gameObject.GetComponent<PingComponent>())
                        {
                            #if DEBUG
                                //MelonLogger.Msg("   [MotionTracker].Harmony.GearItemManualUpdatePatch.Postfix.108  (" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") PingComponent exists.");
                            #endif

                            PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
                        }
                    }
                }
                else if (__instance.m_InPlayerInventory)
                {
                    #if DEBUG
                        //MelonLogger.Msg(" [MotionTracker].Harmony.GearItemManualUpdatePatch.Postfix.118  (" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") is in player inventory.");
                    #endif

                    if (__instance.gameObject)
                    {
                        #if DEBUG
                            //MelonLogger.Msg("  [MotionTracker].Harmony.GearItemManualUpdatePatch.Postfix.124  (" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") gameObject exists.");
                        #endif

                        if (__instance.gameObject.GetComponent<PingComponent>())
                        {
                            #if DEBUG
                                //MelonLogger.Msg("   [MotionTracker].Harmony.GearItemManualUpdatePatch.Postfix.130  (" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") PingComponent exists.");
                            #endif

                            PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
                        }
                    }
                }
                else
                {
                    if (!__instance.gameObject.GetComponent<PingComponent>())
                    {
                        // Arrow is not in inventory or container and does not have a PingComponent
                        #if DEBUG
                            MelonLogger.Msg("[MotionTracker].Harmony.GearItemManualUpdatePatch.Postfix.143  See some kind of Arrow (" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") and adding PingComponent to object.");
                        #endif

                        __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Arrow);   // Add the PingComponent for the arrow
                        __instance.gameObject.GetComponent<PingComponent>().attachedGearItem = __instance;              // Pointer to this GearItem object.
                    }

                }
            }   // Arrow

        }
    }

    // OnDestroy is called when a stacked GearItem is returned to inventory.  It's not called when the item is a single (non-stacked).  Not sure why.
    [HarmonyLib.HarmonyPatch(typeof(GearItem), "OnDestroy")]
    public class GearItemDestroyPatch
    { 
        public static void Postfix(ref GearItem __instance)
        {
            if (__instance.gameObject.GetComponent<PingComponent>())
            {
                #if DEBUG
                    MelonLogger.Msg("[MotionTracker].Harmony.GearItemDestroyPatch.Postfix.165  (" + __instance.DisplayName + ":" + __instance.m_InstanceID + ") OnDestroy event.");
                #endif

                PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
            }
            else
            {
                // MelonLogger.Msg("[MotionTracker].Harmony.GearItemDestroyPatch.Postfix.146 No PingComponent to delete."); // Lot of logged data
            }
        }
    }
    
    [HarmonyLib.HarmonyPatch(typeof(BaseAi), "Start")]
    public class AiAwakePatch
    {
        public static void Postfix(ref BaseAi __instance)
        {
            if (__instance.m_CurrentMode == AiMode.Dead || __instance.m_CurrentMode == AiMode.Disabled || __instance.m_CurrentMode == AiMode.None)
            {
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

    [HarmonyLib.HarmonyPatch(typeof(Il2Cpp.FlockChild), "Start")]
    public class FlockPatch
    {
        public static void Postfix(ref BaseAi __instance)
        {
            __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Crow);
        }
    }


    [HarmonyLib.HarmonyPatch(typeof(BaseAi), "EnterDead")]
    public class DeathPatch
    {
        public static void Postfix(ref BaseAi __instance)
        {
            PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());           
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(BaseAi), "OnDisable")]
    public class DeathPatch2
    {
        public static void Postfix(ref BaseAi __instance)
        {
            PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Panel_Base), "Enable", new Type[] { typeof(bool)})]
    public class PanelPatch
    {
        public static void Postfix(ref Panel_Base __instance, bool enable)
        {
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
