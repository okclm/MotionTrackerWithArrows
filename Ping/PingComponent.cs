using MelonLoader;
using UnityEngine;
using Il2CppInterop;
using Il2CppInterop.Runtime.Injection; 
using System.Collections;
using Il2Cpp;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine.UI;

namespace MotionTracker
{
	public class PingComponent : MonoBehaviour
	{
        public PingComponent(IntPtr intPtr) : base(intPtr)
        {
        }
               
        public GameObject attachedGameObject;     
        public PingManager.AnimalType animalType;
        public ProjectileType spraypaintType;
        public PingCategory assignedCategory;
     
        public CanvasGroup canvasGroup;
        public GameObject iconObject;
        public bool isInitialized = false;
        public Image iconImage;
        float timer = 0f;           // Accumulate the time since last frame so we can do things after the trigger duration is elapsed (triggerTime).
        float triggerTime = 5f;     // Trigger duration.  When the acculated frame time exceeds this value, we do stuff and reset the timer to zero.
       

        public enum PingCategory
        {
          None, Animal, Spraypaint
        };

        public RectTransform rectTransform;
        public bool clampOnRadar = false;
        public static GameObject playerObject;




        [HideFromIl2Cpp]
        public void CreateIcon()
        {
            if(assignedCategory == PingCategory.Animal)
            {
                iconObject = Instantiate(MotionTrackerMain.GetAnimalPrefab(animalType));
                iconImage = iconObject.GetComponent<Image>();
                iconImage.color = Settings.animalColor;
            }
            else if (assignedCategory == PingCategory.Spraypaint)
            {
                iconObject = Instantiate(MotionTrackerMain.GetSpraypaintPrefab(spraypaintType));
                iconImage = iconObject.GetComponent<Image>();
                iconImage.color = Settings.spraypaintColor;
            }

            iconObject.transform.SetParent(PingManager.instance.iconContainer.transform, false);
            iconObject.active = true;
            canvasGroup = iconObject.GetComponent<CanvasGroup>();
            rectTransform = iconObject.GetComponent<RectTransform>();
        }

        [HideFromIl2Cpp]
        public void DeleteIcon()
        {
            if (iconObject)
            {
                GameObject.Destroy(iconObject);
            }
        }

        [HideFromIl2Cpp]
        public bool AllowedToShow()
        {
            if (assignedCategory == PingCategory.Animal)
            {
                if (animalType == PingManager.AnimalType.Crow && Settings.options.showCrows)
                {
                    return true;
                }
                else if (animalType == PingManager.AnimalType.Rabbit && Settings.options.showRabbits)
                {
                    return true;
                }
                else if (animalType == PingManager.AnimalType.Stag && Settings.options.showStags)
                {
                    return true;
                }
                else if (animalType == PingManager.AnimalType.Doe && Settings.options.showDoes)
                {
                    return true;
                }
                else if (animalType == PingManager.AnimalType.Wolf && Settings.options.showWolves)
                {
                    return true;
                }
                else if (animalType == PingManager.AnimalType.Timberwolf && Settings.options.showTimberwolves)
                {
                    return true;
                }
                else if (animalType == PingManager.AnimalType.Bear && Settings.options.showBears)
                {
                    return true;
                }
                else if (animalType == PingManager.AnimalType.Cougar && Settings.options.showCougars)
                {
                    return true;
                }
                else if (animalType == PingManager.AnimalType.Moose && Settings.options.showMoose)
                {
                    return true;
                }
                else if (animalType == PingManager.AnimalType.PuffyBird && Settings.options.showPuffyBirds)
                {
                    return true;
                }

                // CLM - Arrows!
                else if (animalType == PingManager.AnimalType.Arrow && Settings.options.showArrows)
                {
                    return true;
                }

                else
                {
                    return false;
                }
            }
            else if (assignedCategory == PingCategory.Spraypaint && Settings.options.showSpraypaint)
            {
                return true;                
            }         

            return false;
        }

        [HideFromIl2Cpp]
        public static void ManualDelete(PingComponent pingComponent)
        {
            if (pingComponent != null)
            {
                MelonLogger.Msg("[MotionTracker].PingComponents.ManualDelete.144: pingComponent.name = (" + pingComponent.name + ")");
                pingComponent.DeleteIcon();
                GameObject.Destroy(pingComponent);
            }
        }

