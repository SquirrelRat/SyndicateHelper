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

        [Menu("Debug", "Display debug information")]
        public ToggleNode EnableDebugDrawing { get; set; } = new ToggleNode(false);

        [Menu("Frame Thickness", "Thickness of the frames drawn around choices")]
        public RangeNode<int> FrameThickness { get; set; } = new RangeNode<int>(2, 1, 10);
        
        [Menu("Colors")]
        public EmptyNode Colors { get; set; }
        
        [Menu("Good Choice", "Color for a good/recommended choice", parentIndex = 3)]
        public ColorNode GoodChoiceColor { get; set; } = new ColorNode(Color.LimeGreen);

        [Menu("Neutral Choice", "Color for a neutral choice", parentIndex = 3)]
        public ColorNode NeutralChoiceColor { get; set; } = new ColorNode(Color.Yellow);

        [Menu("Bad Choice", "Color for a bad/detrimental choice", parentIndex = 3)]
        public ColorNode BadChoiceColor { get; set; } = new ColorNode(Color.Red);
        
        [Menu("Strategy Profile", "Select a pre-configured strategy")]
        public ListNode StrategyProfile { get; set; } = new ListNode();
        
        [Menu("Member Goals", "Assign a target division for each Syndicate member. Use (Leader) to mark the primary target for a division.")]
        public EmptyNode MemberGoals { get; set; }
        
        [Menu("Aisling", parentIndex = 6)] public ListNode Aisling { get; set; } = new ListNode();
        [Menu("Cameria", parentIndex = 6)] public ListNode Cameria { get; set; } = new ListNode();
        [Menu("Elreon", parentIndex = 6)] public ListNode Elreon { get; set; } = new ListNode();
        [Menu("Gravicius", parentIndex = 6)] public ListNode Gravicius { get; set; } = new ListNode();
        [Menu("Guff", parentIndex = 6)] public ListNode Guff { get; set; } = new ListNode();
        [Menu("Haku", parentIndex = 6)] public ListNode Haku { get; set; } = new ListNode();
        [Menu("Hillock", parentIndex = 6)] public ListNode Hillock { get; set; } = new ListNode();
        [Menu("It That Fled", parentIndex = 6)] public ListNode ItThatFled { get; set; } = new ListNode();
        [Menu("Janus", parentIndex = 6)] public ListNode Janus { get; set; } = new ListNode();
        [Menu("Jorgin", parentIndex = 6)] public ListNode Jorgin { get; set; } = new ListNode();
        [Menu("Korell", parentIndex = 6)] public ListNode Korell { get; set; } = new ListNode();
        [Menu("Leo", parentIndex = 6)] public ListNode Leo { get; set; } = new ListNode();
        [Menu("Rin", parentIndex = 6)] public ListNode Rin { get; set; } = new ListNode();
        [Menu("Riker", parentIndex = 6)] public ListNode Riker { get; set; } = new ListNode();
        [Menu("Tora", parentIndex = 6)] public ListNode Tora { get; set; } = new ListNode();
        [Menu("Vagan", parentIndex = 6)] public ListNode Vagan { get; set; } = new ListNode();
        [Menu("Vorici", parentIndex = 6)] public ListNode Vorici { get; set; } = new ListNode();
        
        public SyndicateHelperSettings()
        {
            var goalOptions = new List<string> {
                "None",
                "Transportation", "Transportation (Leader)",
                "Fortification", "Fortification (Leader)",
                "Research", "Research (Leader)",
                "Intervention", "Intervention (Leader)"
            };

            var profileOptions = new List<string> {
                "Custom",
                "Crafting Meta (Research)",
                "Scarab Farm (Intervention)",
                "Gamble (Currency/Div)"
            };
            StrategyProfile.Values = profileOptions;
            StrategyProfile.Value = "Scarab Farm (Intervention)";

            Aisling.Values = goalOptions; Cameria.Values = goalOptions; Elreon.Values = goalOptions;
            Gravicius.Values = goalOptions; Guff.Values = goalOptions; Haku.Values = goalOptions;
            Hillock.Values = goalOptions; ItThatFled.Values = goalOptions; Janus.Values = goalOptions;
            Jorgin.Values = goalOptions; Korell.Values = goalOptions; Leo.Values = goalOptions;
            Rin.Values = goalOptions; Riker.Values = goalOptions; Tora.Values = goalOptions;
            Vagan.Values = goalOptions; Vorici.Values = goalOptions;
        }
    }
}
