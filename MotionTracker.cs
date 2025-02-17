using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using Il2CppInterop;
using Il2CppInterop.Runtime.Injection;
using System.Collections;
using Il2Cpp;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Il2CppSystem.Net.ServicePointManager;


namespace MotionTracker
{
    public class MotionTrackerMain : MelonMod
	{
        public static AssetBundle assetBundle;
        public static GameObject motionTrackerParent;
        public static PingManager activePingManager;

        public static GameObject trackerPrefab;
        public static GameObject trackerObject;

        public static GameObject modSettingPage;

        public static Dictionary<PingManager.AnimalType, GameObject> animalPingPrefabs = new Dictionary<PingManager.AnimalType, GameObject>();
        public static Dictionary<ProjectileType, GameObject> spraypaintPingPrefabs = new Dictionary<ProjectileType, GameObject>();

        //public static GameObject arrow;
        // public GameObject player;
        //public Transform arrow2;

        // From https://discussions.unity.com/t/how-to-find-a-child-gameobject-by-name/31255/3
        //public GameObject GetChildGameObject(GameObject fromGameObject, string withName)
        //{
        //    var allKids = fromGameObject.GetComponentsInChildren<Transform>();
        //    var kid = allKids.FirstOrDefault(k => k.gameObject.name == withName);
        //    if (kid == null) return null;
        //    return kid.gameObject;
        //}


        public void LogMessage(string message, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string? caller = null)
        {
            #if DEBUG
                LoggerInstance.Msg("." + caller + "." + lineNumber + ": " + message);
            #endif
        }

        public override void OnInitializeMelon()
        {
            #if DEBUG
                LogMessage("Initializing Melon.");
            #endif

            ClassInjector.RegisterTypeInIl2Cpp<TweenManager>();
            ClassInjector.RegisterTypeInIl2Cpp<PingManager>();
            ClassInjector.RegisterTypeInIl2Cpp<PingComponent>();
            LoadEmbeddedAssetBundle();

            MotionTracker.Settings.OnLoad();
        }

