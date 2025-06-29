using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;
using System.Collections.Generic;

namespace SyndicateHelper
{
    public enum SyndicateDivision
    {
        Research,
        Intervention,
        Fortification,
        Transportation,
        None // Option to not track a member
    }

    public class SyndicateHelperSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(true);

        [Menu("Strategy Profile", "Select a strategy to automatically set member goals.")]
        public ListNode StrategyProfile { get; set; } = new ListNode();

        [Menu("Frame Thickness")]
        public RangeNode<int> FrameThickness { get; set; } = new RangeNode<int>(3, 1, 10);

        [Menu("Good Choice Color")]
        public ColorNode GoodChoiceColor { get; set; } = new ColorNode(Color.Green);

        [Menu("Bad Choice Color")]
        public ColorNode BadChoiceColor { get; set; } = new ColorNode(Color.Red);

        [Menu("Neutral Choice Color")]
        public ColorNode NeutralChoiceColor { get; set; } = new ColorNode(Color.Yellow);
        
        [Menu("Aisling")]
        public ListNode Aisling { get; set; } = new ListNode();
        [Menu("Cameria")]
        public ListNode Cameria { get; set; } = new ListNode();
        [Menu("Elreon")]
        public ListNode Elreon { get; set; } = new ListNode();
        [Menu("Gravicius")]
        public ListNode Gravicius { get; set; } = new ListNode();
        [Menu("Guff")]
        public ListNode Guff { get; set; } = new ListNode();
        [Menu("Haku")]
        public ListNode Haku { get; set; } = new ListNode();
        [Menu("Hillock")]
        public ListNode Hillock { get; set; } = new ListNode();
        [Menu("It That Fled")]
        public ListNode ItThatFled { get; set; } = new ListNode();
        [Menu("Janus")]
        public ListNode Janus { get; set; } = new ListNode();
        [Menu("Jorgin")]
        public ListNode Jorgin { get; set; } = new ListNode();
        [Menu("Korell")]
        public ListNode Korell { get; set; } = new ListNode();
        [Menu("Leo")]
        public ListNode Leo { get; set; } = new ListNode();
        [Menu("Rin")]
        public ListNode Rin { get; set; } = new ListNode();
        [Menu("Riker")]
        public ListNode Riker { get; set; } = new ListNode();
        [Menu("Tora")]
        public ListNode Tora { get; set; } = new ListNode();
        [Menu("Vagan")]
        public ListNode Vagan { get; set; } = new ListNode();
        [Menu("Vorici")]
        public ListNode Vorici { get; set; } = new ListNode();
        
        [Menu("Enable Debug Drawing", "Show debug messages on screen to diagnose issues.")]
        public ToggleNode EnableDebugDrawing { get; set; } = new ToggleNode(false);


        public SyndicateHelperSettings()
        {
            var divisionOptions = new List<string> { "Research", "Intervention", "Fortification", "Transportation", "None" };
            StrategyProfile.SetListValues(new List<string> { "Custom", "Crafting Meta (Research)", "Scarab Farm (Intervention)", "Gamble (Currency/Div)" });

            var memberNodes = new[] { Aisling, Cameria, Elreon, Gravicius, Guff, Haku, Hillock, ItThatFled, Janus, Jorgin, Korell, Leo, Rin, Riker, Tora, Vagan, Vorici };
            foreach (var node in memberNodes)
            {
                node.SetListValues(divisionOptions);
                node.Value = "None"; 
            }
        }
    }
}