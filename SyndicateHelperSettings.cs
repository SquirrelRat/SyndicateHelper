// SyndicateHelperSettings.cs
// Configuration settings for the SyndicateHelper plugin.
// Defines all user-configurable options including strategy profiles, visual styles, action scores, and member goals.

using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;
using System.Collections.Generic;
using System.Linq;

namespace SyndicateHelper
{
    public class SyndicateHelperSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(true);

        [Menu("Strategies Configuration", "Configure strategy profiles and member goals")]
        public EmptyNode StrategiesConfiguration { get; set; }

        [Menu("Strategy Profile", "Pre-configured strategy profiles")]
        public ListNode StrategyProfile { get; set; } = new ListNode();

        [Menu("Visual Style", "Customize appearance of overlay")]
        public EmptyNode VisualStyle { get; set; }

        [Menu("Background Alpha", "Transparency of background (0-255)")]
        public RangeNode<int> BackgroundAlpha { get; set; } = new RangeNode<int>(166, 0, 255);

        [Menu("Frame Thickness", "Thickness of drawn frames in pixels")]
        public RangeNode<int> FrameThickness { get; set; } = new RangeNode<int>(2, 1, 10);

        [Menu("Good Choice", "Color for choices that advance your goals")]
        public ColorNode GoodChoiceColor { get; set; } = new ColorNode(Color.LimeGreen);

        [Menu("Goal Completion", "Color for choices that complete a member's goal")]
        public ColorNode GoalCompletionColor { get; set; } = new ColorNode(new Color((byte)157, (byte)0, (byte)255, (byte)255));

        [Menu("Neutral Choice", "Color for choices with no significant impact")]
        public ColorNode NeutralChoiceColor { get; set; } = new ColorNode(Color.Yellow);

        [Menu("Bad Choice", "Color for choices that work against your goals")]
        public ColorNode BadChoiceColor { get; set; } = new ColorNode(Color.Red);

        [Menu("UI Settings", "Toggle UI elements and debug options")]
        public EmptyNode UISettings { get; set; }

        [Menu("Show Goal Info", "Display goal information on portraits")]
        public ToggleNode ShowGoalInfo { get; set; } = new ToggleNode(true);

        [Menu("Show Action Buttons", "Show clickable action buttons")]
        public ToggleNode ShowButtons { get; set; } = new ToggleNode(true);

        [Menu("Show Curve Connections", "Display Bezier curves between goals and actions")]
        public ToggleNode ShowCurves { get; set; } = new ToggleNode(true);

        [Menu("Debug", "Debug and development options")]
        public EmptyNode Debug { get; set; }

        [Menu("Enable Debug Drawing", "Show additional debug information")]
        public ToggleNode EnableDebugDrawing { get; set; } = new ToggleNode(false);

        [Menu("Draw Portraits", "Draw member portrait outlines for debugging")]
        public ToggleNode DrawPortraits { get; set; } = new ToggleNode(false);

        [Menu("Draw Relationships", "Draw relationship lines between members")]
        public ToggleNode DrawRelations { get; set; } = new ToggleNode(false);

        [Menu("Action Score Weights", "How much each action type contributes to scoring")]
        public EmptyNode ActionScoreWeights { get; set; }

        [Menu("Positive Outcomes", "Actions that give benefits")]
        public EmptyNode PositiveOutcomes { get; set; }

        [Menu("Execute", "Score for ranking up a member")]
        public RangeNode<int> ExecuteScore { get; set; } = new RangeNode<int>(35, 0, 100);

        [Menu("Promote NPC", "Score for ranking up another member")]
        public RangeNode<int> PromoteNPCScore { get; set; } = new RangeNode<int>(40, 0, 100);

        [Menu("Steal Ranks", "Base score for stealing ranks")]
        public RangeNode<int> StealRanksScore { get; set; } = new RangeNode<int>(60, 0, 100);

        [Menu("Gain Scarabs", "Score for dropping scarabs")]
        public RangeNode<int> GainItemScarabScore { get; set; } = new RangeNode<int>(80, 0, 100);

        [Menu("Gain Uniques", "Score for dropping uniques")]
        public RangeNode<int> GainItemAnyUniqueScore { get; set; } = new RangeNode<int>(40, 0, 100);

        [Menu("Gain Currency", "Score for dropping currency")]
        public RangeNode<int> GainItemCurrencyScore { get; set; } = new RangeNode<int>(35, 0, 100);

        [Menu("Make Friends/Rivals", "Score for forming a relationship")]
        public RangeNode<int> NPCBefriendsAnotherScore { get; set; } = new RangeNode<int>(30, 0, 100);

        [Menu("Gain Intelligence", "Score for gaining intelligence directly")]
        public RangeNode<int> GainIntelligenceScore { get; set; } = new RangeNode<int>(15, 0, 100);

        [Menu("Gain Large Intelligence", "Score for gaining a large amount of intelligence")]
        public RangeNode<int> GainIntelligenceLargeScore { get; set; } = new RangeNode<int>(25, 0, 100);

        [Menu("Mixed Outcomes", "Actions with mixed positive/negative effects")]
        public EmptyNode MixedOutcomes { get; set; }

        [Menu("Swap Jobs", "Base score for swapping jobs")]
        public RangeNode<int> SwapNPCJobScore { get; set; } = new RangeNode<int>(5, -100, 100);

        [Menu("Swap Leader", "Base score for swapping leader")]
        public RangeNode<int> SwapLeaderScore { get; set; } = new RangeNode<int>(5, -100, 100);

        [Menu("Negative Outcomes", "Actions that generally work against you")]
        public EmptyNode NegativeOutcomes { get; set; }

