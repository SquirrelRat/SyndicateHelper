using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Nodes;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Numerics;

namespace SyndicateHelper
{
    // This class is more tightly coupled to UI elements, so it stays in the main file.
    public class SyndicateDecision { public string MemberName { get; set; } public Element InterrogateButton { get; set; } public Element ReleaseButton { get; set; } public Element SpecialButton { get; set; } public string InterrogateText { get; set; } public string SpecialText { get; set; } }

    public class SyndicateHelper : BaseSettingsPlugin<SyndicateHelperSettings>
    {
        private readonly List<Tuple<RectangleF, Color>> _rectanglesToDraw = new();
        private readonly List<Tuple<string, Color>> _strategicGoals = new List<Tuple<string, Color>>();
        private string _currentStrategy = "";
        private readonly List<string> _debugMessages = new List<string>();
        private readonly HashSet<SyndicateDivision> _targetDivisions = new HashSet<SyndicateDivision>();

        private static readonly List<string> SyndicateMemberNames = new List<string> { "Aisling", "Cameria", "Elreon", "Gravicius", "Guff", "Haku", "Hillock", "It That Fled", "Janus", "Jorgin", "Korell", "Leo", "Rin", "Riker", "Tora", "Vagan", "Vorici" };

        public override bool Initialise()
        {
            Name = "SyndicateHelper";
            return true;
        }

        public override Job Tick()
        {
            _rectanglesToDraw.Clear();
            _debugMessages.Clear();
            _strategicGoals.Clear();

            if (!CanRun()) return null;

            if (Settings.StrategyProfile.Value != "Custom" && _currentStrategy != Settings.StrategyProfile.Value)
            {
                ApplyStrategyProfile();
                _currentStrategy = Settings.StrategyProfile.Value;
            }

            var betrayalWindow = GameController.IngameState.IngameUi.BetrayalWindow as SyndicatePanel;
            if (betrayalWindow == null || !betrayalWindow.IsVisible) return null;

            AddDebug("Betrayal window is visible.");
            GenerateStrategicGoals(betrayalWindow);

            var eventDataElement = betrayalWindow.BetrayalEventData as BetrayalEventData;
            if (eventDataElement != null && eventDataElement.IsVisible)
            {
                AddDebug("EventData found, processing choices...");
                ProcessEncounterChoices(betrayalWindow);
            }

            return null;
        }

        public override void Render()
        {
            if (!Settings.Enable) return;

            foreach (var rect in _rectanglesToDraw)
            {
                Graphics.DrawFrame(rect.Item1, rect.Item2, Settings.FrameThickness);
            }

            var betrayalWindow = GameController.IngameState.IngameUi.BetrayalWindow as SyndicatePanel;
            if (betrayalWindow != null && betrayalWindow.IsVisible)
            {
                ProcessBoardOverlays(betrayalWindow);
                RenderStrategyAdvisor(betrayalWindow);
            }

            if (Settings.EnableDebugDrawing.Value)
            {
                var y = 300f;
                var a = new System.Numerics.Vector2(100, y);
                foreach (var msg in _debugMessages)
                {
                    Graphics.DrawText(msg, a, Color.White);
                    a.Y += 20;
                }
            }
        }
        
        private MemberGoal ParseGoal(string goal)
        {
            if (string.IsNullOrEmpty(goal) || goal == "None")
                return new MemberGoal { Division = SyndicateDivision.None, IsPrimaryLeader = false };

            var isLeader = goal.Contains("(Leader)");
            var divisionName = goal.Replace(" (Leader)", "").Trim();

            if (Enum.TryParse(divisionName, out SyndicateDivision division))
            {
                return new MemberGoal { Division = division, IsPrimaryLeader = isLeader };
            }
            return new MemberGoal { Division = SyndicateDivision.None, IsPrimaryLeader = false };
        }

