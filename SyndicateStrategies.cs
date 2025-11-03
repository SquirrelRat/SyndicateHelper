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
                Name = "Comprehensive Scarab Farm",
                MemberGoals = new Dictionary<string, string>
                {
                    { "Cameria", "Intervention (Leader)" }, { "Rin", "Intervention" }, { "Vagan", "Intervention" },
                    { "Gravicius", "Intervention" }, { "Tora", "Fortification" }, { "Hillock", "Fortification" },
                    { "Guff", "Fortification" }, { "Aisling", "Research" }, { "Vorici", "Research" },
                    { "It That Fled", "Research" }, { "Leo", "Transportation" }, { "Janus", "Transportation" }
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
                Name = "Crafting Meta (Research)",
                MemberGoals = new Dictionary<string, string>
                {
                    { "Aisling", "Research (Leader)" }, { "Vorici", "Research" }, { "It That Fled", "Research" },
                    { "Hillock", "Fortification (Leader)" }, { "Tora", "Fortification" }, { "Guff", "Fortification" },
                    { "Vagan", "Intervention" }, { "Cameria", "Intervention" }
                },
                ScoreOverrides = new Dictionary<string, int>
                {
                    { "PromoteNPCScore", 60 },
                    { "ExecuteScore", 50 },
                    { "SwapNPCJobScore", 20 }
                }
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
                    { "GainItemCurrencyScore", 90 },
                    { "GainItemAnyUniqueScore", 60 }
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
            },
            new SyndicateStrategyDefinition
            {
                Name = "Einhar's Menagerie",
                MemberGoals = new Dictionary<string, string>
                {
                    { "Jorgin", "Research (Leader)" }, { "It That Fled", "Research" }, { "Vorici", "Research" },
                    { "Aisling", "Research" }, { "Tora", "Fortification" }, { "Guff", "Fortification" },
                    { "Vagan", "Intervention" }, { "Rin", "Intervention" }
                }
            },
            new SyndicateStrategyDefinition
            {
                Name = "The Atlas Grind",
                MemberGoals = new Dictionary<string, string>
                {
                    { "Hillock", "Transportation" }, { "Gravicius", "Fortification" }, { "Cameria", "Intervention" },
                    { "Rin", "Intervention" }, { "It That Fled", "Research" }, { "Vorici", "Research" }
                }
            }
        };
    }
}
