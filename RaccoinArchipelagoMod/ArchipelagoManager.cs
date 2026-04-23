using System;
using System.Collections.Generic;
using System.IO;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Helpers;
using UnityEngine;

namespace RaccoinArchipelagoMod
{
    public static class ArchipelagoManager
    {
        public static ArchipelagoSession Session;
        public static bool IsConnected => Session != null && Session.Socket.Connected;
        public static int LocationsChecked = 0;
        public static int ProcessedItemIndex = 0;
        public static int AP_PointsValue = 100;
        public static int AP_SmallTowerCoins = 100;
        public static int AP_MediumTowerCoins = 250;
        public static int AP_LargeTowerCoins = 500;
        public static int AP_WheelSpinSmall = 3;
        public static int AP_WheelSpinMedium = 4;
        public static int AP_WheelSpinLarge = 5;
        public static int AP_GiftRainSmall = 30;
        public static int AP_GiftRainMedium = 40;
        public static int AP_GiftRainLarge = 50;
        public static int AP_EarthquakeShakes = 7;
        public static int AP_RestockCoins = 40;
        public static int AP_TubeLauncherCoins = 20;

        // Inventory Tracking
        public static HashSet<long> UnlockedItems = new HashSet<long>();
        public static HashSet<long> UnlockedCharacters = new HashSet<long>();
        
        // Mapping: Vanilla Coin ID -> AP Item ID
        public static Dictionary<int, long> CoinIdMapping = new Dictionary<int, long>();

        // Tracks the active character (1001-1006) for logic filtering
        public static int ActiveCharacterID = 1001; 

        // Tracks how many AP locations each specific character has checked (Max 50)
        public static Dictionary<int, int> CharacterDropCounts = new Dictionary<int, int>()
        {
            { 1001, 0 }, // Manager
            { 1002, 0 }, // Biologist
            { 1003, 0 }, // Chemist
            { 1004, 0 }, // Trader
            { 1005, 0 }, // Astronomer
            { 1006, 0 }  // Big Eater
        };

        // Queue for processing incoming Archipelago items on the Unity main thread
        public static Queue<long> ItemQueue = new Queue<long>();

        // Score Milestones (Syncs with YAML Difficulty)
        public static long[] ScoreMilestones = new long[28];
        
        // Milestone Location IDs
        public static readonly long[] MilestoneLocationIDs = {
            90001, 90002, 90003, 90004, 90005, 90006, 90007, 90008, 90009, 90010,
            90011, 90012, 90013, 90014, 90015, 90016, 90017, 90018, 90019, 90020,
            90021, 90022, 90023, 90024, 90025, 90026, 90027, 90028
        }; 
        
        public static int MilestonesClaimed = 0;
        public static Dictionary<long, NetworkItem> ScoutedMilestones = new Dictionary<long, NetworkItem>();

        public static void InitializeCoinMapping()
        {
            CoinIdMapping.Clear();
            // Maps ranges 1000, 2000, 3000, and 5000 to the 80000 AP blocks
            for (int i = 1000; i <= 6000; i++)
            {
                long apId = 0;
                if (i >= 1000 && i < 2000)      apId = 81000 + (i % 1000);
                else if (i >= 2000 && i < 3000) apId = 82000 + (i % 2000);
                else if (i >= 3000 && i < 4000) apId = 83000 + (i % 3000);
                else if (i >= 5000 && i < 6000) apId = 85000 + (i % 5000);

                if (apId != 0) CoinIdMapping[i] = apId;
            }
        }