        private void GenerateStrategicGoals(SyndicatePanel betrayalWindow)
        {
            if (betrayalWindow.SyndicateStates == null) return;

            var boardState = ParseBoardState(betrayalWindow);
            
            var allGoals = Settings.GetType().GetProperties()
                .Where(p => p.PropertyType == typeof(ListNode) && SyndicateMemberNames.Contains(p.Name))
                .Select(p => new { Name = p.Name, GoalString = (p.GetValue(Settings) as ListNode).Value })
                .ToDictionary(g => g.Name, g => ParseGoal(g.GoalString));
                
            var targetDivisionsWithGoals = allGoals.Values
                .Where(g => g.Division != SyndicateDivision.None)
                .Select(g => g.Division)
                .Distinct();

            foreach (var division in targetDivisionsWithGoals)
            {
                var primaryLeaderGoal = allGoals.FirstOrDefault(g => g.Value.Division == division && g.Value.IsPrimaryLeader);
                var currentLeader = boardState.Values.FirstOrDefault(m => m.Division == division && m.IsLeader);
                
                if (primaryLeaderGoal.Key != null)
                {
                    if (currentLeader?.Name == primaryLeaderGoal.Key)
                    {
                        _strategicGoals.Add(new Tuple<string, Color>($"{primaryLeaderGoal.Key} is leading {division}. (Optimal)", Color.LimeGreen));
                    }
                    else
                    {
                        if (currentLeader != null)
                        {
                            _strategicGoals.Add(new Tuple<string, Color>($"Problem: {currentLeader.Name} is blocking {primaryLeaderGoal.Key} from leading {division}.", Color.OrangeRed));
                        }
                        
                        if (boardState.TryGetValue(primaryLeaderGoal.Key, out var primaryState))
                        {
                             if (primaryState.Division != division)
                                _strategicGoals.Add(new Tuple<string, Color>($"Move {primaryLeaderGoal.Key} to {division} to become leader", Color.Yellow));
                             else
                                _strategicGoals.Add(new Tuple<string, Color>($"Rank up {primaryLeaderGoal.Key} to become leader of {division}", Color.LightGreen));
                        }
                        else
                        {
                            _strategicGoals.Add(new Tuple<string, Color>($"Place {primaryLeaderGoal.Key} in {division} to become leader", Color.Yellow));
                        }
                    }
                }

                var subordinateGoals = allGoals.Where(g => g.Value.Division == division && !g.Value.IsPrimaryLeader);
                foreach (var subGoal in subordinateGoals)
                {
                    if (boardState.TryGetValue(subGoal.Key, out var subState))
                    {
                        if (subState.Division != division)
                        {
                            _strategicGoals.Add(new Tuple<string, Color>($"Move {subGoal.Key} to {division}", Color.CornflowerBlue));
                        }
                        else if (subState.Rank != "Captain" && !(primaryLeaderGoal.Key != null && currentLeader?.Name == primaryLeaderGoal.Key))
                        {
                            _strategicGoals.Add(new Tuple<string, Color>($"Rank up {subGoal.Key} in {division}", Color.CornflowerBlue));
                        }
                    }
                    else
                    {
                        _strategicGoals.Add(new Tuple<string, Color>($"Place {subGoal.Key} in {division}", Color.CornflowerBlue));
                    }
                }
            }

            if (_strategicGoals.Count == 0)
            {
                _strategicGoals.Add(new Tuple<string, Color>("No strategy configured or board is optimal.", Color.White));
            }
        }
        
        private void ApplyStrategyProfile()
        {
            _targetDivisions.Clear();
            var allNodes = SyndicateMemberNames.ToDictionary(
                name => name,
                name => Settings.GetType().GetProperty(name)?.GetValue(Settings) as ListNode
            );

            // THE FIX IS HERE: Added .Where(n => n != null) to safely skip nulls.
            foreach (var node in allNodes.Values.Where(n => n != null))
            {
                node.Value = "None";
            }

            var strategy = new Dictionary<string, string>();
            switch (Settings.StrategyProfile.Value)
            {
                case "Crafting Meta (Research)":
                    strategy["Aisling"] = "Research (Leader)"; strategy["Vorici"] = "Research";
                    strategy["Hillock"] = "Fortification (Leader)";
                    break;
                case "Scarab Farm (Intervention)":
                    strategy["Cameria"] = "Intervention (Leader)"; strategy["Rin"] = "Intervention";
                    strategy["Vagan"] = "Intervention"; strategy["Janus"] = "Intervention";
                    strategy["Vorici"] = "Intervention";
                    break;
                case "Gamble (Currency/Div)":
                    strategy["Gravicius"] = "Transportation (Leader)"; strategy["Rin"] = "Intervention (Leader)";
                    strategy["Riker"] = "Research";
                    break;
            }

            foreach (var set in strategy)
            {
                if (allNodes.TryGetValue(set.Key, out var node) && node != null)
                {
                    node.Value = set.Value;
                    var parsedGoal = ParseGoal(set.Value);
                    if (parsedGoal.Division != SyndicateDivision.None)
                        _targetDivisions.Add(parsedGoal.Division);
                }
            }
        }

