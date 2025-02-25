using MelonLoader;
using UnityEngine;
using Il2CppInterop;
using Il2CppInterop.Runtime.Injection; 
using System.Collections;
using Il2Cpp;
using UnityEngine.UI;
using System.Runtime.CompilerServices;
using System.Buffers.Text;
using Il2CppNewtonsoft.Json;
using Il2CppSystem.Diagnostics.CodeAnalysis;

namespace MotionTracker
{
    public class PingManager : MonoBehaviour
    {
        public PingManager(IntPtr intPtr) : base(intPtr)
        {
        }
        // public enum AnimalType { Crow, Rabbit, Stag, Doe, Wolf, Timberwolf, Bear, Moose, PuffyBird };
        
        // CLM - Hack... adding Arrow to the AnimalType enum so we can look for Arrows in the scene
        // CLM - adding Cougar to the AnimalType enum so we can track cougars in the scene
        public enum AnimalType { Crow, Rabbit, Stag, Doe, Wolf, Timberwolf, Bear, Moose, PuffyBird, Cougar, Arrow };

        public static bool isVisible = false;
        public static PingManager? instance;

        public RectTransform iconContainer, radarUI;
        public Image backgroundImage;
        public Canvas trackerCanvas;

        public bool applyRotation = true;
        public static bool inMenu = false;

        float timer = 0f;           // Accumulate the time since last frame so we can do things after the trigger duration is elapsed (triggerTime).
        float triggerTime = 5f;     // Trigger duration.  When the acculated frame time is equal to or exceeds this value, we do stuff and reset the timer to zero.  (Don't set too low or you'll be doing stuff every frame.)

        // Trying to determine when a radar icon is not being updated and can be presumed orphaned.
        public Vector3 lastTransformPosition = Vector3.zero;   // This will track the last position of the radar icon screen position.
        public int stuckPositionCounter = 0;    // This will track the number of times the radar icon is in the same position.
        public Dictionary<int, Vector3> iconPosition = new Dictionary<int, Vector3>();  // This will track the position of the radar icon for each icon instance ID.

        public void LogMessage(string message, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string? caller = null, [CallerFilePath] string? filepath = null)
        {
#if DEBUG
         MelonLogger.Msg(Path.GetFileName(filepath) + ":" + caller + "." + lineNumber + ": " + message);
#endif
        }
        public void ClearIcons()
        {
            Image[] icons = iconContainer.transform.GetComponentsInChildren<Image>();
#if DEBUG
            LogMessage("iconContainer has " + icons.Count() + " elements.");
#endif
            // int x = icons.Count();

            foreach (Image icon in icons) 
            { 
                if (icon.gameObject == null) 
                {
#if DEBUG
                    LogMessage("(" + icon.name +") icon.gameObject == null"); 
#endif
                }

                Destroy(icon.gameObject);
            }
            /// Debug.Log("PingManager.cs:ClearIcons: Cleared Icons");
#if DEBUG
            LogMessage("Clearing the iconPosition dictionary so we can start fresh with new icons.");
#endif 
            iconPosition.Clear();  // Clear the iconPosition dictionary so we can start fresh with new icons.
#if DEBUG
            LogMessage("Cleared Icons.");
#endif
        }

