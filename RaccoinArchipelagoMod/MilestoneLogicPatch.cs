using HarmonyLib;
using System; 
using System.Globalization;

namespace RaccoinArchipelagoMod
{
    [HarmonyPatch(typeof(GameplayData), "EndRound")] 
    public class RoundEndMilestonePatch
    {
        [HarmonyPostfix]
        static void Postfix(GameplayData __instance) 
        {
            if (!ArchipelagoManager.IsConnected) return;

            try
            {
                string scoreText = __instance.curPt.ToString().Replace(",", "").Trim();
                
                if (double.TryParse(scoreText, NumberStyles.Any, CultureInfo.InvariantCulture, out double currentScoreDouble))
                {
                    long roundScore = (long)currentScoreDouble;

                    // Add this round's score to the grand total
                    RaccoinPlugin.SavedCumulativeScore.Value += roundScore;
                    
                    // save the score total to the hard drive
                    RaccoinPlugin.Instance.Config.Save();

                    long grandTotal = RaccoinPlugin.SavedCumulativeScore.Value;
                    RaccoinPlugin.ModLogger.LogMessage($"[AP] Round Ended! Added {roundScore}. New Grand Total: {grandTotal}");

                    // Check Archipelago against the TOTAL score in the file, not the round score
                    ArchipelagoManager.CheckScoreMilestones(grandTotal);
                }
            }
            catch (Exception e)
            {
                RaccoinPlugin.ModLogger.LogError($"[AP] Milestone check failed: {e.Message}");
            }
        }
    }
}