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


namespace MotionTracker
{

    [HarmonyLib.HarmonyPatch(typeof(GearItem), "Awake")]
    public class GearItemAwakePatch
    {
        //public void LogMessage(string message, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null)
        //{
        //    MelonLogger.Msg("." + caller + "." + lineNumber + ": " + message);
        //}

        public static void Postfix(ref GearItem __instance)
        {
            // MelonLogger.Msg("See " + __instance.name);  // This could be a lot of log data!

            if (__instance.name.Contains("Arrow"))
            {
                // MelonLogger.Msg("See " + __instance.name);
                // MelonLogger.Msg("See " + __instance.name);

                if (__instance.m_InPlayerInventory)
                {
                    MelonLogger.Msg("See " + __instance.name + " in the player's inventory.");
                }
                else
                {
                    MelonLogger.Msg("See " + __instance.name + " not in the player's inventory.");
                }

                if (__instance.gameObject.name.Contains("GEAR_Arrow"))
                {
                    MelonLogger.Msg("GEAR_Arrow seen at " + __instance.gameObject.transform.position);
                }
                else if (__instance.gameObject.name.Contains("GEAR_BrokenArrow"))
                {
                    MelonLogger.Msg("GEAR_BrokenArrow seen at " + __instance.gameObject.transform.position);
                }
                else if (__instance.gameObject.name.Contains("GEAR_ArrowHardened"))
                {
                    MelonLogger.Msg("GEAR_ArrowHardened seen at " + __instance.gameObject.transform.position);
                }
                else if (__instance.gameObject.name.Contains("GEAR_BrokenArrowHardened"))
                {
                    MelonLogger.Msg("GEAR_BrokenArrowHardened seen at " + __instance.gameObject.transform.position);
                }
                else if (__instance.gameObject.name.Contains("GEAR_ArrowManufactured"))
                {
                    MelonLogger.Msg("GEAR_ArrowManufactured seen at " + __instance.gameObject.transform.position);
                }
                else if (__instance.gameObject.name.Contains("GEAR_BrokenArrowManufactured"))
                {
                    MelonLogger.Msg("GEAR_BrokenArrowManufactured seen at " + __instance.gameObject.transform.position);
                }

            }
        }
    }


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

  