        [HideFromIl2Cpp]
        public void SetVisible(bool visibility)
        {
            if (AllowedToShow() && visibility)
            {
                canvasGroup.alpha = 1f;
            }
            else
            {
                canvasGroup.alpha = 0f;
            }
        }

        [HideFromIl2Cpp]       
        public void Initialize(PingManager.AnimalType type)
        {
            attachedGameObject = this.gameObject;
            animalType = type;
            assignedCategory = PingCategory.Animal;
            
            CreateIcon();

            isInitialized = true;
        }

        [HideFromIl2Cpp]
        public void Initialize(ProjectileType type)
        {
            attachedGameObject = this.gameObject;
            spraypaintType = type;
            assignedCategory = PingCategory.Spraypaint;
            

            CreateIcon();

            isInitialized = true;
        }

        [HideFromIl2Cpp]
        private void OnDisable()
        {
            DeleteIcon();
        }

       
        public void Update()
        {
            if (Settings.options.enableMotionTracker && PingManager.isVisible)
            {
                if (SaveGameSystem.m_CurrentGameMode == SaveSlotType.SANDBOX)
                {
                    if (GameManager.GetVpFPSPlayer() != null)
                    {
                        timer += Time.deltaTime;    // Accumulated time since we last logged stuff

                        UpdateLocatableIcons();

                        // Check if we need to reset the accumulated time
                        if (timer > triggerTime)
                        {
                            MelonLogger.Msg("[MotionTracker].PingComponents.Update.210: GameManager.GetVpFPSPlayer().gameObject.transform.position = (" + GameManager.GetVpFPSPlayer().gameObject.transform.position + ")");
                            timer = 0f;
                        }
                    }
                }
            }
        }

        private void UpdateLocatableIcons()
        {
            if (TryGetIconLocation(out var iconLocation))
            {
                // MelonLogger.Msg("[MotionTracker].PingComponents.UpdateLocatableIcons.216: See something to be updated. (" + this.name + ")");

                SetVisible(true);
                rectTransform.anchoredPosition = iconLocation;

                // MelonLogger.Msg("[MotionTracker].PingComponents.UpdateLocatableIcons.221: anchoredPosition = " + rectTransform.anchoredPosition);

                if (assignedCategory == PingCategory.Spraypaint)
                {
                    // MelonLogger.Msg("[MotionTracker].PingComponents.UpdateLocatableIcons.225: Assigned category is Spraypaint = " + assignedCategory);

                    if (iconImage.color != Settings.spraypaintColor || rectTransform.localScale != Settings.spraypaintScale)
                    {
                        rectTransform.localScale = Settings.spraypaintScale;
                        iconImage.color = Settings.spraypaintColor;
                    }
                }
                else if (assignedCategory == PingCategory.Animal)
                {
                    if (iconImage.color != Settings.spraypaintColor || rectTransform.localScale != Settings.spraypaintScale)
                    {
                        rectTransform.localScale = Settings.animalScale;
                        iconImage.color = Settings.animalColor;
                    }

                    if (this.name.Contains("Arrow"))
                    {
                        iconImage.color = Color.magenta;

                        if (timer > triggerTime)
                        {
                            MelonLogger.Msg("[MotionTracker].PingComponents.UpdateLocatableIcons.253: Setting visible to TRUE.  Assigned category is Animal.Arrow (" + this.name + ") and anchoredPosition = " + rectTransform.anchoredPosition);
                        }
                        // MelonLogger.Msg("[MotionTracker].PingComponents.UpdateLocatableIcons.238: GameManager.GetVpFPSPlayer().gameObject.transform.position = (" + GameManager.GetVpFPSPlayer().gameObject.transform.position + ")");
                    }

                    if (this.name.Contains("Cougar"))
                    {
                        iconImage.color = Color.yellow;
                        if (timer > triggerTime)
                        {
                            MelonLogger.Msg("[MotionTracker].PingComponents.UpdateLocatableIcons.263: Setting visible to TRUE.  Assigned category is Animal.Cougar (" + this.name + ") and anchoredPosition = " + rectTransform.anchoredPosition);
                        }
                    }
                }
            }
            else
            {
                if (this.name.Contains("Arrow"))
                {
                    if (timer > triggerTime)
                    {
                        MelonLogger.Msg("[MotionTracker].PingComponents.UpdateLocatableIcons.274: Setting visible to FALSE.  Assigned category is Animal.Arrow (" + this.name + ") and anchoredPosition = " + rectTransform.anchoredPosition);
                    }
                    // MelonLogger.Msg("[MotionTracker].PingComponents.UpdateLocatableIcons.247: GameManager.GetVpFPSPlayer().gameObject.transform.position = (" + GameManager.GetVpFPSPlayer().gameObject.transform.position + ")");
                }

                // MelonLogger.Msg("[MotionTracker].PingComponents.UpdateLocatableIcons.240: Setting visible to FALSE.");
                SetVisible(false);
            }
        }

