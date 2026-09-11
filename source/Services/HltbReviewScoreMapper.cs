using System;

namespace HowLongToBeat.Services
{
    /// <summary>
    /// Maps Playnite user scores to HowLongToBeat review score percentages (API <c>review.score</c>).
    /// </summary>
    /// <remarks>
    /// HLTB stores the percentage value itself (e.g. UI 45% → <c>45</c>) in 5% steps.
    /// </remarks>
    internal static class HltbReviewScoreMapper
    {
        private const int StepPercent = 5;
        private const int MinPercent = 0;
        private const int MaxPercent = 100;

        /// <summary>
        /// Tries to map a Playnite user score (typically 0–100) to an HLTB review score.
        /// </summary>
        /// <param name="userScore">Playnite <c>Game.UserScore</c>; null means nothing to send.</param>
        /// <param name="hltbReviewScore">Mapped percentage (multiple of 5) when the method returns true.</param>
        /// <returns><c>false</c> when <paramref name="userScore"/> is null; otherwise <c>true</c>.</returns>
        public static bool TryMapFromPlayniteUserScore(int? userScore, out int hltbReviewScore)
        {
            hltbReviewScore = 0;
            if (!userScore.HasValue)
            {
                return false;
            }

            int clamped = userScore.Value;
            if (clamped < MinPercent)
            {
                clamped = MinPercent;
            }
            else if (clamped > MaxPercent)
            {
                clamped = MaxPercent;
            }

            hltbReviewScore = (int)Math.Round(clamped / (double)StepPercent, MidpointRounding.AwayFromZero) * StepPercent;
            if (hltbReviewScore < MinPercent)
            {
                hltbReviewScore = MinPercent;
            }
            else if (hltbReviewScore > MaxPercent)
            {
                hltbReviewScore = MaxPercent;
            }

            return true;
        }
    }
}
