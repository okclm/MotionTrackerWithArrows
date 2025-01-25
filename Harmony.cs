using MelonLoader;
using UnityEngine;
using Il2CppInterop;
using Il2CppInterop.Runtime.Injection; 
using System.Collections;
using Il2Cpp;
using MelonLoader.TinyJSON;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;


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

    [HarmonyLib.HarmonyPatch(typeof(GearItem), "Awake")]
    // [HarmonyLib.HarmonyPatch(typeof(GearItem), "Start")]
    public class GearItemAwakePatch
    {
        //public void LogMessage(string message, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        //{
        //    MelonLogger.Msg("." + caller + "." + lineNumber + ": " + message);
        //}

        public static void Postfix(ref GearItem __instance)
        {
            // MelonLogger.Msg("[MotionTracker].Harmony.Postfix.50 See " + __instance.name);  // This could be a lot of log data!

            if (__instance.gameObject.name.Contains("Arrow"))
            {
                // Add the Pingcomponent to the Arrow.  The pingComponent updates the position regulary and translates it to the UI

                // Don't add PingComponent if this is NOT a cloned item.
                //if (__instance.gameObject.name.Contains("(Clone)"))
                //{
                //    MelonLogger.Msg("[MotionTracker].Harmony.Postfix.59 See some kind of cloned Arrow (" + __instance.name + ") and adding PingComponent to object.");
                //    __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Arrow);
                //}
                //else  
                //{
                //    MelonLogger.Msg("[MotionTracker].Harmony.Postfix.64 See some kind of NON-CLONED Arrow (" + __instance.name + ") so NOT adding PingComponent to object.");
                //}

                MelonLogger.Msg("[MotionTracker].Harmony.Postfix.67 See some kind of Arrow (" + __instance.name + ") and adding PingComponent to object.");
                __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Arrow);

                // return; // Enough.  Return.

                if (__instance.gameObject.name.Contains("GEAR_Arrow"))
                {
                    MelonLogger.Msg("[MotionTracker].Harmony.Postfix.74 " + __instance.gameObject.name + " seen at " + __instance.gameObject.transform.position);
                }
                else if (__instance.gameObject.name.Contains("GEAR_BrokenArrow"))
                {
                    MelonLogger.Msg("[MotionTracker].Harmony.Postfix.78 " + __instance.gameObject.name + " seen at " + __instance.gameObject.transform.position);
                }
                else if (__instance.gameObject.name.Contains("GEAR_ArrowHardened"))
                {
                    MelonLogger.Msg("[MotionTracker].Harmony.Postfix.82 " + __instance.gameObject.name + " seen at " + __instance.gameObject.transform.position);
                }
                else if (__instance.gameObject.name.Contains("GEAR_BrokenArrowHardened"))
                {
                    MelonLogger.Msg("[MotionTracker].Harmony.Postfix.86 " + __instance.gameObject.name + " seen at " + __instance.gameObject.transform.position);
                }
                else if (__instance.gameObject.name.Contains("GEAR_ArrowManufactured"))
                {
                    MelonLogger.Msg("[MotionTracker].Harmony.Postfix.90 " + __instance.gameObject.name + " seen at " + __instance.gameObject.transform.position);
                }
                else if (__instance.gameObject.name.Contains("GEAR_BrokenArrowManufactured"))
                {
                    MelonLogger.Msg("[MotionTracker].Harmony.Postfix.94 " + __instance.gameObject.name + " seen at " + __instance.gameObject.transform.position);
                }

            }   // Arrow
        }
    }

    // Let's talk Unity events.  https://gamedevbeginner.com/start-vs-awake-in-unity/
    // Awake, OnEnable, Start, FixedUpdate, Update, LateUpdate,OnDisable, and OnDestroy.
    // From the object inspector, I think there is not an OnDisable event.
    // But I think there IS an OnDestroy event.

    //[HarmonyLib.HarmonyPatch(typeof(GearItem), "OnDisable")]
    //public class GearItemDisablePatch
    //{
    //    public static void Postfix(ref GearItem __instance)
    //    {
    //        MelonLogger.Msg("[MotionTracker].Harmony.GearItemDisablePatch.Postfix.97  (" + __instance.DisplayName + ") OnDisable event.");
    //        // PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
    //    }
    //}

    [HarmonyLib.HarmonyPatch(typeof(GearItem), "OnDestroy")]
    public class GearItemDestroyPatch
    { 
        public static void Postfix(ref GearItem __instance)
        {
            if (__instance.gameObject.GetComponent<PingComponent>())
            {
                MelonLogger.Msg("[MotionTracker].Harmony.GearItemDestroyPatch.Postfix.123  (" + __instance.DisplayName + ") OnDestroy event.");
                PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
            }
            else
            {
                // MelonLogger.Msg("[MotionTracker].Harmony.GearItemDestroyPatch.Postfix.123 No PingComponent to delete."); // Lot of logged data
            }
        }
    }


    // Something about deleting arrow pingComponents?  Maybe?  
    //[HarmonyLib.HarmonyPatch(typeof(GearItem), "OnDisable")]
    //public class GearDisablePatch
    //{
    //    public static void Postfix(ref GearItem __instance)
    //    {
    //        __instance.
    //        MelonLogger.Msg("[MotionTracker].Harmony.GearDisablePatch.Postfix.105 ManualDeleting " + __instance.gameObject.name + " seen at " + __instance.gameObject.transform.position);
    //        PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());
    //    }
    //}

    [HarmonyLib.HarmonyPatch(typeof(BaseAi), "Start")]
    public class AiAwakePatch
    {
        public static void Postfix(ref BaseAi __instance)
        {

            // LogMessage("Scene " + sceneName + " was loaded.");


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
            
            // CLM - Arrow!  Arrows don't have AI.  So think this is in the wrong method.
            // else if (__instance.gameObject.name.Contains("_Arrow"))
            // {
            //    // __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Arrow);
            //     return;
            // }
            else if (true)
            {
                // __instance.gameObject.AddComponent<PingComponent>().Initialize(PingManager.AnimalType.Arrow);
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

  
