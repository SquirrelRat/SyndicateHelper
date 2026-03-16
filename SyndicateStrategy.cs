// SyndicateStrategy.cs
// Decision scoring engine for syndicate encounter choices.
// Evaluates actions based on current board state, member goals, and configured strategy weights.

using System;
using System.Collections.Generic;
using System.Linq;

namespace SyndicateHelper
{
    public class SyndicateStrategy
    {
        private readonly SyndicateHelperSettings _settings;
        private readonly Dictionary<string, SyndicateMemberState> _boardState;
        private readonly int _imprisonedMemberCount;
        private readonly SyndicateStrategyDefinition _currentStrategy;
        private readonly HashSet<Tuple<SyndicateDivision, SyndicateDivision>> _opposedDivisions = new HashSet<Tuple<SyndicateDivision, SyndicateDivision>>();
        private readonly HashSet<Tuple<SyndicateDivision, SyndicateDivision>> _alliedDivisions = new HashSet<Tuple<SyndicateDivision, SyndicateDivision>>();

        public SyndicateStrategy(SyndicateHelperSettings settings, Dictionary<string, SyndicateMemberState> boardState, int imprisonedMemberCount, SyndicateStrategyDefinition currentStrategy)
        {
            _settings = settings;
            _boardState = boardState;
            _imprisonedMemberCount = imprisonedMemberCount;
            _currentStrategy = currentStrategy;

            if (_currentStrategy?.Name == "Relationship-Based")
            {
                ParseRelationshipRules(_currentStrategy.OpposedDivisions, _opposedDivisions);
                ParseRelationshipRules(_currentStrategy.AlliedDivisions, _alliedDivisions);
            }
        }

        private int GetScore(string scoreName, int defaultValue)
        {
            if (_currentStrategy?.ScoreOverrides.TryGetValue(scoreName, out var score) ?? false)
            {
                return score;
            }
            return defaultValue;
        }

        public int ScoreChoiceByCode(string actionCode, SyndicateDecision decision)
        {
            if (string.IsNullOrWhiteSpace(actionCode)) return -999;

            if (_settings.StrategyProfile.Value == "Relationship-Based" && actionCode == "NPCBefriendsAnother")
            {
                return GetScore("NPCBefriendsAnotherScore", _settings.NPCBefriendsAnotherScore.Value) + ScoreRelationshipChoice(decision);
            }

            if (actionCode == "Interrogate")
            {
                if (!_boardState.TryGetValue(decision.MemberName, out var memberState)) return 0;
                var desiredGoal = SyndicateHelperUtility.ParseGoal(SyndicateHelperUtility.GetDesiredDivisionForMember(decision.MemberName, _settings));
                if (desiredGoal.IsPrimaryLeader && memberState.IsLeader && memberState.Division == desiredGoal.Division)
                {
                    return SyndicateHelperConstants.PenaltyLeaderInterrogation;
                }

                if (_imprisonedMemberCount < SyndicateHelperConstants.MaxPrisonSlots)
                {
                    int rank = memberState.Rank switch { "Sergeant" => 1, "Lieutenant" => 2, "Captain" => 3, _ => 0 };
                    return SyndicateHelperConstants.BaseInterrogateScore + (rank * SyndicateHelperConstants.InterrogateRankMultiplier);
                }
                else
                {
                    return SyndicateHelperConstants.PenaltyPrisonFull;
                }
            }

            switch (actionCode)
            {
                case "Execute": return GetScore("ExecuteScore", _settings.ExecuteScore.Value);
                case "PromoteNPC": return GetScore("PromoteNPCScore", _settings.PromoteNPCScore.Value);
                case "NPCBefriendsAnother": return GetScore("NPCBefriendsAnotherScore", _settings.NPCBefriendsAnotherScore.Value);
                case "GainItemScarab": return GetScore("GainItemScarabScore", _settings.GainItemScarabScore.Value);
                case "GainItemAnyUnique": return GetScore("GainItemAnyUniqueScore", _settings.GainItemAnyUniqueScore.Value);
                case "GainItemCurrency": return GetScore("GainItemCurrencyScore", _settings.GainItemCurrencyScore.Value);
                case "GainItemMap": return SyndicateHelperConstants.ScoreGainItemMap;
                case "GainItemVeiledItem": return SyndicateHelperConstants.ScoreGainItemVeiledItem;
                case "GainIntelligence": return GetScore("GainIntelligenceScore", _settings.GainIntelligenceScore.Value);
                case "GainIntelligenceLarge": return GetScore("GainIntelligenceLargeScore", _settings.GainIntelligenceLargeScore.Value);
                case "DestroyAllItemsInDivision": return GetScore("DestroyItemsScore", _settings.DestroyItemsScore.Value);
                case "DestroyAllItemsOfRivalDivision": return GetScore("DestroyItemsScore", _settings.DestroyItemsScore.Value);
                case "RemoveAllRivalries": return GetScore("RemoveRivalriesScore", _settings.RemoveRivalriesScore.Value);
                case "RemoveAllRivalriesInDivision": return GetScore("RemoveRivalriesScore", _settings.RemoveRivalriesScore.Value);
                case "RemoveAllFromPrison": return GetScore("RemoveFromPrisonScore", _settings.RemoveFromPrisonScore.Value);
                case "SwapNPCJob": return ScoreSwapJob(decision);
                case "SwapLeader": return ScoreSwapLeader(decision);
                case "StealRanks": return ScoreStealRanks(decision);
                case "StealIntelligence": return ScoreStealIntelligence(decision);
                case "RemoveNPCFromOrg": return ScoreRemoveNpc(decision);
                case "NPCLeavesOrg": return ScoreRemoveNpc(decision);
                case "DownrankRivalsUprankMyDivision": return SyndicateHelperConstants.ScoreDownrankRivalsUprankMyDivision;
                case "ExecuteSafehouse": return 0;
                default: return 0;
            }
        }