        public static void LoadEmbeddedAssetBundle()
        {
            MemoryStream memoryStream;
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MotionTracker.Resources.motiontracker");
            memoryStream = new MemoryStream((int)stream.Length);
            stream.CopyTo(memoryStream);

            assetBundle = AssetBundle.LoadFromMemory(memoryStream.ToArray());

        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
		{
            // LoggerInstance.Msg($"Scene {sceneName} with build index {buildIndex} has been loaded!");    // CLM

            if (sceneName.Contains("MainMenu"))
            {
                //SCRIPT_InterfaceManager/_GUI_Common/Camera/Anchor/Panel_OptionsMenu/Pages/ModSettings/GameObject/ScrollPanel/Offset/

                PingManager.inMenu = true;
                
                #if DEBUG
                    LogMessage("Scene name containing MainMenu " + sceneName + " was loaded. (1)");
                #endif

                FirstTimeSetup();
            }
            else if (sceneName.Contains("SANDBOX") && motionTrackerParent)
            {
                /// Debug.Log("MotionTracker.cs:OnSceneWasLoaded: Contains SANDBOX");
                #if DEBUG
                    LogMessage("Scene name containing SANDBOX " + sceneName + " was loaded. (2)");
                #endif

                if (PingManager.instance)
                {
#if DEBUG
                    MelonLogger.Msg("[MotionTracker].MotionTracker.OnSceneWasLoaded.101 Scene name containing SANDBOX " + sceneName + " was loaded.");
#endif
                    PingManager.instance.ClearIcons();
                    PingManager.inMenu = false;
                }
            }
            else
            {
                #if DEBUG
                    LogMessage("Uninteresting scene " + sceneName + " was loaded. (3)");
                #endif
            }
        }

        public void FirstTimeSetup()
        {
            if (!motionTrackerParent)
            {
                motionTrackerParent = new GameObject("MotionTracker");
                trackerObject = UnityEngine.Object.Instantiate(assetBundle.LoadAsset<GameObject>("MotionTracker"), motionTrackerParent.transform);
                GameObject.DontDestroyOnLoad(motionTrackerParent);

                activePingManager = motionTrackerParent.AddComponent<PingManager>();

                GameObject prefabSafe = new GameObject("PrefabSafe");
                prefabSafe.transform.parent = motionTrackerParent.transform;
                animalPingPrefabs = new Dictionary<PingManager.AnimalType, GameObject>();
                animalPingPrefabs.Add(PingManager.AnimalType.Crow, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("crow"), prefabSafe.transform));
                animalPingPrefabs.Add(PingManager.AnimalType.Rabbit, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("rabbit"), prefabSafe.transform));
                animalPingPrefabs.Add(PingManager.AnimalType.Wolf, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("wolf"), prefabSafe.transform));
                animalPingPrefabs.Add(PingManager.AnimalType.Timberwolf, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("timberwolf"), prefabSafe.transform));
                animalPingPrefabs.Add(PingManager.AnimalType.Bear, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("bear"), prefabSafe.transform));
                animalPingPrefabs.Add(PingManager.AnimalType.Cougar, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("bear"), prefabSafe.transform));  // This needs a Cougar asset.  Just use Bear for now.
                animalPingPrefabs.Add(PingManager.AnimalType.Moose, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("moose"), prefabSafe.transform));
                animalPingPrefabs.Add(PingManager.AnimalType.Stag, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("stag"), prefabSafe.transform));
                animalPingPrefabs.Add(PingManager.AnimalType.Doe, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("doe"), prefabSafe.transform));
                animalPingPrefabs.Add(PingManager.AnimalType.PuffyBird, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("ptarmigan"), prefabSafe.transform));

                // CLM - Arrows!
                // Throws exception.  The asset bundle contains user developed sprites.  No Arrow sprite... so would need to either
                // add an Arrow sprite and rebuild the asset bundle or create a separate asset bundle for the arrow and Cougar and anything else.
                // 
                // animalPingPrefabs.Add(PingManager.AnimalType.Arrow, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("arrow"), prefabSafe.transform));
                
                animalPingPrefabs.Add(PingManager.AnimalType.Arrow, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("rabbit"), prefabSafe.transform));  // This needs an Arrow asset.  Just use rabbit for now.


                spraypaintPingPrefabs = new Dictionary<ProjectileType, GameObject>();
                spraypaintPingPrefabs.Add(ProjectileType.SprayPaint_Direction, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("SprayPaint_Direction"), prefabSafe.transform));
                spraypaintPingPrefabs.Add(ProjectileType.SprayPaint_Clothing, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("SprayPaint_Clothing"), prefabSafe.transform));
                spraypaintPingPrefabs.Add(ProjectileType.SprayPaint_Danger, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("SprayPaint_Danger"), prefabSafe.transform));
                spraypaintPingPrefabs.Add(ProjectileType.SprayPaint_DeadEnd, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("SprayPaint_DeadEnd"), prefabSafe.transform));
                spraypaintPingPrefabs.Add(ProjectileType.SprayPaint_Avoid, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("SprayPaint_Avoid"), prefabSafe.transform));
                spraypaintPingPrefabs.Add(ProjectileType.SprayPaint_FirstAid, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("SprayPaint_FirstAid"), prefabSafe.transform));
                spraypaintPingPrefabs.Add(ProjectileType.SprayPaint_FoodDrink, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("SprayPaint_FoodDrink"), prefabSafe.transform));
                spraypaintPingPrefabs.Add(ProjectileType.SprayPaint_FireStarting, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("SprayPaint_FireStarting"), prefabSafe.transform));
                spraypaintPingPrefabs.Add(ProjectileType.SprayPaint_Hunting, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("SprayPaint_Hunting"), prefabSafe.transform));
                spraypaintPingPrefabs.Add(ProjectileType.SprayPaint_Materials, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("SprayPaint_Materials"), prefabSafe.transform));
                spraypaintPingPrefabs.Add(ProjectileType.SprayPaint_Storage, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("SprayPaint_Storage"), prefabSafe.transform));
                spraypaintPingPrefabs.Add(ProjectileType.SprayPaint_Tools, GameObject.Instantiate(assetBundle.LoadAsset<GameObject>("SprayPaint_Tools"), prefabSafe.transform));

                foreach (KeyValuePair<PingManager.AnimalType, GameObject> singlePrefab in animalPingPrefabs)
                {
                    singlePrefab.Value.active = false;
                }

                foreach (KeyValuePair<ProjectileType, GameObject> singlePrefab in spraypaintPingPrefabs)
                {
                    singlePrefab.Value.active = false;
                }

                GameObject.DontDestroyOnLoad(prefabSafe);
            }
        }

        public static GameObject GetAnimalPrefab(PingManager.AnimalType animalType)
        {  
            return animalPingPrefabs[animalType];
        }

        public static GameObject GetSpraypaintPrefab(ProjectileType pingType)
        {
            return spraypaintPingPrefabs[pingType];
        }

        public override void OnUpdate()
		{
            if (Settings.options.displayStyle == Settings.DisplayStyle.Toggle)
            {
                if (InputManager.GetKeyDown(InputManager.m_CurrentContext, Settings.options.toggleKey))
                {
                    if (PingManager.instance)
                    {
                        Settings.toggleBool = !Settings.toggleBool;                       
                    }
                }
            }       
        }
    }
}