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
        private readonly HashSet<Tuple<SyndicateDivision, SyndicateDivision>> _opposedDivisions = new HashSet<Tuple<SyndicateDivision, SyndicateDivision>>();
        private readonly HashSet<Tuple<SyndicateDivision, SyndicateDivision>> _alliedDivisions = new HashSet<Tuple<SyndicateDivision, SyndicateDivision>>();

        public SyndicateStrategy(SyndicateHelperSettings settings, Dictionary<string, SyndicateMemberState> boardState, int imprisonedMemberCount)
        {
            _settings = settings;
            _boardState = boardState;
            _imprisonedMemberCount = imprisonedMemberCount;

            if (settings.StrategyProfile.Value == "Relationship-Based")
            {
                ParseRelationshipRules(settings.OpposedDivisions.Value, _opposedDivisions);
                ParseRelationshipRules(settings.AlliedDivisions.Value, _alliedDivisions);
            }
        }
        
        public int ScoreChoiceByCode(string actionCode, SyndicateDecision decision)
        {
            if (string.IsNullOrWhiteSpace(actionCode)) return -999;

            if (_settings.StrategyProfile.Value == "Relationship-Based" && actionCode == "NPCBefriendsAnother")
            {
                return _settings.NPCBefriendsAnotherScore.Value + ScoreRelationshipChoice(decision);
            }

            if (actionCode == "Interrogate")
            {
                if (!_boardState.TryGetValue(decision.MemberName, out var memberState)) return 0;
                var desiredGoal = ParseGoal(GetDesiredDivisionForMember(decision.MemberName));
                if (desiredGoal.IsPrimaryLeader && memberState.IsLeader && memberState.Division == desiredGoal.Division)
                {
                    return -200;
                }

                if (_imprisonedMemberCount < 3)
                {
                    int rank = memberState.Rank switch { "Sergeant" => 1, "Lieutenant" => 2, "Captain" => 3, _ => 0 };
                    return 10 + (rank * 10);
                }
                else
                {
                    return -100;
                }
            }
            
            switch (actionCode)
            {
                case "Execute": return _settings.ExecuteScore.Value;
                case "PromoteNPC": return _settings.PromoteNPCScore.Value;
                case "NPCBefriendsAnother": return _settings.NPCBefriendsAnotherScore.Value;
                case "GainItemScarab": return _settings.GainItemScarabScore.Value;
                case "GainItemAnyUnique": return _settings.GainItemAnyUniqueScore.Value;
                case "GainItemCurrency": return _settings.GainItemCurrencyScore.Value;
                case "GainItemMap": return 20;
                case "GainItemVeiledItem": return 20;
                case "GainIntelligence": return _settings.GainIntelligenceScore.Value;
                case "GainIntelligenceLarge": return _settings.GainIntelligenceLargeScore.Value;
                case "DestroyAllItemsInDivision": return _settings.DestroyItemsScore.Value;
                case "DestroyAllItemsOfRivalDivision": return _settings.DestroyItemsScore.Value;
                case "RemoveAllRivalries": return _settings.RemoveRivalriesScore.Value;
                case "RemoveAllRivalriesInDivision": return _settings.RemoveRivalriesScore.Value;
                case "RemoveAllFromPrison": return _settings.RemoveFromPrisonScore.Value;
                case "SwapNPCJob": return ScoreSwapJob(decision);
                case "SwapLeader": return ScoreSwapLeader(decision);
                case "StealRanks": return ScoreStealRanks(decision);
                case "StealIntelligence": return ScoreStealIntelligence(decision);
                case "RemoveNPCFromOrg": return ScoreRemoveNpc(decision);
                case "NPCLeavesOrg": return ScoreRemoveNpc(decision);
                case "DownrankRivalsUprankMyDivision": return 50;
                case "ExecuteSafehouse": return 0;
                default: return 0;
            }
        }
        
        #region Contextual Scoring Helpers
        private int ScoreSwapJob(SyndicateDecision decision)
        {
            return _settings.SwapNPCJobScore.Value;
        }

        private int ScoreSwapLeader(SyndicateDecision decision)
        {
            return _settings.SwapLeaderScore.Value;
        }

        private int ScoreStealRanks(SyndicateDecision decision)
        {
            return _settings.StealRanksScore.Value;
        }
        
        private int ScoreStealIntelligence(SyndicateDecision decision)
        {
            return 20;
        }

        private int ScoreRemoveNpc(SyndicateDecision decision)
        {
            var desiredGoal = ParseGoal(GetDesiredDivisionForMember(decision.MemberName));
            return (desiredGoal.Division == SyndicateDivision.None) ? 40 : -60;
        }

        private int ScoreRelationshipChoice(SyndicateDecision decision)
        {
            var text = decision.SpecialText;
            var match = System.Text.RegularExpressions.Regex.Match(text, "(.+?) (befriends|becomes rivals with) (.+)");
            if (!match.Success) return 0;

            var member1Name = match.Groups[1].Value.Trim();
            var relationshipType = match.Groups[2].Value.Trim();
            var member2Name = match.Groups[3].Value.Trim();

            if (!_boardState.TryGetValue(member1Name, out var member1State) || !_boardState.TryGetValue(member2Name, out var member2State)) return 0;

            var tuple = new Tuple<SyndicateDivision, SyndicateDivision>(member1State.Division, member2State.Division);

            if (relationshipType == "befriends")
            {
                if (_opposedDivisions.Contains(tuple)) return -_settings.RelationshipScoreModifier.Value;
                if (_alliedDivisions.Contains(tuple)) return _settings.RelationshipScoreModifier.Value;
            }
            else if (relationshipType == "becomes rivals with")
            {
                if (_opposedDivisions.Contains(tuple)) return _settings.RelationshipScoreModifier.Value;
                if (_alliedDivisions.Contains(tuple)) return -_settings.RelationshipScoreModifier.Value;
            }

            return 0;
        }
        #endregion

        #region Utility methods
        private MemberGoal ParseGoal(string goal)
        {
            if (string.IsNullOrEmpty(goal) || goal == "None")
                return new MemberGoal { Division = SyndicateDivision.None, IsPrimaryLeader = false };
            var isLeader = goal.Contains("(Leader)");
            var divisionName = goal.Replace(" (Leader)", "").Trim();
            if (System.Enum.TryParse(divisionName, out SyndicateDivision division))
                return new MemberGoal { Division = division, IsPrimaryLeader = isLeader };
            return new MemberGoal { Division = SyndicateDivision.None, IsPrimaryLeader = false };
        }

        private string GetDesiredDivisionForMember(string memberName)
        {
            var property = _settings.GetType().GetProperty(memberName);
            if (property == null) return "None";
            var listNode = property.GetValue(_settings) as ExileCore.Shared.Nodes.ListNode;
            return listNode?.Value ?? "None";
        }

        private void ParseRelationshipRules(string rules, HashSet<Tuple<SyndicateDivision, SyndicateDivision>> ruleSet)
        {
            if (string.IsNullOrWhiteSpace(rules)) return;
            var pairs = rules.Split(',');
            foreach (var pair in pairs)
            {
                var divisions = pair.Split('-');
                if (divisions.Length == 2 && System.Enum.TryParse(divisions[0], out SyndicateDivision div1) && System.Enum.TryParse(divisions[1], out SyndicateDivision div2))
                {
                    ruleSet.Add(new Tuple<SyndicateDivision, SyndicateDivision>(div1, div2));
                    ruleSet.Add(new Tuple<SyndicateDivision, SyndicateDivision>(div2, div1)); // Add the reverse pair for easy lookup
                }
            }
        }
        #endregion
    }
}