        [Menu("Destroy Items", "Score for destroying items (should be negative)")]
        public RangeNode<int> DestroyItemsScore { get; set; } = new RangeNode<int>(-50, -100, 0);

        [Menu("Remove Rivalries", "Score for removing rivalries (should be negative)")]
        public RangeNode<int> RemoveRivalriesScore { get; set; } = new RangeNode<int>(-75, -100, 0);

        [Menu("Remove From Prison", "Score for removing all from prison (should be negative)")]
        public RangeNode<int> RemoveFromPrisonScore { get; set; } = new RangeNode<int>(-80, -100, 0);

        [Menu("Relationship Strategy", "Configure how relationships affect scoring")]
        public EmptyNode RelationshipStrategy { get; set; }

        [Menu("Opposed Divisions", "Comma-separated pairs of divisions that should NOT have relationships (e.g., 'Transportation-Research,Fortification-Intervention')")]
        public TextNode OpposedDivisions { get; set; } = new TextNode("");

        [Menu("Allied Divisions", "Comma-separated pairs of divisions that SHOULD have relationships (e.g., 'Fortification-Transportation,Intervention-Research')")]
        public TextNode AlliedDivisions { get; set; } = new TextNode("");

        [Menu("Relationship Score Modifier", "Score multiplier for choices that affect relationships (0-100%)")]
        public RangeNode<int> RelationshipScoreModifier { get; set; } = new RangeNode<int>(50, 0, 100);

        [Menu("Member Goals", "Set desired division for each syndicate member")]
        public EmptyNode MemberGoals { get; set; }

        [Menu("Fortification Members", "Members in Fortification division")]
        public EmptyNode FortificationMembers { get; set; }
        [Menu("Aisling")] public ListNode Aisling { get; set; } = new ListNode();
        [Menu("Cameria")] public ListNode Cameria { get; set; } = new ListNode();
        [Menu("Elreon")] public ListNode Elreon { get; set; } = new ListNode();
        [Menu("Gravicius")] public ListNode Gravicius { get; set; } = new ListNode();

        [Menu("Research Members", "Members in Research division")]
        public EmptyNode ResearchMembers { get; set; }
        [Menu("Guff")] public ListNode Guff { get; set; } = new ListNode();
        [Menu("Haku")] public ListNode Haku { get; set; } = new ListNode();
        [Menu("Hillock")] public ListNode Hillock { get; set; } = new ListNode();
        [Menu("It That Fled")] public ListNode ItThatFled { get; set; } = new ListNode();

        [Menu("Intervention Members", "Members in Intervention division")]
        public EmptyNode InterventionMembers { get; set; }
        [Menu("Janus")] public ListNode Janus { get; set; } = new ListNode();
        [Menu("Jorgin")] public ListNode Jorgin { get; set; } = new ListNode();
        [Menu("Korell")] public ListNode Korell { get; set; } = new ListNode();
        [Menu("Leo")] public ListNode Leo { get; set; } = new ListNode();

        [Menu("Transportation Members", "Members in Transportation division")]
        public EmptyNode TransportationMembers { get; set; }
        [Menu("Rin")] public ListNode Rin { get; set; } = new ListNode();
        [Menu("Riker")] public ListNode Riker { get; set; } = new ListNode();
        [Menu("Tora")] public ListNode Tora { get; set; } = new ListNode();
        [Menu("Vagan")] public ListNode Vagan { get; set; } = new ListNode();
        [Menu("Vorici")] public ListNode Vorici { get; set; } = new ListNode();
        
        public SyndicateHelperSettings()
        {
            var goalOptions = new List<string>
            {
                "None",
                "Transportation", "Transportation (Leader)",
                "Fortification", "Fortification (Leader)",
                "Research", "Research (Leader)",
                "Intervention", "Intervention (Leader)"
            };

            var profileOptions = SyndicateStrategies.Strategies.Select(s => s.Name).ToList();
            profileOptions.Insert(0, "Custom");
            StrategyProfile.Values = profileOptions;
            StrategyProfile.Value = "3.28 Scarab Powerfarm";

            var allMembers = new List<ListNode>
            {
                Aisling, Cameria, Elreon, Gravicius,
                Guff, Haku, Hillock, ItThatFled,
                Janus, Jorgin, Korell, Leo,
                Rin, Riker, Tora, Vagan, Vorici
            };

            foreach (var member in allMembers)
            {
                if (member?.Values == null || member.Values.Count == 0)
                {
                    member.Values = goalOptions;
                }
            }

            ApplyStrategyGoals(StrategyProfile.Value);
        }

        public void ApplyStrategyGoals(string strategyName)
        {
            var members = new Dictionary<string, ListNode>
            {
                { "Aisling", Aisling }, { "Cameria", Cameria }, { "Elreon", Elreon }, { "Gravicius", Gravicius },
                { "Guff", Guff }, { "Haku", Haku }, { "Hillock", Hillock }, { "It That Fled", ItThatFled },
                { "Janus", Janus }, { "Jorgin", Jorgin }, { "Korell", Korell }, { "Leo", Leo },
                { "Rin", Rin }, { "Riker", Riker }, { "Tora", Tora }, { "Vagan", Vagan }, { "Vorici", Vorici }
            };

            foreach (var member in members.Values)
            {
                member.Value = "None";
            }

            if (strategyName == "Custom") return;

            var strategy = SyndicateStrategies.Strategies.FirstOrDefault(s => s.Name == strategyName);
            if (strategy == null) return;

            foreach (var goal in strategy.MemberGoals)
            {
                if (members.TryGetValue(goal.Key, out var memberNode))
                {
                    memberNode.Value = goal.Value;
                }
            }

            OpposedDivisions.Value = strategy.OpposedDivisions;
            AlliedDivisions.Value = strategy.AlliedDivisions;
        }
    }
}