        private void ProcessBoardOverlays(SyndicatePanel betrayalWindow)
        {
            if (betrayalWindow.SyndicateStates == null) return;
            var boardState = ParseBoardState(betrayalWindow);
            var portraitElements = FindPortraitElements(betrayalWindow);
            AddDebug($"Portraits Found: {portraitElements.Count}");

            foreach (var memberState in boardState.Values)
            {
                if (portraitElements.TryGetValue(memberState.Name, out var portraitElement) &&
                    SyndicateRewardData.Rewards.TryGetValue(memberState.Name, out var memberRewards) &&
                    memberRewards.TryGetValue(memberState.Division, out var rewardInfo))
                {
                    var desiredGoal = ParseGoal(GetDesiredDivisionForMember(memberState.Name));
                    var desiredDivision = desiredGoal.Division.ToString();
                    var rewardText = rewardInfo.Text;
                    Color textColor;

                    bool isTargetReward = memberState.Division.ToString() == desiredDivision && desiredDivision != "None";

                    if (isTargetReward) textColor = Settings.GoodChoiceColor.Value;
                    else
                    {
                        textColor = rewardInfo.Tier switch {
                            RewardTier.Great => Color.LimeGreen, RewardTier.Good => Color.Yellow,
                            RewardTier.Average => Color.White, RewardTier.Worst => new Color(255, 80, 80),
                            _ => Color.White
                        };
                    }

                    if (rewardInfo.Tier == RewardTier.Worst && desiredDivision != "None")
                        rewardText += $" (-> {desiredDivision})";

                    var rect = portraitElement.GetClientRectCache;
                    var textSize = Graphics.MeasureText(rewardText);
                    var textPos = new System.Numerics.Vector2(rect.Center.X - textSize.X / 2, rect.Bottom + 2);
                    Graphics.DrawTextWithBackground(rewardText, textPos, textColor, FontAlign.Left, new Color(0, 0, 0, 220));
                }
            }
        }
        
        private void RenderStrategyAdvisor(SyndicatePanel betrayalWindow)
        {
            var drawPos = new System.Numerics.Vector2(100, 150);
            Graphics.DrawText("Strategy Advisor:", drawPos, Color.White, 18);
            drawPos.Y += 25;
            foreach (var goal in _strategicGoals)
            {
                Graphics.DrawText(goal.Item1, drawPos, goal.Item2);
                drawPos.Y += 20;
            }
        }

        private void ProcessEncounterChoices(SyndicatePanel betrayalWindow)
        {
            var eventDataElement = betrayalWindow.BetrayalEventData as BetrayalEventData;
            if (eventDataElement == null || !eventDataElement.IsVisible) return;
            var currentBoardState = ParseBoardState(betrayalWindow);
            var decision = ParseDecision(eventDataElement);
            if (decision == null) return;

            var interrogateColor = EvaluateChoice(decision, "Interrogate", currentBoardState, betrayalWindow);
            var specialColor = EvaluateChoice(decision, decision.SpecialText, currentBoardState, betrayalWindow);

            if (interrogateColor == Settings.BadChoiceColor.Value && specialColor == Settings.BadChoiceColor.Value && decision.ReleaseButton.IsVisible)
                _rectanglesToDraw.Add(new Tuple<RectangleF, Color>(decision.ReleaseButton.GetClientRectCache, Settings.GoodChoiceColor.Value));
            else {
                _rectanglesToDraw.Add(new Tuple<RectangleF, Color>(decision.InterrogateButton.GetClientRectCache, interrogateColor));
                _rectanglesToDraw.Add(new Tuple<RectangleF, Color>(decision.SpecialButton.GetClientRectCache, specialColor));
            }
        }
        
        private string GetDesiredDivisionForMember(string memberName) => (string)Settings.GetType().GetProperty(memberName)?.GetValue(Settings, null).GetType().GetProperty("Value")?.GetValue(Settings.GetType().GetProperty(memberName)?.GetValue(Settings, null), null);
        
        private Dictionary<string, SyndicateMemberState> ParseBoardState(SyndicatePanel betrayalWindow)
        {
            var boardState = new Dictionary<string, SyndicateMemberState>();
            if (betrayalWindow.SyndicateStates == null) return boardState;
            var leaders = betrayalWindow.SyndicateLeadersData.Leaders.Select(l => l.Target.Name).ToHashSet();
            foreach (var memberState in betrayalWindow.SyndicateStates)
            {
                var memberName = memberState.Target.Name;
                var rank = memberState.Rank.Name;
                var job = memberState.Job.Name;
                if (string.IsNullOrWhiteSpace(memberName)) continue;
                if (Enum.TryParse(job, out SyndicateDivision division) || job == "None")
                {
                    boardState[memberName] = new SyndicateMemberState {
                        Name = memberName, Rank = rank,
                        Division = (job == "None") ? SyndicateDivision.None : division,
                        IsLeader = leaders.Contains(memberName)
                    };
                }
            }
            return boardState;
        }