        private bool TryGetIconLocation(out Vector2 iconLocation)
        {
            iconLocation = GetDistanceToPlayer(this);

            if (this.name.Contains("Arrow"))
            {
                if (timer > triggerTime)
                {
                    MelonLogger.Msg("[MotionTracker].PingComponents.TryGetIconLocation.292: Assigned category is Animal.Arrow (" + this.name + ") and distance (iconLocation) = " + iconLocation);
                }
            }

            float radarSize = GetRadarUISize();

            var scale = radarSize / Settings.options.detectionRange;

            iconLocation *= scale;

            // Rotate the icon by the players y rotation if enabled
            if (PingManager.instance.applyRotation)
            {
                var playerForwardDirectionXZ = new Vector3(0, 0, 0);

                // Get the forward vector of the player projected on the xz plane
                if (GameManager.GetVpFPSPlayer())
                {
                    playerForwardDirectionXZ = Vector3.ProjectOnPlane(GameManager.GetVpFPSPlayer().gameObject.transform.forward, Vector3.up);
                }

                // Create a roation from the direction
                var rotation = Quaternion.LookRotation(playerForwardDirectionXZ);

                // Mirror y rotation
                var euler = rotation.eulerAngles;
                euler.y = -euler.y;
                rotation.eulerAngles = euler;

                // Rotate the icon location in 3D space
                var rotatedIconLocation = rotation * new Vector3(iconLocation.x, 0.0f, iconLocation.y);

                // Convert from 3D to 2D
                iconLocation = new Vector2(rotatedIconLocation.x, rotatedIconLocation.z);
            }

            if (iconLocation.sqrMagnitude < radarSize * radarSize || this.clampOnRadar)
            {
                // Make sure it is not shown outside the radar
                iconLocation = Vector2.ClampMagnitude(iconLocation, radarSize);

                return true;
            }

            if (this.name.Contains("Arrow"))
            {
                if (timer > triggerTime)
                {
                    MelonLogger.Msg("[MotionTracker].PingComponents.TryGetIconLocation.340: Assigned category is Animal.Arrow (" + this.name + ") and final distance (iconLocation) = " + iconLocation);
                }
            }

            return false;
        }


        private float GetRadarUISize()
        {
            return PingManager.instance.iconContainer.rect.width / 2;
        }

        private Vector2 GetDistanceToPlayer(PingComponent locatable)
        {
            if (GameManager.GetVpFPSPlayer() && locatable)
            {
                // Debug.Log("(CLM) PingComponent.cs:GetDistanceToPlayer: locatable=", locatable);

                Vector3 distanceToPlayer = locatable.transform.position - GameManager.GetVpFPSPlayer().gameObject.transform.position;

                if (locatable.name.Contains("Arrow"))
                {
                    if (timer > triggerTime)
                    {
                        MelonLogger.Msg("[MotionTracker].PingComponents.GetDistanceToPlayer.365: Arrow (" + this.name  + ") position is (" + this.transform.position + ") and distance is " + distanceToPlayer);
                    }
                }


                return new Vector2(distanceToPlayer.x, distanceToPlayer.z);
            }

            return new Vector2(0, 0);
        }
    }
}