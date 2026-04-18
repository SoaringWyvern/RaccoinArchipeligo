using System;
using System.Collections.Generic;
using System.IO;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Helpers;

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
        public static HashSet<long> UnlockedCharacters = new HashSet<long>();
        // The ID of the character the player just selected in the menu
        public static int ActiveCharacterID = 1001; // Defaults to Manager

        // Tracks how many AP locations each specific character has checked (Max 17)
        public static Dictionary<int, int> CharacterDropCounts = new Dictionary<int, int>()
        {
            { 1001, 0 }, // Manager
            { 1002, 0 }, // Biologist
            { 1003, 0 }, // Chemist
            { 1004, 0 }, // Trader
            { 1005, 0 }, // Astronomer
            { 1006, 0 }  // Big Eater
        };

        // Initialize the Milestones array to set aside the slots in memory and avoid checks being send to the sever prematurely
        public static long[] ScoreMilestones = new long[] {
            100000, 250000, 500000, 750000, 1000000, 1500000, 2000000, 2500000, 3000000, 4000000,
            5000000, 6000000, 7500000, 10000000, 15000000, 20000000, 25000000, 30000000, 40000000,
            50000000, 60000000, 75000000, 100000000, 150000000, 200000000, 250000000, 500000000, 1000000000
        };
        public static readonly long[] MilestoneLocationIDs = {
            4000, 4001, 4002, 4003, 4004, 4005, 4006, 4007, 4008, 4009,
            4010, 4011, 4012, 4013, 4014, 4015, 4016, 4017, 4018, 4019,
            4020, 4021, 4022, 4023, 4024, 4025, 4026, 4027
        }; 
        
        public static int MilestonesClaimed = 0;
        public static Dictionary<long, NetworkItem> ScoutedMilestones = new Dictionary<long, NetworkItem>();
        
        public static readonly long BaseLocationId = 81000;
        public static readonly int MaxLocations = 50; 

        public static bool Connect(string url, int port, string slotName, string password, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                Session = ArchipelagoSessionFactory.CreateSession(url, port);
                
                LoginResult result = Session.TryConnectAndLogin(
                    "RACCOIN", 
                    slotName,
                    ItemsHandlingFlags.AllItems, 
                    new Version(0, 6, 7), 
                    null, 
                    null, 
                    password
                );

                if (!result.Successful)
                {
                    LoginFailure failure = (LoginFailure)result;
                    errorMessage = string.Join(" | ", failure.Errors); 
                    return false; 
                }

                // Get milestone slot data from AP server
                var loginSuccess = (LoginSuccessful)result;
                if (loginSuccess.SlotData.ContainsKey("milestone_1"))
                {
                    for (int i = 0; i < 5; i++)
                    {
                        string key = $"milestone_{i + 1}";
                        if (loginSuccess.SlotData.ContainsKey(key))
                        {
                            ScoreMilestones[i] = Convert.ToInt64(loginSuccess.SlotData[key]);
                        }
                    }
                    RaccoinPlugin.ModLogger.LogMessage("[AP] Custom milestone values received from server.");
                }

                // Get the game seed
                string currentSeed = Session.RoomState.Seed;
                string currentSlotName = loginSuccess.SlotData.Count > 0 ? Session.Players.GetPlayerAlias(loginSuccess.Slot) : "UnknownPlayer";
                string uniqueSessionId = $"{currentSeed}_{currentSlotName}";

                // Check if the seed matches the one stored in the score file
                if (RaccoinPlugin.LastPlayedSeed.Value != uniqueSessionId)
                {
                    RaccoinPlugin.ModLogger.LogMessage("=========================================");
                    RaccoinPlugin.ModLogger.LogMessage("[AP] NEW SEED DETECTED! Wiping cumulative score to 0.");
                    RaccoinPlugin.ModLogger.LogMessage("=========================================");
                    
                    // Wipe the score
                    RaccoinPlugin.SavedCumulativeScore.Value = 0;
                    
                    // Save the new Seed ID so it doesn't wipe it again next round
                    RaccoinPlugin.LastPlayedSeed.Value = uniqueSessionId;
                    
                    // Save the file
                    RaccoinPlugin.Instance.Config.Save();
                }

                UnlockedCharacters.Clear();

                RaccoinPlugin.ModLogger.LogMessage("--- CHECKING STARTING INVENTORY ---");
                foreach (var itemInfo in Session.Items.AllItemsReceived)
                {
                    long itemId = itemInfo.Item;
                    RaccoinPlugin.ModLogger.LogMessage($"[AP SYNC] Server says we own Item ID: {itemId}");
                    
                    if (itemId >= 80020 && itemId <= 80025)
                    {
                        UnlockedCharacters.Add(itemId);
                        RaccoinPlugin.ModLogger.LogMessage($"---> STARTER CHARACTER CAUGHT! (ID: {itemId})");
                    }
                }
                RaccoinPlugin.ModLogger.LogMessage("-----------------------------------");

                Session.MessageLog.OnMessageReceived += OnMessageReceived;
                
                // Get dynamic event parameters from AP server
                if (loginSuccess.SlotData.TryGetValue("ap_points_value", out var pv)) AP_PointsValue = Convert.ToInt32(pv);
                
                if (loginSuccess.SlotData.TryGetValue("ap_small_tower_coins", out var stv)) AP_SmallTowerCoins = Convert.ToInt32(stv);
                if (loginSuccess.SlotData.TryGetValue("ap_medium_tower_coins", out var mtv)) AP_MediumTowerCoins = Convert.ToInt32(mtv);
                if (loginSuccess.SlotData.TryGetValue("ap_large_tower_coins", out var ltv)) AP_LargeTowerCoins = Convert.ToInt32(ltv);
                
                if (loginSuccess.SlotData.TryGetValue("ap_wheel_spin_small", out var wsv)) AP_WheelSpinSmall = Convert.ToInt32(wsv);
                if (loginSuccess.SlotData.TryGetValue("ap_wheel_spin_medium", out var wmv)) AP_WheelSpinMedium = Convert.ToInt32(wmv);
                if (loginSuccess.SlotData.TryGetValue("ap_wheel_spin_large", out var wlv)) AP_WheelSpinLarge = Convert.ToInt32(wlv);

                if (loginSuccess.SlotData.TryGetValue("ap_gift_rain_coins_small", out var rsv)) AP_GiftRainSmall = Convert.ToInt32(rsv);
                if (loginSuccess.SlotData.TryGetValue("ap_gift_rain_coins_medium", out var rmv)) AP_GiftRainMedium = Convert.ToInt32(rmv);
                if (loginSuccess.SlotData.TryGetValue("ap_gift_rain_coins_large", out var rlv)) AP_GiftRainLarge = Convert.ToInt32(rlv);

                if (loginSuccess.SlotData.TryGetValue("ap_quake_shakes", out var qv)) AP_EarthquakeShakes = Convert.ToInt32(qv);
                if (loginSuccess.SlotData.TryGetValue("ap_restock_coins", out var rcv)) AP_RestockCoins = Convert.ToInt32(rcv);
                if (loginSuccess.SlotData.TryGetValue("ap_tube_coins", out var tcv)) AP_TubeLauncherCoins = Convert.ToInt32(tcv);

                // Sync and Setup
                SyncOnConnect();
                Session.Items.ItemReceived += OnItemReceived;
                ScoutMilestoneLocations();
                
                // Apply Milestone Patch
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

        private static void OnMessageReceived(Archipelago.MultiClient.Net.MessageLog.Messages.LogMessage message)
        {

            string text = message.ToString();
            
            // Send it to the UI
            APNotificationManager.SendNotification(text);
            
            // Also print it to the BepInEx console just so we have a record of it
            RaccoinPlugin.ModLogger.LogMessage($"[AP LOG] {text}");
        }

        public static void ScoutMilestoneLocations()
        {
            if (Session == null || !Session.Socket.Connected) return;
            Session.Locations.ScoutLocationsAsync(false, MilestoneLocationIDs).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully && task.Result != null)
                {
                    foreach (var networkItem in task.Result.Locations)
                        ScoutedMilestones[networkItem.Location] = networkItem;
                }
            });
        }

        public static void SyncOnConnect()
        {
            if (Session == null || !Session.Socket.Connected) return;
            LocationsChecked = Session.Locations.AllLocationsChecked.Count;

            string currentPlayer = Session.Players.GetPlayerName(Session.ConnectionInfo.Slot);
            string currentSeed = Session.RoomState.Seed; 
            string saveFilePath = $"AP_Save_{currentPlayer}_{currentSeed}.txt";

            if (File.Exists(saveFilePath))
            {
                if (int.TryParse(File.ReadAllText(saveFilePath), out int savedIndex))
                    ProcessedItemIndex = savedIndex;
            }
            else ProcessedItemIndex = 0;
        }

        public static void CheckScoreMilestones(long currentTotalScore)
        {
            if (Session == null || !Session.Socket.Connected) return;

            // Create a temporary list to hold all the checks we unlock this frame
            List<long> checksToSend = new List<long>();

            while (MilestonesClaimed < ScoreMilestones.Length && currentTotalScore >= ScoreMilestones[MilestonesClaimed])
            {
                checksToSend.Add(MilestoneLocationIDs[MilestonesClaimed]);
                MilestonesClaimed++;
            }

            // If we gathered any checks, send them all in a single network packet
            if (checksToSend.Count > 0)
            {
                Session.Locations.CompleteLocationChecks(checksToSend.ToArray());
                RaccoinPlugin.ModLogger.LogMessage($"[AP] Batched and sent {checksToSend.Count} Milestone Checks to the server!");
            }
        }

        public static void SendNextCheck()
        {
            if (Session == null || !Session.Socket.Connected) return;
            if (LocationsChecked < MaxLocations)
            {
                long nextCheckId = BaseLocationId + LocationsChecked;
                Session.Locations.CompleteLocationChecks(nextCheckId);
                LocationsChecked++;
            }
        }

        public static Queue<long> ItemQueue = new Queue<long>();
        private static void OnItemReceived(ReceivedItemsHelper helper)
        {
            while (ProcessedItemIndex < helper.AllItemsReceived.Count)
            {
                var item = helper.AllItemsReceived[ProcessedItemIndex];
                ItemQueue.Enqueue(item.Item);
                ProcessedItemIndex++;
                
                string currentPlayer = Session.Players.GetPlayerName(Session.ConnectionInfo.Slot);
                string currentSeed = Session.RoomState.Seed;
                string saveFilePath = $"AP_Save_{currentPlayer}_{currentSeed}.txt";
                File.WriteAllText(saveFilePath, ProcessedItemIndex.ToString());
            }
        }
    }
}