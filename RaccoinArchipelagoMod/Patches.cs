using HarmonyLib;
using UnityEngine;
using System.IO;
using BepInEx;
using System;


namespace RaccoinArchipelagoMod
{
    // AP Ball Spawner
    [HarmonyPatch(typeof(GameplayData), nameof(GameplayData.AddPrizeBall))]
    public class SpawnPrizeBallPatch
    {
        static int spawnCounter = 0; 

        [HarmonyPrefix]
        public static void Prefix(ref int excelID)
        {
            spawnCounter++;
            
            // Every 5th prize ball requested by the game becomes an AP Ball
            if (spawnCounter >= 5)
            {
                spawnCounter = 0;
                int oldID = excelID;
                
                // Hijack the request and tell the chute to drop ID 4005 instead
                excelID = 80000; 
                RaccoinPlugin.ModLogger.LogInfo($"[AP] Intercepted chute! Changed normal ball {oldID} to AP Check (ID 4005)!");
            }
        }
    }

    // AP ITEMBALL CATCHER
    // Handles checks safely regardless of which vanilla method triggers it
    // (Rewritten to avoid double fire bug. Still needs work)
    public static class APCatcher
    {
        private static float lastCatchTime = 0f;

        public static void ProcessCatch()
        {
            // DEBOUNCE: If both AddPrize and RemovePrizeBall fire within 0.1 seconds of each other, 
            // ignore the duplicate trigger to prevent double-sending!
            if (Time.unscaledTime - lastCatchTime < 0.1f) return;
            
            lastCatchTime = Time.unscaledTime;

            int activeChar = ArchipelagoManager.ActiveCharacterID;
            
            // Ignore the tutorial character (1000)
            if (activeChar == 1000) return; 

            int currentCount = ArchipelagoManager.CharacterDropCounts[activeChar];

            // Check if they have reached the limit of 50 checks for this character
            if (currentCount < 50) 
            {
                currentCount++; 
                ArchipelagoManager.CharacterDropCounts[activeChar] = currentCount; 

                // The * 1000 Math Fix is applied here so the server registers the IDs correctly!
                long baseLocationId = 91000 + ((activeChar - 1001) * 1000);
                long locationToSend = baseLocationId + currentCount;

                ArchipelagoManager.Session.Locations.CompleteLocationChecks(locationToSend);
                
                RaccoinPlugin.ModLogger.LogMessage($"=========================================");
                RaccoinPlugin.ModLogger.LogMessage($"[AP] CAUGHT AP BALL!");
                RaccoinPlugin.ModLogger.LogMessage($"[AP] Sent Check {locationToSend} (Character {activeChar} - Check {currentCount}/50)!");
                RaccoinPlugin.ModLogger.LogMessage($"=========================================");
            }
            else
            {
                APNotificationManager.SendNotification("You have already found all 50 items for this character!");
                RaccoinPlugin.ModLogger.LogMessage($"[AP] Character {activeChar} is maxed out at 50 checks!");
            }
        }
    }

