
using System.Collections.Generic;

namespace SyndicateHelper
{
    public class SyndicateStrategyDefinition
    {
        public string Name { get; set; }
        public Dictionary<string, string> MemberGoals { get; set; }
        public Dictionary<string, int> ScoreOverrides { get; set; } = new Dictionary<string, int>();
        public string OpposedDivisions { get; set; } = "";
        public string AlliedDivisions { get; set; } = "";
    }

    public static class SyndicateStrategies
    {
        public static readonly List<SyndicateStrategyDefinition> Strategies = new List<SyndicateStrategyDefinition>
        {
            new SyndicateStrategyDefinition
            {
                Name = "3.29 Scarab Farm (2-2-5-5)",
                MemberGoals = new Dictionary<string, string>
                {
                    { "It That Fled", "Transportation (Leader)" }, { "Gravicius", "Transportation" },
                    { "Vorici", "Research (Leader)" }, { "Aisling", "Research" },
                    { "Vagan", "Intervention" }, { "Cameria", "Intervention" }, { "Rin", "Intervention" },
                    { "Haku", "Intervention" }, { "Leo", "Intervention" },
                    { "Hillock", "Fortification" }, { "Tora", "Fortification" }
                },
                ScoreOverrides = new Dictionary<string, int>
                {
                    { "GainItemScarabScore", 70 },
                    { "PromoteNPCScore", 50 },
                    { "ExecuteScore", 45 },
                    { "GainItemAnyUniqueScore", 60 },
                    { "GainIntelligenceScore", 15 },
                    { "StealRanksScore", 60 }
                },
                OpposedDivisions = "Transportation-Research",
                AlliedDivisions = "Intervention-Fortification"
            },
            new SyndicateStrategyDefinition
            {
                Name = "Relationship-Based",
                MemberGoals = new Dictionary<string, string>
                {
                    { "Gravicius", "Transportation" }, { "Rin", "Transportation" }, { "Janus", "Research" },
                    { "Guff", "Research" }, { "Hillock", "Fortification" }
                },
                ScoreOverrides = new Dictionary<string, int>
                {
                    { "NPCBefriendsAnotherScore", 100 },
                    { "RelationshipScoreModifier", 75 },
                    { "RemoveRivalriesScore", -50 }
                },
                OpposedDivisions = "Transportation-Research,Fortification-Intervention",
                AlliedDivisions = "Fortification-Transportation,Fortification-Research,Intervention-Transportation,Intervention-Research"
            },
            new SyndicateStrategyDefinition
            {
                Name = "Gamble (Currency/Div)",
                MemberGoals = new Dictionary<string, string>
                {
                    { "It That Fled", "Research" }, { "Jorgin", "Research" }, { "Vorici", "Research" },
                    { "Leo", "Research" }, { "Rin", "Intervention" }, { "Cameria", "Intervention" },
                    { "Gravicius", "Intervention" }
                },
                ScoreOverrides = new Dictionary<string, int>
                {
                    { "PromoteNPCScore", 50 },
                    { "ExecuteScore", 40 }
                }
            },
            new SyndicateStrategyDefinition
            {
                Name = "Einhar's Menagerie",
                MemberGoals = new Dictionary<string, string>
                {
                    { "Jorgin", "Research (Leader)" }, { "It That Fled", "Research" }, { "Vorici", "Research" },
                    { "Aisling", "Research" }, { "Tora", "Fortification" }, { "Guff", "Fortification" },
                    { "Vagan", "Intervention" }, { "Cameria", "Intervention" }, { "Gravicius", "Intervention" }
                },
                ScoreOverrides = new Dictionary<string, int>
                {
                    { "GainItemScarabScore", 100 },
                    { "PromoteNPCScore", 50 },
                    { "ExecuteScore", 40 }
                }
            },
            new SyndicateStrategyDefinition
            {
                Name = "The Atlas Grind",
                MemberGoals = new Dictionary<string, string>
                {
                    { "Hillock", "Transportation" }, { "Gravicius", "Transportation" }, { "Tora", "Fortification" },
                    { "Haku", "Intervention" }, { "Janus", "Intervention" }, { "Leo", "Intervention" },
                    { "Rin", "Intervention" }, { "Vagan", "Intervention" }, { "Vorici", "Intervention" }
                },
                ScoreOverrides = new Dictionary<string, int>
                {
                    { "GainItemAnyUniqueScore", 90 },
                    { "GainItemCurrencyScore", 80 }
                }
            },
            new SyndicateStrategyDefinition
            {
                Name = "Delve Deeper",
                MemberGoals = new Dictionary<string, string>
                {
                    { "Hillock", "Transportation" }, { "Gravicius", "Fortification" }, { "Tora", "Research" },
                    { "Vagan", "Intervention" }, { "Rin", "Intervention" }, { "Cameria", "Intervention" }
                },
                ScoreOverrides = new Dictionary<string, int>
                {
                    { "GainIntelligenceLargeScore", 80 },
                    { "GainIntelligenceScore", 40 }
                }
            }
        };
    }
}