        public static bool Connect(string url, int port, string slotName, string password, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                Session = ArchipelagoSessionFactory.CreateSession(url, port);
                LoginResult result = Session.TryConnectAndLogin("raccoin", slotName, ItemsHandlingFlags.AllItems, new Version(0, 6, 7), null, null, password);

                if (!result.Successful)
                {
                    LoginFailure failure = (LoginFailure)result;
                    errorMessage = string.Join(" | ", failure.Errors); 
                    return false; 
                }

                var loginSuccess = (LoginSuccessful)result;

                // Sync all 28 milestones from Slot Data
                for (int i = 0; i < ScoreMilestones.Length; i++)
                {
                    string key = $"milestone_{i + 1}";
                    if (loginSuccess.SlotData.TryGetValue(key, out var val))
                        ScoreMilestones[i] = Convert.ToInt64(val);
                }

                // Seed Management
                string currentSeed = Session.RoomState.Seed;
                string uniqueSessionId = $"{currentSeed}_{slotName}";
                if (RaccoinPlugin.LastPlayedSeed.Value != uniqueSessionId)
                {
                    RaccoinPlugin.SavedCumulativeScore.Value = 0;
                    RaccoinPlugin.LastPlayedSeed.Value = uniqueSessionId;
                    RaccoinPlugin.Instance.Config.Save();
                }

                // Inventory Initialization
                InitializeCoinMapping();
                UnlockedCharacters.Clear();
                UnlockedItems.Clear();

                foreach (var itemInfo in Session.Items.AllItemsReceived)
                {
                    long itemId = itemInfo.Item;
                    // Match character unlock range from Items.py (80901-80905)
                    if (itemId >= 80900 && itemId <= 80905) UnlockedCharacters.Add(itemId);
                    else if (itemId >= 81000 && itemId <= 86000) UnlockedItems.Add(itemId);
                }

                Session.MessageLog.OnMessageReceived += OnMessageReceived;
                Session.Items.ItemReceived += OnItemReceived;

                // Sync Slot Data for Event Params
                if (loginSuccess.SlotData.TryGetValue("ap_points_value", out var pv)) AP_PointsValue = Convert.ToInt32(pv);
                if (loginSuccess.SlotData.TryGetValue("ap_restock_coins", out var rcv)) AP_RestockCoins = Convert.ToInt32(rcv);

                SyncOnConnect();
                ScoutMilestoneLocations();
                RaccoinPlugin.Instance.PatchMilestoneRequirements();
                
                return true; 
            }
            catch (Exception e)
            {
                RaccoinPlugin.ModLogger.LogError($"[AP] Connection Error: {e.Message}");
                errorMessage = "Network Exception";
                return false;
            }
        }

        private static void OnItemReceived(ReceivedItemsHelper helper)
        {
            while (ProcessedItemIndex < helper.AllItemsReceived.Count)
            {
                var item = helper.AllItemsReceived[ProcessedItemIndex];
                long itemId = item.Item;

                // 1. Add to HashSets instantly so UI menus unlock
                if (itemId >= 80900 && itemId <= 80905) UnlockedCharacters.Add(itemId);
                else UnlockedItems.Add(itemId);

                // 2. Add to the Queue so the Postmaster patch can physically grant the rewards (Points/Events)
                ItemQueue.Enqueue(itemId);

                ProcessedItemIndex++;
                
                // 3. Persistence
                string currentPlayer = Session.Players.GetPlayerName(Session.ConnectionInfo.Slot);
                string currentSeed = Session.RoomState.Seed;
                string saveFilePath = $"AP_Save_{currentPlayer}_{currentSeed}.txt";
                File.WriteAllText(saveFilePath, ProcessedItemIndex.ToString());
            }
        }

        public static void CheckScoreMilestones(long currentTotalScore)
        {
            if (Session == null || !IsConnected) return;

            List<long> checksToSend = new List<long>();
            while (MilestonesClaimed < ScoreMilestones.Length && currentTotalScore >= ScoreMilestones[MilestonesClaimed])
            {
                checksToSend.Add(MilestoneLocationIDs[MilestonesClaimed]);
                MilestonesClaimed++;
            }

            if (checksToSend.Count > 0)
            {
                Session.Locations.CompleteLocationChecks(checksToSend.ToArray());
                RaccoinPlugin.ModLogger.LogMessage($"[AP] Sent {checksToSend.Count} Milestones to server.");
            }
        }

        public static void FilterCoinList(Il2CppSystem.Collections.Generic.List<int> il2cppList)
        {
            if (il2cppList == null) return;
            var safeList = new List<int>();

            for (int i = 0; i < il2cppList.Count; i++)
            {
                int coinId = il2cppList[i];
                if (CoinIdMapping.TryGetValue(coinId, out long apItemId))
                {
                    if (UnlockedItems.Contains(apItemId)) safeList.Add(coinId);
                }
                else safeList.Add(coinId); // Keep basic metals (1001-1003)
            }

            il2cppList.Clear();
            foreach (int allowedCoin in safeList) il2cppList.Add(allowedCoin);
        }

        private static void OnMessageReceived(Archipelago.MultiClient.Net.MessageLog.Messages.LogMessage message)
        {
            APNotificationManager.SendNotification(message.ToString());
        }

        public static void ScoutMilestoneLocations()
        {
            if (Session == null || !IsConnected) return;
            Session.Locations.ScoutLocationsAsync(false, MilestoneLocationIDs).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    foreach (var networkItem in task.Result.Locations)
                        ScoutedMilestones[networkItem.Location] = networkItem;
                }
            });
        }

        public static void SyncOnConnect()
        {
            if (Session == null || !IsConnected) return;
            string saveFilePath = $"AP_Save_{Session.Players.GetPlayerName(Session.ConnectionInfo.Slot)}_{Session.RoomState.Seed}.txt";
            if (File.Exists(saveFilePath) && int.TryParse(File.ReadAllText(saveFilePath), out int savedIndex))
                ProcessedItemIndex = savedIndex;
        }
    }
}