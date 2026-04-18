using HarmonyLib;
using System; 
using System.Globalization;

namespace RaccoinArchipelagoMod
{
    [HarmonyPatch(typeof(GameplayData), "EndRound")]
    public class RoundEndMilestonePatch
    {
        [HarmonyPostfix]
        public static void Postfix(GameplayData __instance) 
        {
            if (!ArchipelagoManager.IsConnected) return;

            try
            {
                // Grab the raw RabbitLong object directly from the game
                RabbitLong currentPoints = __instance.curPt;
                
                // Convert it to a real C# number using math
                // Formula: mantissa * 10^exponent
                double trueScoreDouble = currentPoints.mantissa * Math.Pow(10, currentPoints.exponent);
                
                // Safely cast to a 64-bit integer. 
                // (A 'long' maxes out at 9.2 Quintillion)
                long roundScore = (long)trueScoreDouble;

                // dd this round's true score to the grand total
                RaccoinPlugin.SavedCumulativeScore.Value += roundScore;
                
                // Save the score total to the hard drive
                RaccoinPlugin.Instance.Config.Save();

                long grandTotal = RaccoinPlugin.SavedCumulativeScore.Value;
                RaccoinPlugin.ModLogger.LogMessage($"[AP] Round Ended! Added {roundScore}. New Grand Total: {grandTotal}");

                // Check Archipelago against the TOTAL score in the file
                ArchipelagoManager.CheckScoreMilestones(grandTotal);
            }
            catch (Exception e)
            {
                RaccoinPlugin.ModLogger.LogError($"[AP] Milestone check failed: {e.Message}");
            }
        }
    }
}