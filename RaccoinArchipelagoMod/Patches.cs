using HarmonyLib;
using UnityEngine;
using System.IO;
using BepInEx;
using System;

namespace RaccoinArchipelagoMod
{
    // 1. PRIZEBALL REPLACER
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
                excelID = 4005; 
                RaccoinPlugin.ModLogger.LogInfo($"[AP] Intercepted chute! Changed normal ball {oldID} to AP Check (ID 4005)!");
            }
        }
    }

    // 2. PRIZEBALL ITEM SYSTEM
    [HarmonyPatch(typeof(GameplayData), nameof(GameplayData.AddPrize))]
    public class CatchPrizePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(int excelID)
        {
            if (excelID == 4005)
            {
                RaccoinPlugin.ModLogger.LogMessage("=========================================");
                RaccoinPlugin.ModLogger.LogMessage($"[AP] CAUGHT AP BALL FALLING OFF THE EDGE!");
                RaccoinPlugin.ModLogger.LogMessage("=========================================");
                
                ArchipelagoManager.SendNextCheck();
                return false; 
            }
            return true; 
        }
    }

    // 2.5 INVENTORY CAP CIRCUMVENTION
    // Prevents AP Prizeballs from being deleted if the players prize inventory is full when the ball falls.
    [HarmonyPatch(typeof(DataHelper_PrizeBall), nameof(DataHelper_PrizeBall.RemovePrizeBall))]
    public class CatchFullInventoryPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(PrizeBallData prizeBallData) 
        {
            if (prizeBallData.excelID == 4005)
            {
                RaccoinPlugin.ModLogger.LogMessage("=========================================");
                RaccoinPlugin.ModLogger.LogMessage($"[AP] CAUGHT AP BALL (INVENTORY FULL DELETION)!");
                RaccoinPlugin.ModLogger.LogMessage("=========================================");
                
                ArchipelagoManager.SendNextCheck();
                return false; 
            }
            return true; 
        }
    }

    // 3. DATA THIEF
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

    // 4. POSTMASTER
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

                // Points
                if (incomingItemId == 80002) 
                {
                    StealDataPatch.CurrentGameData.curPt += ArchipelagoManager.AP_PointsValue;
                    RaccoinPlugin.ModLogger.LogMessage($"[AP REWARD] Gave the player {ArchipelagoManager.AP_PointsValue} Points!");
                }
                // Events & Traps (Expanded to cover IDs up to 80017)
                else if (incomingItemId >= 80003 && incomingItemId <= 80017)
                {
                    EventQueueManager eventManager = UnityEngine.Object.FindObjectOfType<EventQueueManager>();
                    
                    if (eventManager != null)
                    {
                        // --- COIN TOWERS ---
                        if (incomingItemId == 80003) // Small
                        {
                            QueueEvent slip = new QueueEvent(QueueEventDefine.CoinTower, ArchipelagoManager.AP_SmallTowerCoins, 0, 0, "", false);
                            eventManager.AddQueueEvent(slip);
                            RaccoinPlugin.ModLogger.LogMessage($"[AP REWARD] Triggered SMALL Coin Tower ({ArchipelagoManager.AP_SmallTowerCoins})!");
                        }
                        else if (incomingItemId == 80012) // Medium
                        {
                            QueueEvent slip = new QueueEvent(QueueEventDefine.CoinTower, ArchipelagoManager.AP_MediumTowerCoins, 0, 0, "", false);
                            eventManager.AddQueueEvent(slip);
                            RaccoinPlugin.ModLogger.LogMessage($"[AP REWARD] Triggered MEDIUM Coin Tower ({ArchipelagoManager.AP_MediumTowerCoins})!");
                        }
                        else if (incomingItemId == 80013) // Large
                        {
                            QueueEvent slip = new QueueEvent(QueueEventDefine.CoinTower, ArchipelagoManager.AP_LargeTowerCoins, 0, 0, "", false);
                            eventManager.AddQueueEvent(slip);
                            RaccoinPlugin.ModLogger.LogMessage($"[AP REWARD] Triggered LARGE Coin Tower ({ArchipelagoManager.AP_LargeTowerCoins})!");
                        }

                        // --- WHEEL SPINS ---
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

                        // --- GIFT RAINS ---
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

                        // --- EVERYTHING ELSE ---
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
                else
                {
                    StealDataPatch.CurrentGameData.AddPrizeBall(1001); 
                    RaccoinPlugin.ModLogger.LogMessage($"[AP REWARD] Dropped a Prize Ball for unknown item {incomingItemId}!");
                }
            }
        }
    }

    // 5. PRIZEBALL PAINTJOB & CUSTOM ICON
    [HarmonyPatch(typeof(PrizeBallView), nameof(PrizeBallView.InitColor))]
    public class PrizeBallColorPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(PrizeBallView __instance)
        {
            // Is it an AP ball?
            if (__instance.excelID == 4005)
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

    // 6. ACHIEVEMENT SILENCER
    // Blocks the game from unlocking Steam achievements while the randomizer is active
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

    // TEMPORARY DEV TOOOL: EVENT WIRETAP
    // Posts events to the console and displays the values that were passed to them.
    [HarmonyPatch(typeof(EventQueueManager), nameof(EventQueueManager.AddQueueEvent))]
    public class ExpandedEventWiretapPatch
    {
        [HarmonyPrefix]
        public static void Prefix(QueueEvent queueEvent)
        {
            // Filter out the system events to keep the console clean
            if (queueEvent.eventType != QueueEventDefine.Save && 
                queueEvent.eventType != QueueEventDefine.WaitRoundEnd &&
                queueEvent.eventType != QueueEventDefine.RoundEnd &&
                queueEvent.eventType != QueueEventDefine.None &&
                queueEvent.eventType != QueueEventDefine.EnumEnd)
            {
                RaccoinPlugin.ModLogger.LogWarning($"=====================================");
                RaccoinPlugin.ModLogger.LogWarning($"[SPY] CAUGHT EVENT: {queueEvent.eventType}");
                RaccoinPlugin.ModLogger.LogWarning($"[SPY] infoID_0: {queueEvent.infoID_0}");
                RaccoinPlugin.ModLogger.LogWarning($"[SPY] infoID_1: {queueEvent.infoID_1}");
                RaccoinPlugin.ModLogger.LogWarning($"[SPY] infoID_2: {queueEvent.infoID_2}");
                RaccoinPlugin.ModLogger.LogWarning($"[SPY] info string: {queueEvent.info}");
                RaccoinPlugin.ModLogger.LogWarning($"[SPY] checkBool: {queueEvent.checkBool}");
                RaccoinPlugin.ModLogger.LogWarning($"=====================================");
            }
        }
    }

   // TEMPORARY DEV TOOL: EVENT TESTER
    // F8: Fire Event & Increment ID | F9: Force Clear Stuck Event
    [HarmonyPatch(typeof(CoinMachineController), nameof(CoinMachineController.Update))]
    public class EventMapperDevToolPatch
    {
        // Change these two variables to test different events and starting IDs.
        public static QueueEventDefine CurrentTestEvent = QueueEventDefine.CoinTower;
        public static int CurrentTestID = 100; 

        [HarmonyPostfix]
        public static void Postfix()
        {
            // F8: FIRE THE EVENT
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F8))
            {
                EventQueueManager eventManager = UnityEngine.Object.FindObjectOfType<EventQueueManager>();
                if (eventManager != null)
                {
                    QueueEvent slip = new QueueEvent(CurrentTestEvent, CurrentTestID, 1, 1, "", false);
                    eventManager.AddQueueEvent(slip);
                    
                    RaccoinPlugin.ModLogger.LogMessage("=========================================");
                    RaccoinPlugin.ModLogger.LogMessage($"[DEV TOOL] Fired {CurrentTestEvent} with ID: {CurrentTestID}");
                    RaccoinPlugin.ModLogger.LogMessage("=========================================");
                    
                    CurrentTestID++;
                }
            }

            // F9: CLEAR EVENT QUEUE
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F9))
            {
                EventQueueManager eventManager = UnityEngine.Object.FindObjectOfType<EventQueueManager>();
                if (eventManager != null)
                {
                    RaccoinPlugin.ModLogger.LogWarning($"[DEV TOOL] Attempting to force-clear stuck {CurrentTestEvent}...");
                    
                    // Force the game to think the current event finished naturally
                    eventManager.EndQueueEvent_Deal(CurrentTestEvent);
                
                    eventManager.ClearInQueueEvent(CurrentTestEvent);
                    
                    RaccoinPlugin.ModLogger.LogWarning($"[DEV TOOL] Queue cleared! Safe to press F8 again.");
                }
            }
        }
    }

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

    // 7. THE PROGRESS BAR HIJACK
    [HarmonyPatch(typeof(MilestoneExcelData), "RefreshMilestone")]
    public class OverrideMilestoneProgressPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref RabbitLong score)
        {
            if (!ArchipelagoManager.IsConnected || StealDataPatch.CurrentGameData == null) return;

            try
            {
                // 1. Get the LIVE score from the current active round
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

                // 2. Add it to the saved total from all past rounds
                long grandTotal = roundScore + RaccoinPlugin.SavedCumulativeScore.Value;

                // 3. Convert the Grand Total back to Mantissa/Exponent for the UI
                float newMantissa = grandTotal;
                int newExponent = 0;

                while (newMantissa >= 10)
                {
                    newMantissa /= 10;
                    newExponent++;
                }

                // 4. Overwrite the incoming score variable so the UI uses our Grand Total.
                // (Because 'score' was passed by value into the original method, changing its fields here 
                // only affects the UI calculation, it safely ignores the player's actual save file.)
                score.mantissa = newMantissa;
                score.exponent = newExponent;
            }
            catch (Exception e)
            {
                RaccoinPlugin.ModLogger.LogWarning($"[AP] Progress Bar Hijack failed: {e.Message}");
            }
        }
    }

}