        public void Update()
        {
            timer += Time.deltaTime;    // Accumulated time since we last logged stuff

            // Let's check the status of the radar screen icons.
            // Can we determine that gameObject instanceID no longer exists in the scene?
            if (timer >= triggerTime)
            {
                int i = 0;
                Image[] icons = iconContainer.transform.GetComponentsInChildren<Image>();                        // What sort of magic is this!?  Get all the Image components in the iconContainer.
                LogMessage("PingManager Update. iconContainer has " + icons.Count() + " icon elements.");

                foreach (Image icon in icons)
                {
                    if (icon != null)
                    {
                        if (icon.name.Contains( $"crow", StringComparison.CurrentCultureIgnoreCase)) // Use this to limit type of icon we are tracking / cleaning up
                        {
                            // LogMessage("Crow icon # " + i + " (" + icon.name + ":" + GetInstanceID() + ");  gameObject (" + icon.gameObject.name + ":" + icon.gameObject.GetInstanceID() + ") seen.");
                            // iconPosition[icon.gameObject.GetInstanceID()] = icon.gameObject.transform.position;

#if DEBUG
                            LogMessage("iconContainer icon # " + i + " Icon:ID (" + icon.name + ":" + icon.GetInstanceID() + ") " +
                                                "GameObject:ID (" + icon.gameObject.name + ":" + icon.gameObject.GetInstanceID() + ") " +
                                                "GameObject:Position (" + icon.gameObject.transform.position + ")");
#endif
                            // Determine if this icon is orphaned.
                            // lastTransformPosition = Vector3.zero;   // Zero out the last position of the radar icon screen position.
                            if (iconPosition.TryGetValue(icon.gameObject.GetInstanceID(), value: out lastTransformPosition))
                            {
                                // lastTransformPosition has the last position of the radar icon screen position.
                                LogMessage("iconContainer icon # " + i + " GameObject:ID(" + icon.gameObject.name + ":" + icon.gameObject.GetInstanceID() + ") is in iconPosition dictionary.");
                            }
                            else
                            {
                                lastTransformPosition = Vector3.zero;   // Zero out the last position of the radar icon screen position.
                                LogMessage("iconContainer icon # " + i + " GameObject:ID(" + icon.gameObject.name + ":" + icon.gameObject.GetInstanceID() + ") is NOT in iconPosition dictionary.");
                            }

                            if (lastTransformPosition == icon.gameObject.transform.position)      // How to determine that this icon is orphaned?
                            {
                                // Remove entry in iconPosition dictionary and delete the icon.
#if DEBUG
                                LogMessage("Stale icon position detected!");
                                LogMessage("icon # " + i + " GameObject:Position (" + icon.gameObject.name + ":" + icon.gameObject.transform.position + ") is the same as last position (" + lastTransformPosition + ") so deleting it.");

                                LogMessage("Cleaning up iconPosition dictionary.  Total key/value pairs in iconPosition dictionary is : " + iconPosition.Count);
#endif
                                if (iconPosition.Remove(icon.gameObject.GetInstanceID()))
                                {
#if DEBUG
                                    LogMessage("Removed key/value (" + icon.gameObject.name + ":" + icon.gameObject.GetInstanceID() + ") from iconPosition dictionary.");
                                    LogMessage("Total key/value pairs in iconPosition dictionary (after deleting entry) is : " + iconPosition.Count);
#endif
                                }
                                else
                                {
#if DEBUG
                                    LogMessage("Failed to remove key/value pair ("+ icon.gameObject.name + ":" + icon.gameObject.GetInstanceID() + ") from iconPosition dictionary.");
                                    LogMessage("Total key/value pairs in iconPosition dictionary (after failed entry deletion) is : " + iconPosition.Count);
#endif
                                }

                                // I don't think there is a pingComponent to delete.  The icon is orphaned because the pingComponent is gone.
                                // And if it does exist, I don't think the icon can get a reference to the pingComponent to delete it.  It's not coupled (parent or child-wise) to the pingComponent.  I might be wrong on this.
                                // PingComponent.ManualDelete(__instance.gameObject.GetComponent<PingComponent>());

                                Destroy(icon.gameObject);
                            }
                            else
                            {
                                if (iconPosition.ContainsKey(icon.gameObject.GetInstanceID()))
                                {
#if DEBUG
                                    LogMessage("Icon # " + i + " GameObject:ID (" + icon.gameObject.name + ":" + icon.gameObject.GetInstanceID() + ") is in iconPosition dictionary.");
                                    LogMessage("Total key/value pairs in iconPosition dictionary is : " + iconPosition.Count + ".  Updating iconPosition entry for GameObject:ID (" + icon.gameObject.name + ":" + icon.gameObject.GetInstanceID() + ")");
#endif
                                    // record latest position of icon in iconPosition dictionary
                                    iconPosition[icon.gameObject.GetInstanceID()] = icon.gameObject.transform.position;
#if DEBUG
                                    LogMessage("Total key/value pairs in iconPosition dictionary (after updating entry) is : " + iconPosition.Count);
#endif
                                }
                                else
                                {
#if DEBUG
                                    LogMessage("Icon # " + i + " GameObject:ID (" + icon.gameObject.name + ":" + icon.gameObject.GetInstanceID() + ") is NOT in iconPosition dictionary.");
                                    LogMessage("Total key/value pairs in iconPosition dictionary is : " + iconPosition.Count + ".  Adding iconPosition entry for GameObject:ID (" + icon.gameObject.name + ":" + icon.gameObject.GetInstanceID() + ")");
#endif
                                    iconPosition[icon.gameObject.GetInstanceID()] = icon.gameObject.transform.position;     // Add the icon to the iconPosition dictionary.
#if DEBUG
                                    LogMessage("Total key/value pairs in iconPosition dictionary (after adding entry) is : " + iconPosition.Count);
#endif
                                }
                            }
                        }
                    }
                    i += 1;
                }

            }

            if (AllowedToBeVisible())
            {
                SetVisible(true);
            }
            else
            {
                SetVisible(false);
            }

            // Check if we need to reset the accumulated time
            if (timer >= triggerTime)
            {
                //LogMessage("timer = (" + timer + ") which is greater than triggerTime = (" + triggerTime + ")");
                //LogMessage("GameManager.GetVpFPSPlayer().gameObject.transform.position = (" + GameManager.GetVpFPSPlayer().gameObject.transform.position + ")");
                timer = 0f;
                //LogMessage("timer reset to 0 (" + timer + ") and triggerTime = (" + triggerTime + ")");
            }
        }

