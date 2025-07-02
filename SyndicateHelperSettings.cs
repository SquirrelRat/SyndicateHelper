using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;
using System.Collections.Generic;

namespace SyndicateHelper
{
    public class SyndicateHelperSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(true);

        [Menu("Debug")]
        public ToggleNode EnableDebugDrawing { get; set; } = new ToggleNode(false);

        [Menu("Frame Thickness")]
        public RangeNode<int> FrameThickness { get; set; } = new RangeNode<int>(2, 1, 10);
        
        [Menu("Colors")]
        public EmptyNode Colors { get; set; }
        [Menu("Good Choice")] public ColorNode GoodChoiceColor { get; set; } = new ColorNode(Color.LimeGreen);
        [Menu("Goal Completion Choice")] public ColorNode GoalCompletionColor { get; set; } = new ColorNode(new Color(157, 0, 255));
        [Menu("Neutral Choice")] public ColorNode NeutralChoiceColor { get; set; } = new ColorNode(Color.Yellow);
        [Menu("Bad Choice")] public ColorNode BadChoiceColor { get; set; } = new ColorNode(Color.Red);
        
        [Menu("Strategy Profile")]
        public ListNode StrategyProfile { get; set; } = new ListNode();
        
        [Menu("Action Score Weights")]
        public EmptyNode ActionScoreWeights { get; set; }
        [Menu("Execute", "Score for ranking up a member.")] public RangeNode<int> ExecuteScore { get; set; } = new RangeNode<int>(35, 0, 100);
        [Menu("Promote NPC", "Score for ranking up another member.")] public RangeNode<int> PromoteNPCScore { get; set; } = new RangeNode<int>(40, 0, 100);
        [Menu("Steal Ranks", "Base score for stealing ranks.")] public RangeNode<int> StealRanksScore { get; set; } = new RangeNode<int>(60, 0, 100);
        [Menu("Gain Scarabs", "Score for dropping scarabs.")] public RangeNode<int> GainItemScarabScore { get; set; } = new RangeNode<int>(80, 0, 100);
        [Menu("Gain Uniques", "Score for dropping uniques.")] public RangeNode<int> GainItemAnyUniqueScore { get; set; } = new RangeNode<int>(40, 0, 100);
        [Menu("Gain Currency", "Score for dropping currency.")] public RangeNode<int> GainItemCurrencyScore { get; set; } = new RangeNode<int>(35, 0, 100);
        [Menu("Make Friends/Rivals", "Score for forming a relationship.")] public RangeNode<int> NPCBefriendsAnotherScore { get; set; } = new RangeNode<int>(30, 0, 100);
        [Menu("Gain Intelligence", "Score for gaining intelligence directly.")] public RangeNode<int> GainIntelligenceScore { get; set; } = new RangeNode<int>(15, 0, 100);
        [Menu("Gain Large Intelligence", "Score for gaining a large amount of intelligence.")] public RangeNode<int> GainIntelligenceLargeScore { get; set; } = new RangeNode<int>(25, 0, 100);
        [Menu("Swap Jobs", "Base score for swapping jobs.")] public RangeNode<int> SwapNPCJobScore { get; set; } = new RangeNode<int>(5, -100, 100);
        [Menu("Swap Leader", "Base score for swapping leader.")] public RangeNode<int> SwapLeaderScore { get; set; } = new RangeNode<int>(5, -100, 100);
        [Menu("Destroy Items", "Score for destroying items. Should be negative.")] public RangeNode<int> DestroyItemsScore { get; set; } = new RangeNode<int>(-50, -100, 0);
        [Menu("Remove Rivalries", "Score for removing rivalries. Should be negative.")] public RangeNode<int> RemoveRivalriesScore { get; set; } = new RangeNode<int>(-75, -100, 0);
        [Menu("Remove From Prison", "Score for removing all from prison. Should be negative.")] public RangeNode<int> RemoveFromPrisonScore { get; set; } = new RangeNode<int>(-80, -100, 0);
        
        [Menu("Member Goals")]
        public EmptyNode MemberGoals { get; set; }
        [Menu("Aisling")] public ListNode Aisling { get; set; } = new ListNode();
        [Menu("Cameria")] public ListNode Cameria { get; set; } = new ListNode();
        [Menu("Elreon")] public ListNode Elreon { get; set; } = new ListNode();
        [Menu("Gravicius")] public ListNode Gravicius { get; set; } = new ListNode();
        [Menu("Guff")] public ListNode Guff { get; set; } = new ListNode();
        [Menu("Haku")] public ListNode Haku { get; set; } = new ListNode();
        [Menu("Hillock")] public ListNode Hillock { get; set; } = new ListNode();
        [Menu("It That Fled")] public ListNode ItThatFled { get; set; } = new ListNode();
        [Menu("Janus")] public ListNode Janus { get; set; } = new ListNode();
        [Menu("Jorgin")] public ListNode Jorgin { get; set; } = new ListNode();
        [Menu("Korell")] public ListNode Korell { get; set; } = new ListNode();
        [Menu("Leo")] public ListNode Leo { get; set; } = new ListNode();
        [Menu("Rin")] public ListNode Rin { get; set; } = new ListNode();
        [Menu("Riker")] public ListNode Riker { get; set; } = new ListNode();
        [Menu("Tora")] public ListNode Tora { get; set; } = new ListNode();
        [Menu("Vagan")] public ListNode Vagan { get; set; } = new ListNode();
        [Menu("Vorici")] public ListNode Vorici { get; set; } = new ListNode();
        
        public SyndicateHelperSettings()
        {
            var goalOptions = new List<string> {
                "None", "Transportation", "Transportation (Leader)", "Fortification", "Fortification (Leader)",
                "Research", "Research (Leader)", "Intervention", "Intervention (Leader)"
            };
            
            var profileOptions = new List<string> {
                "Custom", "Comprehensive Scarab Farm", "Crafting Meta (Research)", "Gamble (Currency/Div)",
                "Delve Deeper", "Einhar's Menagerie", "The Atlas Grind"
            };
            StrategyProfile.Values = profileOptions;
            StrategyProfile.Value = "Comprehensive Scarab Farm";
            
            var members = new[] {
                Aisling, Cameria, Elreon, Gravicius, Guff, Haku, Hillock, ItThatFled, Janus, Jorgin,
                Korell, Leo, Rin, Riker, Tora, Vagan, Vorici
            };
            foreach (var member in members) member.Values = goalOptions;
        }
    }
}