using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Il2CppInterop.Runtime.Injection;

namespace RaccoinArchipelagoMod
{
    [BepInPlugin("com.yourname.raccoin.archipelago", "Raccoin Archipelago", "1.0.0")]
    public class RaccoinPlugin : BasePlugin 
    {
        internal static ManualLogSource ModLogger;
        public static Sprite APLogoSprite;
        public static RaccoinPlugin Instance; 
        public static ConfigEntry<long> SavedCumulativeScore;

        public override void Load() 
        {
            Instance = this;
            ModLogger = Log;

            SavedCumulativeScore = Config.Bind("Archipelago", "CumulativeScore", 0L, "The total score accumulated across all AP rounds.");
            
            // 1. Load the AP Logo
            string logoPath = Path.Combine(Paths.GameRootPath, "ArchipelagoAssets", "AP_Logo.png");
            APLogoSprite = AssetLoader.LoadSprite(logoPath);

            if (APLogoSprite != null) ModLogger.LogMessage("[AP] Successfully loaded AP_Logo.png into memory!");
            else ModLogger.LogWarning("[AP] Could not find AP_Logo.png!");

            // 2. Setup UI
            ClassInjector.RegisterTypeInIl2Cpp<ArchipelagoUI>();
            GameObject apUiObject = new GameObject("Archipelago UI Object");
            apUiObject.AddComponent<ArchipelagoUI>();
            UnityEngine.Object.DontDestroyOnLoad(apUiObject);
            
            // 3. Apply Patches
            Harmony.CreateAndPatchAll(typeof(RaccoinPlugin).Assembly);
        }

        // DATA INJECTION METHOD
        public void PatchMilestoneRequirements()
        {
            try
            {
                var dataManager = ExcelDataManager.Instance; 
                
                if (dataManager == null || dataManager.milestoneExcelData == null)
                {
                    ModLogger.LogWarning("[AP] ExcelDataManager instance is not ready yet.");
                    return;
                }

                var milestoneItems = dataManager.milestoneExcelData.items;

                // 1. REPROGRAM THE GAME'S MATH
                for (int i = 0; i < ArchipelagoManager.ScoreMilestones.Length; i++)
                {
                    if (i < milestoneItems.Count)
                    {
                        long newScore = ArchipelagoManager.ScoreMilestones[i];
                        float mantissa = newScore;
                        int exponent = 0;

                        while (mantissa >= 10)
                        {
                            mantissa /= 10;
                            exponent++;
                        }

                        MilestoneExcelItem item = milestoneItems[i];
                        item.mantissa = mantissa;
                        item.exponent = exponent;
                        
                        ModLogger.LogMessage($"[AP] Milestone {i+1} Reprogrammed to {newScore} pts.");
                    }
                }
                
                // 2. UI SCREEN WIPE
                // FindObjectsOfTypeAll forces Unity to find the UI elements even if the menu is currently closed/hidden
                var activeMilestoneBoxes = UnityEngine.Resources.FindObjectsOfTypeAll<MilestoneStageView>();
                
                // Log exactly how many boxes it found so we can verify the GC didn't eat them
                ModLogger.LogMessage($"[AP UI] Screen Sweep found {activeMilestoneBoxes.Count} Milestone Boxes.");
                
                foreach (var box in activeMilestoneBoxes)
                {
                    if (box._rewardIcon != null && APLogoSprite != null)
                    {
                        box._rewardIcon.sprite = APLogoSprite;
                        box._rewardIcon.color = Color.white;
                    }
                    if (box._rewardSubIcon != null) 
                    {
                        box._rewardSubIcon.text = "";
                    }
                }
                
                ModLogger.LogMessage("[AP] Swept the UI (including hidden menus) and forced AP Logos onto all Milestones!");
            }
            catch (Exception e)
            {
                ModLogger.LogError($"[AP] Milestone Injection Failed: {e.Message}");
            }
        }

        // SHOP SLOT SPRITE REPLACEMENT (unused currently)
        [HarmonyPatch(typeof(ShopSlot), "InitView")] 
        [HarmonyPostfix] 
        public static void ForceArchipelagoVisuals(ShopSlot __instance)
        {
            var textComponents = __instance.gameObject.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in textComponents) 
            { 
                if (!text.gameObject.name.ToLower().Contains("price")) text.text = "AP ITEM"; 
            }

            var imageComponents = __instance.gameObject.GetComponentsInChildren<Image>();
            foreach (var img in imageComponents)
            {
                if (img.gameObject.name == "Icon" && APLogoSprite != null)
                {
                    img.sprite = APLogoSprite;
                    img.color = Color.white; 
                }
            }
        }
    }

    // ASSET LOADER
    public static class AssetLoader
    {
        private static Sprite _cachedSprite;

        public static unsafe Sprite LoadSprite(string filePath)
        {
            // Return the cached sprite immediately if it already exists
            if (_cachedSprite != null) return _cachedSprite;
            
            if (!File.Exists(filePath)) 
            {
                RaccoinPlugin.ModLogger.LogWarning($"[AP] Could not find AP_Logo.png at: {filePath}");
                return null;
            }

            try
            {
                byte[] imageAsBytes = File.ReadAllBytes(filePath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                
                // Protect the raw texture memory
                tex.hideFlags |= HideFlags.HideAndDontSave;
                
                var intPtr = UnityEngine.Object.MarshalledUnityObject.MarshalNotNull(tex);

                fixed (byte* ptr = imageAsBytes)
                {
                    var managedSpanWrapper = new UnityEngine.Bindings.ManagedSpanWrapper(ptr, imageAsBytes.Length);
                    UnityEngine.ImageConversion.LoadImage_Injected(intPtr, ref managedSpanWrapper, false);
                }

                Rect rect = new Rect(0, 0, tex.width, tex.height);
                Vector2 pivot = new Vector2(0.5f, 0.5f);
                
                _cachedSprite = Sprite.Create(tex, rect, pivot);
                
                // Protect the final Sprite using bitwise OR
                _cachedSprite.hideFlags |= HideFlags.HideAndDontSave;
                
                RaccoinPlugin.ModLogger.LogMessage("[AP] Image successfully loaded and cached in memory.");
                return _cachedSprite;
            }
            catch (Exception e)
            {
                RaccoinPlugin.ModLogger.LogError($"[AP] Asset Loader Failed: {e.Message}");
                return null;
            }
        }
    }
}