        private int ScoreSwapJob(SyndicateDecision decision)
        {
            return GetScore("SwapNPCJobScore", _settings.SwapNPCJobScore.Value);
        }

        private int ScoreSwapLeader(SyndicateDecision decision)
        {
            return GetScore("SwapLeaderScore", _settings.SwapLeaderScore.Value);
        }

        private int ScoreStealRanks(SyndicateDecision decision)
        {
            return GetScore("StealRanksScore", _settings.StealRanksScore.Value);
        }

        private int ScoreStealIntelligence(SyndicateDecision decision)
        {
            return SyndicateHelperConstants.ScoreStealIntelligence;
        }

        private int ScoreRemoveNpc(SyndicateDecision decision)
        {
            var desiredGoal = SyndicateHelperUtility.ParseGoal(SyndicateHelperUtility.GetDesiredDivisionForMember(decision.MemberName, _settings));
            return (desiredGoal.Division == SyndicateDivision.None) ? SyndicateHelperConstants.ScoreRemoveNpcNeutral : SyndicateHelperConstants.ScoreRemoveNpcWithGoal;
        }

        private int ScoreRelationshipChoice(SyndicateDecision decision)
        {
            var text = decision.SpecialText;
            var match = System.Text.RegularExpressions.Regex.Match(text, @"(.+?)\s+(befriends|becomes rivals with)\s+(.+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!match.Success) return 0;

            var member1Name = match.Groups[1].Value.Trim();
            var relationshipType = match.Groups[2].Value.Trim();
            var member2Name = match.Groups[3].Value.Trim();

            if (!_boardState.TryGetValue(member1Name, out var member1State) || !_boardState.TryGetValue(member2Name, out var member2State)) return 0;

            var tuple = new Tuple<SyndicateDivision, SyndicateDivision>(member1State.Division, member2State.Division);
            var relationshipScoreModifier = GetScore("RelationshipScoreModifier", _settings.RelationshipScoreModifier.Value);

            if (relationshipType.Equals("befriends", StringComparison.OrdinalIgnoreCase))
            {
                if (_opposedDivisions.Contains(tuple)) return -relationshipScoreModifier;
                if (_alliedDivisions.Contains(tuple)) return relationshipScoreModifier;
            }
            else if (relationshipType.Equals("becomes rivals with", StringComparison.OrdinalIgnoreCase))
            {
                if (_opposedDivisions.Contains(tuple)) return relationshipScoreModifier;
                if (_alliedDivisions.Contains(tuple)) return -relationshipScoreModifier;
            }

            return 0;
        }

        private void ParseRelationshipRules(string rules, HashSet<Tuple<SyndicateDivision, SyndicateDivision>> ruleSet)
        {
            if (string.IsNullOrWhiteSpace(rules)) return;

            var pairs = rules.Split(',');
            foreach (var pair in pairs)
            {
                var trimmedPair = pair.Trim();
                if (string.IsNullOrWhiteSpace(trimmedPair)) continue;

                var divisions = trimmedPair.Split('-');
                if (divisions.Length != 2) continue;

                var div1Name = divisions[0].Trim();
                var div2Name = divisions[1].Trim();

                if (System.Enum.TryParse(div1Name, out SyndicateDivision div1) &&
                    System.Enum.TryParse(div2Name, out SyndicateDivision div2))
                {
                    ruleSet.Add(new Tuple<SyndicateDivision, SyndicateDivision>(div1, div2));
                    ruleSet.Add(new Tuple<SyndicateDivision, SyndicateDivision>(div2, div1));
                }
            }
        }
    }
}