    // 1. Normal Catch (Inventory has space)
    [HarmonyPatch(typeof(GameplayData), nameof(GameplayData.AddPrize))]
    public class CatchPrizePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(int excelID)
        {
            if (excelID == 80000)
            {
                APCatcher.ProcessCatch();
                
                // Return false to prevent the vanilla game from trying to load ID 80000 into the UI
                return false; 
            }
            return true; 
        }
    }

    // 2. Full Inventory Catch & Cleanup
    [HarmonyPatch(typeof(DataHelper_PrizeBall), nameof(DataHelper_PrizeBall.RemovePrizeBall))]
    public class CatchFullInventoryPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(PrizeBallData prizeBallData) 
        {
            if (prizeBallData.excelID == 80000)
            {
                APCatcher.ProcessCatch();
                
                // CRITICAL FIX: ALWAYS return true here!
                // If this returns false, the physical 3D ball never gets destroyed by the engine and permanently clogs the board.
                return true; 
            }
            
            return true; 
        }
    }

    // HOOK INTO GAME DATA
    [HarmonyPatch(typeof(GameplayData), nameof(GameplayData.NewRound))]
    public class StealDataPatch
    {
        public static GameplayData CurrentGameData = null;

        [HarmonyPostfix]
        public static void Postfix(GameplayData __instance)
        {
            CurrentGameData = __instance;
        }
    }

    // POSTMASTER
    [HarmonyPatch(typeof(CoinMachineController), nameof(CoinMachineController.Update))]
    public class ProcessAPQueuePatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (StealDataPatch.CurrentGameData == null) return;

            while (ArchipelagoManager.ItemQueue.Count > 0)
            {
                long incomingItemId = ArchipelagoManager.ItemQueue.Dequeue();

                // POINTS
                if (incomingItemId == 80002) 
                {
                    StealDataPatch.CurrentGameData.curPt += ArchipelagoManager.AP_PointsValue;
                    RaccoinPlugin.ModLogger.LogMessage($"[AP REWARD] Gave the player {ArchipelagoManager.AP_PointsValue} Points!");
                }
                // COIN UNLOCKS (AP IDs 82001 to 82123)
                else if (incomingItemId >= 81000 && incomingItemId <= 86000)
                {
                    // Add it to our internal AP memory so the unlock patch sees it
                    ArchipelagoManager.UnlockedItems.Add(incomingItemId);
                    RaccoinPlugin.ModLogger.LogMessage($"[AP REWARD] Unlocked a new Drop Coin (AP ID: {incomingItemId})!");

                    // Force the active game board to immediately reshuffle its drop deck!
                    if (StealDataPatch.CurrentGameData != null)
                    {
                        StealDataPatch.CurrentGameData.RefreshDrawPool();
                        RaccoinPlugin.ModLogger.LogMessage("[AP] Successfully forced the game to refresh the active drop pool!");
                    }
                }
                // EVENTS & TRAPS
                else if (incomingItemId >= 80003 && incomingItemId <= 80017)
                {
                    EventQueueManager eventManager = UnityEngine.Object.FindObjectOfType<EventQueueManager>();
                    
                    if (eventManager != null)
                    {
                        if (incomingItemId == 80003) // Small Tower
                        {
                            QueueEvent slip = new QueueEvent(QueueEventDefine.CoinTower, ArchipelagoManager.AP_SmallTowerCoins, 0, 0, "", false);
                            eventManager.AddQueueEvent(slip);
                            RaccoinPlugin.ModLogger.LogMessage($"[AP REWARD] Triggered SMALL Coin Tower ({ArchipelagoManager.AP_SmallTowerCoins})!");
                        }
                        else if (incomingItemId == 80012) // Medium Tower
                        {
                            QueueEvent slip = new QueueEvent(QueueEventDefine.CoinTower, ArchipelagoManager.AP_MediumTowerCoins, 0, 0, "", false);
                            eventManager.AddQueueEvent(slip);
                            RaccoinPlugin.ModLogger.LogMessage($"[AP REWARD] Triggered MEDIUM Coin Tower ({ArchipelagoManager.AP_MediumTowerCoins})!");
                        }
                        else if (incomingItemId == 80013) // Large Tower
                        {
                            QueueEvent slip = new QueueEvent(QueueEventDefine.CoinTower, ArchipelagoManager.AP_LargeTowerCoins, 0, 0, "", false);
                            eventManager.AddQueueEvent(slip);
                            RaccoinPlugin.ModLogger.LogMessage($"[AP REWARD] Triggered LARGE Coin Tower ({ArchipelagoManager.AP_LargeTowerCoins})!");
                        }
                        else if (incomingItemId == 80004) // Wheel 3
                        {
                            QueueEvent slip = new QueueEvent(QueueEventDefine.LuckyWheel, ArchipelagoManager.AP_WheelSpinSmall, 0, 0, "", false);
                            eventManager.AddQueueEvent(slip);
                            RaccoinPlugin.ModLogger.LogMessage($"[AP REWARD] Triggered Wheel Spin x{ArchipelagoManager.AP_WheelSpinSmall}!");
                        }
                        else if (incomingItemId == 80014) // Wheel 4
                        {
                            QueueEvent slip = new QueueEvent(QueueEventDefine.LuckyWheel, ArchipelagoManager.AP_WheelSpinMedium, 0, 0, "", false);
                            eventManager.AddQueueEvent(slip);
                            RaccoinPlugin.ModLogger.LogMessage($"[AP REWARD] Triggered Wheel Spin x{ArchipelagoManager.AP_WheelSpinMedium}!");
                        }
                        else if (incomingItemId == 80015) // Wheel 5
                        {
                            QueueEvent slip = new QueueEvent(QueueEventDefine.LuckyWheel, ArchipelagoManager.AP_WheelSpinLarge, 0, 0, "", false);
                            eventManager.AddQueueEvent(slip);
                            RaccoinPlugin.ModLogger.LogMessage($"[AP REWARD] Triggered Wheel Spin x{ArchipelagoManager.AP_WheelSpinLarge}!");
                        }
                        else if (incomingItemId == 80010) // Small Rain
                        {
                            int coinType = UnityEngine.Random.Range(1001, 1004); 
                            QueueEvent slip = new QueueEvent(QueueEventDefine.GiftRain, ArchipelagoManager.AP_GiftRainSmall, coinType, 0, "", false);
                            eventManager.AddQueueEvent(slip);
                            RaccoinPlugin.ModLogger.LogMessage($"[AP REWARD] Triggered SMALL Gift Rain ({ArchipelagoManager.AP_GiftRainSmall} coins)!");
                        }
                        else if (incomingItemId == 80016) // Medium Rain
                        {
                            int coinType = UnityEngine.Random.Range(1001, 1004); 
                            QueueEvent slip = new QueueEvent(QueueEventDefine.GiftRain, ArchipelagoManager.AP_GiftRainMedium, coinType, 0, "", false);
                            eventManager.AddQueueEvent(slip);
                            RaccoinPlugin.ModLogger.LogMessage($"[AP REWARD] Triggered MEDIUM Gift Rain ({ArchipelagoManager.AP_GiftRainMedium} coins)!");
                        }
                        else if (incomingItemId == 80017) // Large Rain
                        {
                            int coinType = UnityEngine.Random.Range(1001, 1004); 
                            QueueEvent slip = new QueueEvent(QueueEventDefine.GiftRain, ArchipelagoManager.AP_GiftRainLarge, coinType, 0, "", false);
                            eventManager.AddQueueEvent(slip);
                            RaccoinPlugin.ModLogger.LogMessage($"[AP REWARD] Triggered LARGE Gift Rain ({ArchipelagoManager.AP_GiftRainLarge} coins)!");
                        }
                        else if (incomingItemId == 80005) // Doom
                        {
                            int randomDoomId = UnityEngine.Random.Range(1001, 1018); 
                            QueueEvent slip = new QueueEvent(QueueEventDefine.Doom, randomDoomId, 0, 0, "", false);
                            eventManager.AddQueueEvent(slip);
                            RaccoinPlugin.ModLogger.LogMessage($"[AP TRAP] Triggered DOOM with coin ID {randomDoomId}!");
                        }
                        else if (incomingItemId == 80006) // Earthquake
                        {
                            QueueEvent slip = new QueueEvent(QueueEventDefine.Shake, ArchipelagoManager.AP_EarthquakeShakes, 0, 0, "", false);
                            eventManager.AddQueueEvent(slip);
                            RaccoinPlugin.ModLogger.LogMessage($"[AP EVENT] Triggered an Earthquake ({ArchipelagoManager.AP_EarthquakeShakes} shakes)!");
                        }
                        else if (incomingItemId == 80007) // Restock
                        {
                            QueueEvent slip = new QueueEvent(QueueEventDefine.FillCoin, ArchipelagoManager.AP_RestockCoins, 3, 0, "0_0", false);
                            eventManager.AddQueueEvent(slip);
                            RaccoinPlugin.ModLogger.LogMessage($"[AP REWARD] Triggered a Coin Restock ({ArchipelagoManager.AP_RestockCoins} coins)!");
                        }
                        else if (incomingItemId == 80008) // Russian Roulette
                        {
                            int rouletteType = UnityEngine.Random.Range(0, 2); 
                            QueueEvent slip = new QueueEvent(QueueEventDefine.RussianRoulette, rouletteType, 0, 0, "", false);
                            eventManager.AddQueueEvent(slip);
                            RaccoinPlugin.ModLogger.LogMessage($"[AP EVENT] Triggered Russian Roulette (Type {rouletteType})!");
                        }
                        else if (incomingItemId == 80009) // Tube Launchers
                        {
                            QueueEvent slip = new QueueEvent(QueueEventDefine.GiftCoin, ArchipelagoManager.AP_TubeLauncherCoins, 0, 0, "", false); 
                            eventManager.AddQueueEvent(slip);
                            RaccoinPlugin.ModLogger.LogMessage($"[AP REWARD] Triggered Tube Launchers ({ArchipelagoManager.AP_TubeLauncherCoins} coins)!");
                        }
                        else if (incomingItemId == 80011) // UFO
                        {
                            int ufoType = UnityEngine.Random.Range(0, 2); 
                            QueueEvent slip = new QueueEvent(QueueEventDefine.UFO, 0, ufoType, 0, "", false);
                            eventManager.AddQueueEvent(slip);
                            RaccoinPlugin.ModLogger.LogMessage($"[AP EVENT] Triggered UFO (Type {ufoType})!");
                        }
                    }
                    else
                    {
                        RaccoinPlugin.ModLogger.LogWarning("[AP ERROR] Couldn't find the EventQueueManager on the board!");
                    }
                }
                // UNKNOWN ITEM FALLBACK
                else
                {
                    StealDataPatch.CurrentGameData.AddPrizeBall(1001); 
                    RaccoinPlugin.ModLogger.LogMessage($"[AP REWARD] Dropped a Prize Ball for unknown item {incomingItemId}!");
                }
            }
        }
    }

    // PRIZEBALL PAINTJOB & CUSTOM ICON
    [HarmonyPatch(typeof(PrizeBallView), nameof(PrizeBallView.InitColor))]
    public class PrizeBallColorPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(PrizeBallView __instance)
        {
            // Is it an AP ball?
            if (__instance.excelID == 80000)
            {
                Color apPurple = new Color(0.65f, 0.13f, 0.96f, 1f);
                Color transparentPurple = new Color(0.65f, 0.13f, 0.96f, 0.5f); 

                // apply custom icon to ap ball
                if (__instance.prizeIcon != null && RaccoinPlugin.APLogoSprite != null) 
                {
                    __instance.prizeIcon.color = Color.white; // Prevent purple tint
                    __instance.prizeIcon.sprite = RaccoinPlugin.APLogoSprite; 
                }

                // Paint the 3D capsule
                var renderers = __instance.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                    {
                        var mat = renderer.sharedMaterials[i];
                        if (mat != null)
                        {
                            string matName = mat.name.ToLower();
                            MaterialPropertyBlock block = new MaterialPropertyBlock();
                            renderer.GetPropertyBlock(block, i);

                            if (matName.Contains("transparent"))
                            {
                                block.SetColor("_BaseColor", transparentPurple);
                                block.SetColor("_Color", transparentPurple);
                            }
                            else 
                            {
                                block.SetColor("_BaseColor", apPurple);
                                block.SetColor("_Color", apPurple);
                            }

                            renderer.SetPropertyBlock(block, i);
                        }
                    }
                }
                
                return false; 
            }
            else
            {
                // Is it a normal ball?
                if (__instance.prizeIcon != null) __instance.prizeIcon.color = Color.white; 

                var normalRenderers = __instance.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in normalRenderers)
                {
                    for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                    {
                        renderer.SetPropertyBlock(null, i); 
                    }
                }
                
                return true; 
            }
        }
    }

    // ACHIEVEMENT SILENCER
    // Blocks the game from unlocking Steam achievements while the randomizer is active (needs work)
    [HarmonyPatch(typeof(SteamInterface), nameof(SteamInterface.SetAchievement))]
    public class BlockSteamAchievementsPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(string name)
        {
            RaccoinPlugin.ModLogger.LogMessage($"[AP] Blocked Steam achievement from unlocking: {name}");
            return false; // Return false to completely skip the game's native Steam code.
        }
    }

    [HarmonyPatch(typeof(SteamInterface), nameof(SteamInterface.SetStat))]
    public class BlockSteamStatsPatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            return false; 
        }
    }

    // // TEMPORARY DEV TOOOL: EVENT WIRETAP
    // // Posts events to the console and displays the values that were passed to them.
    // [HarmonyPatch(typeof(EventQueueManager), nameof(EventQueueManager.AddQueueEvent))]
    // public class ExpandedEventWiretapPatch
    // {
    //     [HarmonyPrefix]
    //     public static void Prefix(QueueEvent queueEvent)
    //     {
    //         // Filter out the system events to keep the console clean
    //         if (queueEvent.eventType != QueueEventDefine.Save && 
    //             queueEvent.eventType != QueueEventDefine.WaitRoundEnd &&
    //             queueEvent.eventType != QueueEventDefine.RoundEnd &&
    //             queueEvent.eventType != QueueEventDefine.None &&
    //             queueEvent.eventType != QueueEventDefine.EnumEnd)
    //         {
    //             RaccoinPlugin.ModLogger.LogWarning($"=====================================");
    //             RaccoinPlugin.ModLogger.LogWarning($"[SPY] CAUGHT EVENT: {queueEvent.eventType}");
    //             RaccoinPlugin.ModLogger.LogWarning($"[SPY] infoID_0: {queueEvent.infoID_0}");
    //             RaccoinPlugin.ModLogger.LogWarning($"[SPY] infoID_1: {queueEvent.infoID_1}");
    //             RaccoinPlugin.ModLogger.LogWarning($"[SPY] infoID_2: {queueEvent.infoID_2}");
    //             RaccoinPlugin.ModLogger.LogWarning($"[SPY] info string: {queueEvent.info}");
    //             RaccoinPlugin.ModLogger.LogWarning($"[SPY] checkBool: {queueEvent.checkBool}");
    //             RaccoinPlugin.ModLogger.LogWarning($"=====================================");
    //         }
    //     }
    // }

    // // TEMPORARY DEV TOOL: EVENT TESTER
    // // F8: Fire Event & Increment ID | F9: Force Clear Stuck Event
    // [HarmonyPatch(typeof(CoinMachineController), nameof(CoinMachineController.Update))]
    // public class EventMapperDevToolPatch
    // {
    //     // Change these two variables to test different events and starting IDs.
    //     public static QueueEventDefine CurrentTestEvent = QueueEventDefine.CoinTower;
    //     public static int CurrentTestID = 100; 

    //     [HarmonyPostfix]
    //     public static void Postfix()
    //     {
    //         // F8: FIRE THE EVENT
    //         if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F8))
    //         {
    //             EventQueueManager eventManager = UnityEngine.Object.FindObjectOfType<EventQueueManager>();
    //             if (eventManager != null)
    //             {
    //                 QueueEvent slip = new QueueEvent(CurrentTestEvent, CurrentTestID, 1, 1, "", false);
    //                 eventManager.AddQueueEvent(slip);
                    
    //                 RaccoinPlugin.ModLogger.LogMessage("=========================================");
    //                 RaccoinPlugin.ModLogger.LogMessage($"[DEV TOOL] Fired {CurrentTestEvent} with ID: {CurrentTestID}");
    //                 RaccoinPlugin.ModLogger.LogMessage("=========================================");
                    
    //                 CurrentTestID++;
    //             }
    //         }

    //         // F9: CLEAR EVENT QUEUE
    //         if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F9))
    //         {
    //             EventQueueManager eventManager = UnityEngine.Object.FindObjectOfType<EventQueueManager>();
    //             if (eventManager != null)
    //             {
    //                 RaccoinPlugin.ModLogger.LogWarning($"[DEV TOOL] Attempting to force-clear stuck {CurrentTestEvent}...");
                    
    //                 // Force the game to think the current event finished naturally
    //                 eventManager.EndQueueEvent_Deal(CurrentTestEvent);
                
    //                 eventManager.ClearInQueueEvent(CurrentTestEvent);
                    
    //                 RaccoinPlugin.ModLogger.LogWarning($"[DEV TOOL] Queue cleared! Safe to press F8 again.");
    //             }
    //         }
    //     }
    // }

    // MILESTONE TRACKER VISUAL HIJACK
    [HarmonyPatch(typeof(MilestoneStageView), "SetRewardView")]
    public class MilestoneVisualPatch
    {
        [HarmonyPostfix]
        public static void Postfix(MilestoneStageView __instance)
        {
            try
            {
                if (RaccoinPlugin.APLogoSprite == null)
                {
                    // If this spams, the image failed to load or got destroyed.
                    RaccoinPlugin.ModLogger.LogWarning("[AP UI] Cannot update milestone, APLogoSprite is NULL!");
                    return;
                }

                if (__instance._rewardIcon != null)
                {
                    __instance._rewardIcon.sprite = RaccoinPlugin.APLogoSprite;
                    __instance._rewardIcon.color = Color.white;
                }

                if (__instance._rewardSubIcon != null)
                {
                    __instance._rewardSubIcon.text = ""; 
                }
            }
            catch (Exception e) 
            { 
                RaccoinPlugin.ModLogger.LogError($"[AP UI] Crash during visual update: {e.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(MilestoneExcelItem), "GetDesc")]
    public class MilestoneTooltipPatch
    {
        [HarmonyPostfix]
        public static void Postfix(MilestoneExcelItem __instance, ref string __result)
        {
            if (!ArchipelagoManager.IsConnected) return;
            try
            {
                // Reconstruct the raw integer directly from the game's scientific values.
                long scoreReq = (long)Math.Round(__instance.mantissa * Math.Pow(10, __instance.exponent));
                
                int index = Array.IndexOf(ArchipelagoManager.ScoreMilestones, scoreReq);
                if (index >= 0 && index < ArchipelagoManager.MilestoneLocationIDs.Length)
                {
                    long locId = ArchipelagoManager.MilestoneLocationIDs[index];
                    if (ArchipelagoManager.ScoutedMilestones.TryGetValue(locId, out var itemInfo))
                    {
                        string itemName = ArchipelagoManager.Session.Items.GetItemName(itemInfo.Item);
                        string playerName = ArchipelagoManager.Session.Players.GetPlayerAlias(itemInfo.Player);
                        
                        // Formats the tooltip text back to clean numbers (e.g., 10,000,000)
                        __result = $"Item: <color=#FFD700>{itemName}</color>\nFor: {playerName}\n\nUnlocks at {scoreReq:N0} pts";
                        return;
                    }
                }
                __result = "Archipelago Item\nConnect to see details.";
            }
            catch { }
        }
    }

    // MILESTONE PROGRESS
    [HarmonyPatch(typeof(MilestoneExcelData), "RefreshMilestone")]
    public class OverrideMilestoneProgressPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref RabbitLong score)
        {
            if (!ArchipelagoManager.IsConnected || StealDataPatch.CurrentGameData == null) return;

            try
            {
                // Get the LIVE score from the current active round
                RabbitLong curPt = StealDataPatch.CurrentGameData.curPt;
                long roundScore = 0;

                string scoreText = curPt.ToString().Replace(",", "").Trim();
                if (double.TryParse(scoreText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double currentScoreDouble))
                {
                    roundScore = (long)currentScoreDouble;
                }
                else
                {
                    roundScore = (long)Math.Round(curPt.mantissa * Math.Pow(10, curPt.exponent));
                }

                // Add it to the saved total from all past rounds
                long grandTotal = roundScore + RaccoinPlugin.SavedCumulativeScore.Value;

                // Convert the Grand Total back to Mantissa/Exponent for the UI
                float newMantissa = grandTotal;
                int newExponent = 0;

                while (newMantissa >= 10)
                {
                    newMantissa /= 10;
                    newExponent++;
                }

                // Overwrite the incoming score variable so the UI uses our Grand Total.
                score.mantissa = newMantissa;
                score.exponent = newExponent;
            }
            catch (Exception e)
            {
                RaccoinPlugin.ModLogger.LogWarning($"[AP] Progress Bar Hijack failed: {e.Message}");
            }
        }
    }

    // CHARACTER SELECT

    // Save File Bypass
    [HarmonyPatch(typeof(CharacterExcelItem), nameof(CharacterExcelItem.isRealUnlock), MethodType.Getter)]
    public class CharacterIsRealUnlockPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(CharacterExcelItem __instance, ref bool __result)
        {
            if (!ArchipelagoManager.IsConnected) return true;

            try
            {
                if (__instance.id == 1000) 
                {
                    __result = true; // Tutorial is always unlocked
                    return false; // Skip the original calculation
                }

                if (__instance.id >= 1001 && __instance.id <= 1006)
                {
                    long apItemId = 80900 + (__instance.id - 1001);
                    __result = ArchipelagoManager.UnlockedCharacters.Contains(apItemId);
                    return false; // Skip the original calculation
                }
            }
            catch { }

            return true;
        }
    }

    // Override the character "Select" button
    [HarmonyPatch(typeof(CharacterSelectUIController), nameof(CharacterSelectUIController.UpdateButtonVisual))]
    public class CharacterUpdateButtonVisualPatch
    {
        [HarmonyPostfix]
        public static void Postfix(CharacterSelectUIController __instance)
        {
            if (!ArchipelagoManager.IsConnected) return;

            try
            {
                int charId = __instance.curCharacterID;
                bool isUnlocked = (charId == 1000) ? true : ArchipelagoManager.UnlockedCharacters.Contains(80900 + (charId - 1001));

                if (__instance._selectBtn != null)
                {
                    // Safely cast the custom MyButton to its base Unity Selectable to force it on
                    var selectable = __instance._selectBtn.TryCast<UnityEngine.UI.Selectable>();
                    if (selectable != null)
                    {
                        selectable.interactable = isUnlocked;
                    }
                }
            }
            catch { }
        }
    }

    // Handle the Padlocks, Shadows, and Gray Portraits
    [HarmonyPatch(typeof(CharacterSelectUIController), nameof(CharacterSelectUIController.UpdateCharacterInfo))]
    public class CharacterVisualLockPatch
    {
        [HarmonyPostfix]
        public static void Postfix(CharacterSelectUIController __instance, int characterID)
        {
            if (!ArchipelagoManager.IsConnected) return;

            try
            {
                bool isUnlocked = (characterID == 1000) ? true : ArchipelagoManager.UnlockedCharacters.Contains(80900 + (characterID - 1001));

                if (__instance._characterIconLock != null)
                    __instance._characterIconLock.gameObject.SetActive(!isUnlocked);
                
                if (__instance._characterIconShadow != null)
                    __instance._characterIconShadow.gameObject.SetActive(!isUnlocked);

                if (__instance._characterIcon != null)
                    __instance._characterIcon.color = isUnlocked ? Color.white : new Color(0.3f, 0.3f, 0.3f, 1f);
            }
            catch { }
        }
    }

    // Block clicks on locked characters AND save the active character
    [HarmonyPatch(typeof(CharacterSelectUIController), nameof(CharacterSelectUIController.OnSelectButtonClicked))]
    public class CharacterSelectClickPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(CharacterSelectUIController __instance)
        {
            if (!ArchipelagoManager.IsConnected) return true;

            try
            {
                int charId = __instance.curCharacterID;
                
                // If it's NOT the tutorial, verify we own it
                if (charId != 1000)
                {
                    long apItemId = 80900 + (charId - 1001);
                    if (!ArchipelagoManager.UnlockedCharacters.Contains(apItemId))
                    {
                        return false; // Block the click
                    }
                }

                // If we reach this line, the click is valid. Save the character ID before starting the game.
                ArchipelagoManager.ActiveCharacterID = charId;
                RaccoinPlugin.ModLogger.LogMessage($"[AP] Starting run as Character ID: {charId}");
            }
            catch { }

            return true; // Let the click go through
        }
    }
    
    // Saftey Net: Do the exact same thing if they click "Continue" instead of "Select"
    [HarmonyPatch(typeof(CharacterSelectUIController), nameof(CharacterSelectUIController.OnContinueButtonClicked))]
    public class CharacterContinueClickPatch
    {
        [HarmonyPrefix]
        public static void Prefix(CharacterSelectUIController __instance)
        {
            if (ArchipelagoManager.IsConnected)
            {
                ArchipelagoManager.ActiveCharacterID = __instance.curCharacterID;
                RaccoinPlugin.ModLogger.LogMessage($"[AP] Continuing run as Character ID: {__instance.curCharacterID}");
            }
        }
    }

    // Force an inventory sync
    [HarmonyPatch(typeof(CharacterSelectUIController), nameof(CharacterSelectUIController.ShowPanel))]
    public class CharacterSelectSyncPatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            if (!ArchipelagoManager.IsConnected) return;

            try
            {
                // Clear the old list
                ArchipelagoManager.UnlockedCharacters.Clear();

                // Scan the Archipelago inventory for character IDs
                foreach (var itemInfo in ArchipelagoManager.Session.Items.AllItemsReceived)
                {
                    long itemId = itemInfo.Item;
                    if (itemId >= 80900 && itemId <= 80905)
                    {
                        ArchipelagoManager.UnlockedCharacters.Add(itemId);
                    }
                }
                
                RaccoinPlugin.ModLogger.LogMessage("[AP] Instantly synced Character Unlocks from server!");
            }
            catch (Exception e)
            {
                RaccoinPlugin.ModLogger.LogError($"[AP] Sync failed: {e.Message}");
            }
        }
    }

    // Prizeball Replacer
    [HarmonyPatch(typeof(PrizeExcelData), nameof(PrizeExcelData.GetPrizeExcelItem))]
    public class FakePrizeDatabasePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(PrizeExcelData __instance, ref int id, ref PrizeExcelItem __result)
        {
            // If the game asks for an AP Ball (80000)...
            if (id == 80000)
            {
                // hand it the Coin Cage (4005) instead
                // Because we pass 'id' by reference, modifying it here forces the original method
                // to look up 4005 instead of crashing on 80000.
                id = 4005; 
                
                // Let the original method run with the new ID
                return true; 
            }
            
            return true;
        }
    }

    // // TEMPORARY DEV TOOL: TOOLTIP DATA VIEWER
    // // Used for grabbing coin IDs and names by hovering over them in the Collections screen
    // [HarmonyPatch(typeof(CollectionUIView), nameof(CollectionUIView.ShowInfo))]
    // public class AutoCodexTooltipDumper
    // {
    //     [HarmonyPostfix]
    //     public static void Postfix(CollectionUIView __instance)
    //     {
    //         try
    //         {
    //             // 1. Grab the ID from the instance (just like your reference used __instance)
    //             int gameId = __instance.id;
    //             if (gameId <= 0) return;

    //             // 2. Grab the name from the slot's text component
    //             string coinName = "Unknown";
    //             if (__instance.nameText != null)
    //             {
    //                 coinName = __instance.nameText.text.Trim();
    //             }

    //             // 3. Category Logic
    //             long apId = 0;
    //             if (gameId >= 1000 && gameId < 2000)      apId = 81000 + (gameId % 1000);
    //             else if (gameId >= 2000 && gameId < 3000) apId = 82000 + (gameId % 2000);
    //             else if (gameId >= 3000 && gameId < 4000) apId = 83000 + (gameId % 3000);
    //             else if (gameId >= 5000 && gameId < 6000) apId = 85000 + (gameId % 5000);

    //             // 4. Log it
    //             string dumpPath = System.IO.Path.Combine(BepInEx.Paths.PluginPath, "Python_Final_Mapping.txt");
    //             using (System.IO.StreamWriter writer = new System.IO.StreamWriter(dumpPath, true))
    //             {
    //                 writer.WriteLine($"{coinName} | {gameId} | {apId}");
    //             }

    //             RaccoinPlugin.ModLogger.LogWarning($"[AP SPY] Logged: {coinName} (ID: {gameId})");
    //         }
    //         catch (Exception e)
    //         {
    //             RaccoinPlugin.ModLogger.LogError($"[AP SPY] Hook failed: {e.Message}");
    //         }
    //     }
    // }

    // --- UNIVERSAL COIN LOGGER ---
    // Targets any coin slot (Codex or Character Select) that uses CollectionUIView

    // --- CHARACTER SCREEN COIN LOGGER (REFINED) ---
    // --- CHARACTER SCREEN COIN LOGGER (WITH NAMES) ---
    
    // LOCK THE COIN POOL
    [HarmonyPatch(typeof(GameplayData), nameof(GameplayData.RefreshDrawPool))]
    public class SpawnerCoinFilterPatch
    {
        [HarmonyPostfix]
        public static void Postfix(GameplayData __instance)
        {
            if (!ArchipelagoManager.IsConnected || __instance.list_drawPool_coin == null) return;
            
            // Take any the locked coins out of the active draw pool
            ArchipelagoManager.FilterCoinList(__instance.list_drawPool_coin);
        }
    }

    // LOCK THE SHOP COIN POOL
    [HarmonyPatch(typeof(CoinExcelData), nameof(CoinExcelData.GetCoinShopList))]
    public class ShopCoinFilterPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref Il2CppSystem.Collections.Generic.List<int> __result)
        {
            if (!ArchipelagoManager.IsConnected || __result == null) return;

            // Takes the locked coins out of the shop generation table
            ArchipelagoManager.FilterCoinList(__result);
        }
    }

    // PADLOCK THE CODEX UI
    [HarmonyPatch(typeof(CoinExcelItem), nameof(CoinExcelItem.isRealUnlock), MethodType.Getter)]
    public class CoinIsRealUnlockPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(CoinExcelItem __instance, ref bool __result)
        {
            if (!ArchipelagoManager.IsConnected) return true;

            try
            {
                int coinId = __instance.id;
                if (ArchipelagoManager.CoinIdMapping.TryGetValue(coinId, out long apItemId))
                {
                    // Draw a padlock over the coin if we don't own it in AP
                    __result = ArchipelagoManager.UnlockedItems.Contains(apItemId);
                    return false; 
                }
            }
            catch { }

            return true;
        }
    }

}