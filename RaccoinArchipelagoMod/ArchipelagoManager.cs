using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
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

                // --- NEW: PULL SLOT DATA ---
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

                // Sync and Setup
                SyncOnConnect();
                Session.Items.ItemReceived += OnItemReceived;
                ScoutMilestoneLocations();
                
                // --- NEW: INJECT DATA INTO GAME ---
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

            while (MilestonesClaimed < ScoreMilestones.Length && currentTotalScore >= ScoreMilestones[MilestonesClaimed])
            {
                long locationIdToSend = MilestoneLocationIDs[MilestonesClaimed];
                Session.Locations.CompleteLocationChecks(locationIdToSend); 
                MilestonesClaimed++;
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