        private Color EvaluateChoice(SyndicateDecision decision, string choiceText, Dictionary<string, SyndicateMemberState> boardState, SyndicatePanel betrayalWindow)
        {
            if (choiceText.ToLower().Contains("interrogate") && GetImprisonedCount(betrayalWindow) >= 3) return Settings.BadChoiceColor.Value;
            if (choiceText.Contains("become trusted") || choiceText.Contains("become rivals")) return Settings.GoodChoiceColor.Value;
            var desiredGoal = ParseGoal(GetDesiredDivisionForMember(decision.MemberName));
            if (desiredGoal.Division == SyndicateDivision.None)
            {
                if (choiceText.Contains("Remove from Syndicate") || choiceText.ToLower().Contains("interrogate")) return Settings.GoodChoiceColor.Value;
                foreach (var targetDivision in _targetDivisions)
                    if (choiceText.Contains($"moves to {targetDivision}")) return Settings.BadChoiceColor.Value;
                return choiceText.Contains("moves to") ? Settings.GoodChoiceColor.Value : Settings.BadChoiceColor.Value;
            }
            if (!boardState.TryGetValue(decision.MemberName, out var currentState)) return Settings.NeutralChoiceColor.Value;
            int currentRank = currentState.Rank switch { "Sergeant" => 1, "Lieutenant" => 2, "Captain" => 3, _ => 0 };
            if (choiceText.ToLower().Contains("interrogate"))
            {
                if (currentRank <= 1 && currentState.Division != desiredGoal.Division) return Settings.NeutralChoiceColor.Value;
                if (currentState.Division == SyndicateDivision.None) return Settings.BadChoiceColor.Value;
                return currentState.Division != desiredGoal.Division ? Settings.GoodChoiceColor.Value : Settings.BadChoiceColor.Value;
            }
            if (choiceText.Contains("Remove from Syndicate")) return Settings.BadChoiceColor.Value;
            if (choiceText.Contains($"moves to {desiredGoal.Division}")) return Settings.GoodChoiceColor.Value;
            if (currentState.Division == desiredGoal.Division && currentRank < 3) return Settings.GoodChoiceColor.Value;
            return Settings.NeutralChoiceColor.Value;
        }
        
        private int GetImprisonedCount(SyndicatePanel betrayalWindow)
        {
            var searchRoot = betrayalWindow.GetChildFromIndices(0);
            if (searchRoot == null) return 0;
            int count = 0;
            CountTurnsLeftRecursive(searchRoot, ref count);
            return count;
        }

        private void CountTurnsLeftRecursive(Element currentElement, ref int count)
        {
            if (currentElement == null) return;
            if (currentElement.Text?.Contains("Turns Left") ?? false) count++;
            foreach (var child in currentElement.Children) CountTurnsLeftRecursive(child, ref count);
        }
        
        private void AddDebug(string message) { if (Settings.EnableDebugDrawing.Value) _debugMessages.Add(message); }
        
        private Dictionary<string, Element> FindPortraitElements(SyndicatePanel betrayalWindow)
        {
            var foundPortraits = new Dictionary<string, Element>();
            FindPortraitsRecursive(betrayalWindow, foundPortraits);
            return foundPortraits;
        }

        private void FindPortraitsRecursive(Element currentElement, Dictionary<string, Element> foundPortraits)
        {
            if (currentElement == null) return;
            var memberName = SyndicateMemberNames.FirstOrDefault(name => name == currentElement.Text);
            if (memberName != null && !foundPortraits.ContainsKey(memberName) && currentElement.Parent != null)
            {
                foundPortraits[memberName] = currentElement.Parent;
                return;
            }
            foreach (var child in currentElement.Children) FindPortraitsRecursive(child, foundPortraits);
        }

        private SyndicateDecision ParseDecision(BetrayalEventData eventDataElement)
        {
            var memberName = FindNameInChoiceDialog(eventDataElement);
            if (string.IsNullOrWhiteSpace(memberName)) return null;
            return new SyndicateDecision {
                MemberName = memberName, InterrogateButton = eventDataElement.InterrogateButton,
                SpecialButton = eventDataElement.SpecialButton, ReleaseButton = eventDataElement.ReleaseButton,
                InterrogateText = "Interrogate", SpecialText = eventDataElement.EventText ?? ""
            };
        }

        private string FindNameInChoiceDialog(Element searchRoot)
        {
            if (searchRoot == null) return null;
            var memberName = SyndicateMemberNames.FirstOrDefault(name => name == searchRoot.Text);
            if (memberName != null) return memberName;
            foreach (var child in searchRoot.Children)
            {
                var foundName = FindNameInChoiceDialog(child);
                if (foundName != null) return foundName;
            }
            return null;
        }
        
        private bool CanRun() => Settings.Enable.Value && !GameController.IsLoading && GameController.IngameState.InGame;
    }
}