        public bool AllowedToBeVisible()
        {           
            if (!Settings.options.enableMotionTracker)
            {
                return false;
            }

            if (!MotionTrackerMain.modSettingPage)
            {
                MotionTrackerMain.modSettingPage = GameObject.Find("Mod settings grid (Motion Tracker)");               
            }

            if (MotionTrackerMain.modSettingPage)
            {
                if (MotionTrackerMain.modSettingPage.active)
                {
                    return true;
                }
            }

            if (inMenu)
            {
                return false;
            }

            if (Settings.options.displayStyle == Settings.DisplayStyle.Toggle)
            {
                if(!Settings.toggleBool)
                {
                    return false;                    
                }                
            }

            if (!GameManager.GetVpFPSPlayer())
            {
                return false;
            }

            if (!GameManager.GetWeatherComponent())
            {
                return false;
            }

            if (Settings.options.onlyOutdoors)
            {
                if(GameManager.GetWeatherComponent().IsIndoorEnvironment())
                {
                    return false; 
                }
            }
          
            return true;
        }
       
        private void SetVisible(bool visible)
        {
            if (isVisible == visible)
            {
                return;
            }

            if (visible)
            {
                trackerCanvas.enabled = true;
                isVisible = true;
            }
            else
            {
                trackerCanvas.enabled = false;
                isVisible = false;
            }

            // If the user toggles the MotionTracker (On/Off), let's clear all the radar icons to nuke any lingering zombies.
            // Turns out the icons are only created as part of entering the scene.  So deleting them here and they don't return unless you bounce out/in to a scene.
            // Probably need to see if we can deternine individual zombie icons and only delete the zombies.
            //if (PingManager.instance)
            //{
            //    PingManager.instance.ClearIcons();
            //}
        }

        public void Awake()
        {
            instance = this;

#if DEBUG
            LogMessage(" Awake event.");
#endif

            trackerCanvas = MotionTrackerMain.trackerObject.transform.FindChild("Canvas").GetComponent<Canvas>();

            radarUI = trackerCanvas.transform.FindChild("RadarUI").GetComponent<RectTransform>();
            radarUI.localScale = new Vector3(Settings.options.scale, Settings.options.scale, Settings.options.scale);

            iconContainer = radarUI.transform.FindChild("IconContainer").GetComponent<RectTransform>();

            backgroundImage = radarUI.transform.FindChild("Background").GetComponent<Image>();
            backgroundImage.color = new Color(1f, 1f, 1f, Settings.options.opacity);
            
            SetOpacity(Settings.options.opacity);
            Scale(Settings.options.scale);

            trackerCanvas.enabled = true;
            isVisible = true;
        }

        public void Scale(float scale)
        {
            if (radarUI)
            {
                radarUI.localScale = new Vector3(scale, scale, scale);
            }
        }

        public void SetOpacity(float opacity)
        {
            if (backgroundImage)
            {
                backgroundImage.color = new Color(1f, 1f, 1f, opacity);
            }
        }
    }
}