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
    public class SyndicateDecision { public string MemberName { get; set; } public Element InterrogateButton { get; set; } public Element ReleaseButton { get; set; } public Element SpecialButton { get; set; } public string InterrogateText { get; set; } public string SpecialText { get; set; } }

    public struct EvaluatedChoice
    {
        public string Name { get; set; }
        public int Score { get; set; }
        public Element Button { get; set; }
    }

    public enum GoalPriority { Critical, Major, Minor, Optimal }
    public class StrategicGoal
    {
        public string Text { get; set; }
        public GoalPriority Priority { get; set; }
        public Color DisplayColor { get; set; }
    }

    public class SyndicateHelper : BaseSettingsPlugin<SyndicateHelperSettings>
    {
        private readonly List<Tuple<RectangleF, Color>> _rectanglesToDraw = new();
        private readonly List<Tuple<RectangleF, RectangleF, Color>> _linksToDraw = new();
        private readonly List<StrategicGoal> _strategicGoals = new List<StrategicGoal>();
        private string _currentStrategy = "";
        private readonly List<string> _debugMessages = new List<string>();
        private readonly HashSet<SyndicateDivision> _targetDivisions = new HashSet<SyndicateDivision>();

        private Dictionary<string, SyndicateMemberState> _boardState = new Dictionary<string, SyndicateMemberState>();
        private int _imprisonedMemberCount = 0;
        private SyndicateStrategy _strategyEvaluator;
        
        private List<EvaluatedChoice> _lastChoices = new List<EvaluatedChoice>();
        private SyndicateDecision _lastDecision = null;

        private static readonly List<string> SyndicateMemberNames = new List<string> { "Aisling", "Cameria", "Elreon", "Gravicius", "Guff", "Haku", "Hillock", "It That Fled", "Janus", "Jorgin", "Korell", "Leo", "Rin", "Riker", "Tora", "Vagan", "Vorici" };

        public override bool Initialise()
        {
            Name = "SyndicateHelper";
            return true;
        }

        public override Job Tick()
        {
            _linksToDraw.Clear();
            _debugMessages.Clear();
            _strategicGoals.Clear();

            if (!CanRun())
            {
                _lastDecision = null;
                return null;
            }

            if (Settings.StrategyProfile.Value != "Custom" && _currentStrategy != Settings.StrategyProfile.Value)
            {
                ApplyStrategyProfile();
                _currentStrategy = Settings.StrategyProfile.Value;
            }

            var betrayalWindow = GameController.IngameState.IngameUi.BetrayalWindow as SyndicatePanel;
            if (betrayalWindow == null || !betrayalWindow.IsVisible)
            {
                _lastDecision = null;
                return null;
            }
            
            UpdateBoardAndPrisonState(betrayalWindow);
            GenerateStrategicGoals(betrayalWindow);
            _strategyEvaluator = new SyndicateStrategy(Settings, _boardState, _imprisonedMemberCount);

            var eventDataElement = betrayalWindow.BetrayalEventData as BetrayalEventData;
            _lastDecision = eventDataElement != null && eventDataElement.IsVisible ? ParseDecision(eventDataElement) : null;
            
            if (_lastDecision != null)
            {
                ProcessEncounterChoices(eventDataElement);
            }

            return null;
        }

        public override void Render()
        {
            if (!CanRun()) return;
            
            _rectanglesToDraw.Clear();
            
            var betrayalWindow = GameController.IngameState.IngameUi.BetrayalWindow as SyndicatePanel;
            if (betrayalWindow != null && betrayalWindow.IsVisible)
            {
                ProcessBoardOverlays(betrayalWindow);
                RenderStrategyAdvisor(betrayalWindow);
                RenderChoiceHighlights(); 
            }

            foreach (var rect in _rectanglesToDraw) { Graphics.DrawFrame(rect.Item1, rect.Item2, Settings.FrameThickness.Value); }
            foreach (var link in _linksToDraw)
            {
                var goalCenter = new System.Numerics.Vector2(link.Item1.Center.X, link.Item1.Center.Y);
                var buttonCenter = new System.Numerics.Vector2(link.Item2.Center.X, link.Item2.Center.Y);
                Graphics.DrawLine(goalCenter, buttonCenter, Settings.FrameThickness.Value, link.Item3);
            }
            if (Settings.EnableDebugDrawing.Value)
            {
                var y = 300f;
                var a = new System.Numerics.Vector2(100, y);
                a.Y += 20;
                Graphics.DrawText($"Prison: {_imprisonedMemberCount}/3 slots filled.", a, Color.White);
                a.Y += 20;
                foreach (var msg in _debugMessages)
                {
                    Graphics.DrawText(msg, a, Color.White);
                    a.Y += 20;
                }
            }
        }
        
        private void ProcessEncounterChoices(BetrayalEventData eventDataElement)
        {
            _lastChoices.Clear();
            if (_lastDecision == null) return;

            _lastChoices.Add(new EvaluatedChoice { Name = "Interrogate", Score = _strategyEvaluator.ScoreChoiceByCode("Interrogate", _lastDecision), Button = _lastDecision.InterrogateButton });
            
            var specialActionId = eventDataElement.Action?.Id;
            if (!string.IsNullOrWhiteSpace(specialActionId))
            {
                _lastChoices.Add(new EvaluatedChoice { Name = specialActionId, Score = _strategyEvaluator.ScoreChoiceByCode(specialActionId, _lastDecision), Button = _lastDecision.SpecialButton });
            }

            _lastChoices.Add(new EvaluatedChoice { Name = "Release", Score = 0, Button = _lastDecision.ReleaseButton });
            
            foreach(var choice in _lastChoices) { AddDebug($"Choice: {choice.Name}, Score: {choice.Score}"); }
        }
        
        private void RenderChoiceHighlights()
        {
            if (_lastDecision == null || _lastChoices.Count == 0) return;
            
            var highlightedButtonAddresses = new HashSet<long>();
            
            var drawPosY = 175f;
            foreach (var goal in _strategicGoals.OrderBy(g => g.Priority))
            {
                var fullText = $"[{goal.Priority}] {goal.Text}";
                var goalRect = new RectangleF(100, drawPosY, Graphics.MeasureText(fullText).X + 4, Graphics.MeasureText(fullText).Y);
                drawPosY += 20;

                bool specialCompletes = ChoiceAccomplishesGoal(_lastDecision.SpecialText, goal.Text, _lastDecision.MemberName, _boardState);
                bool interrogateCompletes = ChoiceAccomplishesGoal("Interrogate", goal.Text, _lastDecision.MemberName, _boardState);
                
                var buttonToHighlight = specialCompletes ? _lastDecision.SpecialButton : (interrogateCompletes ? _lastDecision.InterrogateButton : null);

                if(buttonToHighlight != null && buttonToHighlight.IsVisible)
                {
                    _rectanglesToDraw.Add(new Tuple<RectangleF, Color>(goalRect, Settings.GoalCompletionColor.Value));
                    _rectanglesToDraw.Add(new Tuple<RectangleF, Color>(buttonToHighlight.GetClientRectCache, Settings.GoalCompletionColor.Value));
                    highlightedButtonAddresses.Add(buttonToHighlight.Address);
                }
            }
            
            var bestChoice = _lastChoices.OrderByDescending(c => c.Score).First();

            foreach (var choice in _lastChoices)
            {
                var buttonRect = choice.Button?.GetClientRectCache ?? RectangleF.Empty;
                if (buttonRect == RectangleF.Empty) continue;

                var scoreColor = highlightedButtonAddresses.Contains(choice.Button.Address) ? Settings.GoalCompletionColor.Value : 
                                 choice.Score > 0 ? Settings.GoodChoiceColor.Value : 
                                 choice.Score == 0 ? Settings.NeutralChoiceColor.Value : Settings.BadChoiceColor.Value;
                
                var scoreText = $"[{choice.Score}]";
                var textPos = new System.Numerics.Vector2(buttonRect.Right + 5, buttonRect.Center.Y - Graphics.MeasureText(scoreText).Y / 2);
                Graphics.DrawText(scoreText, textPos, scoreColor);
                
                if (highlightedButtonAddresses.Contains(choice.Button.Address)) continue;

                if (choice.Score == bestChoice.Score)
                {
                    _rectanglesToDraw.Add(new Tuple<RectangleF, Color>(buttonRect, scoreColor));
                }
                else if (choice.Score < 0)
                {
                    _rectanglesToDraw.Add(new Tuple<RectangleF, Color>(buttonRect, Settings.BadChoiceColor.Value));
                }
            }
        }
        
        private void UpdateBoardAndPrisonState(SyndicatePanel betrayalWindow)
        {
            var newBoardState = new Dictionary<string, SyndicateMemberState>();
            var prisonCount = 0;
            if (betrayalWindow?.SyndicateStates == null) return;
            var leaders = betrayalWindow.SyndicateLeadersData.Leaders.Select(l => l.Target.Name).ToHashSet();
            foreach (var memberState in betrayalWindow.SyndicateStates)
            {
                var memberName = memberState.Target.Name;
                if (string.IsNullOrWhiteSpace(memberName)) continue;
                var rankName = memberState.Rank.Name;
                if (Enum.TryParse(memberState.Job.Name, out SyndicateDivision division) || memberState.Job.Name == "None" || !string.IsNullOrWhiteSpace(rankName))
                {
                    newBoardState[memberName] = new SyndicateMemberState { Name = memberName, Rank = rankName, Division = division, IsLeader = leaders.Contains(memberName) };
                    if (IsMemberImprisoned(memberState.UIElement)) prisonCount++;
                }
            }
            _boardState = newBoardState;
            _imprisonedMemberCount = prisonCount;
        }

        private bool IsMemberImprisoned(Element element)
        {
            if (element == null || !element.IsVisible) return false;
            if ((element.Text?.Contains("Turn Left") ?? false) || (element.Text?.Contains("Turns Left") ?? false)) return true;
            foreach (var child in element.Children) { if (IsMemberImprisoned(child)) return true; }
            return false;
        }
        
        private void RenderStrategyAdvisor(SyndicatePanel betrayalWindow)
        {
            var drawPos = new System.Numerics.Vector2(100, 150);
            Graphics.DrawText("Strategy Advisor:", drawPos, Color.White, 18);
            drawPos.Y += 25;
            
            foreach (var goal in _strategicGoals.OrderBy(g => g.Priority))
            {
                string prefix = $"[{goal.Priority}] ";
                string fullText = prefix + goal.Text;
                
                if (_lastDecision != null)
                {
                    bool specialCompletes = ChoiceAccomplishesGoal(_lastDecision.SpecialText, goal.Text, _lastDecision.MemberName, _boardState);
                    bool interrogateCompletes = ChoiceAccomplishesGoal("Interrogate", goal.Text, _lastDecision.MemberName, _boardState);
                    var goalRect = new RectangleF(drawPos.X, drawPos.Y, Graphics.MeasureText(fullText).X + 4, Graphics.MeasureText(fullText).Y);

                    var buttonToLink = specialCompletes ? _lastDecision.SpecialButton : (interrogateCompletes ? _lastDecision.InterrogateButton : null);
                    if (buttonToLink != null)
                    {
                         _linksToDraw.Add(new Tuple<RectangleF, RectangleF, Color>(goalRect, buttonToLink.GetClientRectCache, Settings.GoalCompletionColor));
                    }
                }
                Graphics.DrawText(fullText, new System.Numerics.Vector2(drawPos.X + 2, drawPos.Y), goal.DisplayColor);
                drawPos.Y += 20;
            }
        }
        
        private MemberGoal ParseGoal(string goal)
        {
            if (string.IsNullOrEmpty(goal) || goal == "None") return new MemberGoal { Division = SyndicateDivision.None, IsPrimaryLeader = false };
            var isLeader = goal.Contains("(Leader)");
            var divisionName = goal.Replace(" (Leader)", "").Trim();
            if (Enum.TryParse(divisionName, out SyndicateDivision division)) return new MemberGoal { Division = division, IsPrimaryLeader = isLeader };
            return new MemberGoal { Division = SyndicateDivision.None, IsPrimaryLeader = false };
        }

        private void GenerateStrategicGoals(SyndicatePanel betrayalWindow)
        {
            if (_boardState.Count == 0) return;

            var allGoals = SyndicateMemberNames.Select(name => new { Name = name, GoalString = GetDesiredDivisionForMember(name) }).ToDictionary(g => g.Name, g => ParseGoal(g.GoalString));
            
            var allDivisions = Enum.GetValues(typeof(SyndicateDivision)).Cast<SyndicateDivision>().Where(d => d != SyndicateDivision.None);
            var leadersByDivision = _boardState.Values.Where(m => m.IsLeader).ToDictionary(m => m.Division, m => m);

            foreach (var division in allDivisions)
            {
                if (!leadersByDivision.ContainsKey(division))
                {
                     _strategicGoals.Add(new StrategicGoal { Text = $"Establish a leader for {division}", Priority = GoalPriority.Major, DisplayColor = Color.Yellow });
                }
            }

            var targetDivisionsWithGoals = allGoals.Values.Where(g => g.Division != SyndicateDivision.None).Select(g => g.Division).Distinct();
            foreach (var division in targetDivisionsWithGoals)
            {
                var primaryLeaderGoal = allGoals.FirstOrDefault(g => g.Value.Division == division && g.Value.IsPrimaryLeader);
                var currentLeader = leadersByDivision.ContainsKey(division) ? leadersByDivision[division] : null;

                if (primaryLeaderGoal.Key != null)
                {
                    if (currentLeader?.Name == primaryLeaderGoal.Key)
                    {
                        _strategicGoals.Add(new StrategicGoal { Text = $"{primaryLeaderGoal.Key} is leading {division}.", Priority = GoalPriority.Optimal, DisplayColor = Color.LimeGreen });
                    }
                    else
                    {
                        if (currentLeader != null)
                        {
                            _strategicGoals.Add(new StrategicGoal { Text = $"Problem: {currentLeader.Name} is blocking {primaryLeaderGoal.Key} from leading {division}.", Priority = GoalPriority.Critical, DisplayColor = Color.OrangeRed });
                        }
                        if (_boardState.TryGetValue(primaryLeaderGoal.Key, out var primaryState))
                        {
                             if (primaryState.Division != division)
                                _strategicGoals.Add(new StrategicGoal { Text = $"Move {primaryLeaderGoal.Key} to {division} to become leader", Priority = GoalPriority.Major, DisplayColor = Color.Yellow });
                             else
                                _strategicGoals.Add(new StrategicGoal { Text = $"Rank up {primaryLeaderGoal.Key} to become leader of {division}", Priority = GoalPriority.Major, DisplayColor = Color.LightGreen });
                        }
                        else
                        {
                            _strategicGoals.Add(new StrategicGoal { Text = $"Place {primaryLeaderGoal.Key} in {division} to become leader", Priority = GoalPriority.Major, DisplayColor = Color.Yellow });
                        }
                    }
                }
                var subordinateGoals = allGoals.Where(g => g.Value.Division == division && !g.Value.IsPrimaryLeader);
                foreach (var subGoal in subordinateGoals)
                {
                    if (_boardState.TryGetValue(subGoal.Key, out var subState))
                    {
                        if (subState.Division != division)
                            _strategicGoals.Add(new StrategicGoal { Text = $"Move {subGoal.Key} to {division}", Priority = GoalPriority.Minor, DisplayColor = Color.CornflowerBlue });
                        else if (subState.Rank != "Captain" && !(primaryLeaderGoal.Key != null && currentLeader?.Name == primaryLeaderGoal.Key))
                            _strategicGoals.Add(new StrategicGoal { Text = $"Rank up {subGoal.Key} in {division}", Priority = GoalPriority.Minor, DisplayColor = Color.CornflowerBlue });
                    }
                    else
                    {
                        _strategicGoals.Add(new StrategicGoal { Text = $"Place {subGoal.Key} in {division}", Priority = GoalPriority.Minor, DisplayColor = Color.CornflowerBlue });
                    }
                }
            }
            if (_strategicGoals.Count == 0) _strategicGoals.Add(new StrategicGoal { Text = "No strategy configured or board is optimal.", Priority = GoalPriority.Optimal, DisplayColor = Color.White });
        }
        
        private void ApplyStrategyProfile()
        {
            var allMemberNodes = SyndicateMemberNames.ToDictionary(name => name, name => Settings.GetType().GetProperty(name)?.GetValue(Settings) as ListNode);
            
            foreach (var node in allMemberNodes.Values.Where(n => n != null)) node.Value = "None";

            switch (Settings.StrategyProfile.Value)
            {
                case "Comprehensive Scarab Farm":
                    SetGoalsByRewardKeyword("Scarab", SyndicateDivision.Intervention, "Cameria");
                    break;
                case "Crafting Meta (Research)":
                    if(allMemberNodes.TryGetValue("Aisling", out var aislingNode) && aislingNode != null) aislingNode.Value = "Research (Leader)";
                    if(allMemberNodes.TryGetValue("Vorici", out var voriciNode) && voriciNode != null) voriciNode.Value = "Research";
                    if(allMemberNodes.TryGetValue("Hillock", out var hillockNode) && hillockNode != null) hillockNode.Value = "Fortification (Leader)";
                    break;
            }

            _targetDivisions.Clear();
            foreach(var memberName in SyndicateMemberNames)
            {
                var goal = ParseGoal(GetDesiredDivisionForMember(memberName));
                if (goal.Division != SyndicateDivision.None) _targetDivisions.Add(goal.Division);
            }
        }

        private void SetGoalsByRewardKeyword(string keyword, SyndicateDivision primaryDivision, string desiredLeaderName)
        {
            var allMemberNodes = SyndicateMemberNames.ToDictionary(name => name, name => Settings.GetType().GetProperty(name)?.GetValue(Settings) as ListNode);
            foreach (var memberRewardsPair in SyndicateRewardData.Rewards)
            {
                var memberName = memberRewardsPair.Key;
                foreach (var divisionRewardPair in memberRewardsPair.Value)
                {
                    var division = divisionRewardPair.Key;
                    var rewardInfo = divisionRewardPair.Value;
                    if (rewardInfo.Text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        if (allMemberNodes.TryGetValue(memberName, out var node) && node != null)
                        {
                            if (memberName == desiredLeaderName && division == primaryDivision) node.Value = $"{division} (Leader)";
                            else if (node.Value == "None") node.Value = division.ToString();
                        }
                    }
                }
            }
        }

        private void ProcessBoardOverlays(SyndicatePanel betrayalWindow)
        {
            if (betrayalWindow.SyndicateStates == null) return;
            var portraitElements = FindPortraitElements(betrayalWindow);
            foreach (var memberState in _boardState.Values)
            {
                if (portraitElements.TryGetValue(memberState.Name, out var portraitElement) && SyndicateRewardData.Rewards.TryGetValue(memberState.Name, out var memberRewards) && memberRewards.TryGetValue(memberState.Division, out var rewardInfo))
                {
                    var desiredGoal = ParseGoal(GetDesiredDivisionForMember(memberState.Name));
                    var desiredDivision = desiredGoal.Division.ToString();
                    var rewardText = rewardInfo.Text;
                    bool isTargetReward = memberState.Division.ToString() == desiredDivision && desiredDivision != "None";
                    var textColor = isTargetReward ? Settings.GoodChoiceColor.Value : rewardInfo.Tier switch
                    {
                        RewardTier.Great => Color.LimeGreen, RewardTier.Good => Color.Yellow,
                        RewardTier.Average => Color.White, RewardTier.Worst => new Color(255, 80, 80),
                        _ => Color.White
                    };
                    if (rewardInfo.Tier == RewardTier.Worst && desiredDivision != "None" && desiredDivision != memberState.Division.ToString()) rewardText += $" (-> {desiredDivision})";
                    var rect = portraitElement.GetClientRectCache;
                    var textSize = Graphics.MeasureText(rewardText);
                    var textPos = new System.Numerics.Vector2(rect.Center.X - textSize.X / 2, rect.Bottom + 2);
                    Graphics.DrawTextWithBackground(rewardText, textPos, textColor, FontAlign.Left, new Color(0, 0, 0, 220));
                }
            }
        }
        
        private bool ChoiceAccomplishesGoal(string choiceText, string goalText, string memberInDecision, Dictionary<string, SyndicateMemberState> boardState)
        {
            if (string.IsNullOrEmpty(choiceText) || string.IsNullOrEmpty(goalText)) return false;
            if (goalText.Contains("Rank up"))
            {
                var targetMember = goalText.Split(' ')[2];
                return memberInDecision == targetMember && choiceText.IndexOf("rank", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            if (goalText.StartsWith("Problem:"))
            {
                var targetMember = goalText.Split(' ')[1];
                return memberInDecision == targetMember && choiceText.Equals("Interrogate", StringComparison.OrdinalIgnoreCase);
            }
             if (goalText.Contains("Establish a leader"))
            {
                var divisionName = goalText.Split(' ')[4];
                return choiceText.Contains("rank", StringComparison.OrdinalIgnoreCase) && boardState.TryGetValue(memberInDecision, out var state) && state.Division.ToString() == divisionName;
            }
            if (goalText.StartsWith("Move") || goalText.StartsWith("Place"))
            {
                var match = Regex.Match(goalText, @"(Move|Place) (.+?) to (.+?)( |to|$)");
                if (match.Success)
                {
                    var targetMember = match.Groups[2].Value.Trim();
                    var desiredDivision = match.Groups[3].Value.Trim();
                    return choiceText.Contains(targetMember, StringComparison.OrdinalIgnoreCase) && choiceText.Contains($"moves to {desiredDivision}", StringComparison.OrdinalIgnoreCase);
                }
            }
            return false;
        }

        private string GetDesiredDivisionForMember(string memberName)
        {
            var property = Settings.GetType().GetProperty(memberName);
            if (property == null) return "None";
            var listNode = property.GetValue(Settings) as ListNode;
            return listNode?.Value ?? "None";
        }
        
        private void AddDebug(string message) { if (Settings.EnableDebugDrawing.Value) _debugMessages.Add(message); }
        
        private Dictionary<string, Element> FindPortraitElements(SyndicatePanel betrayalWindow)
        {
            var foundPortraits = new Dictionary<string, Element>();
            if(betrayalWindow != null) FindPortraitsRecursive(betrayalWindow, foundPortraits);
            return foundPortraits;
        }

        private void FindPortraitsRecursive(Element currentElement, Dictionary<string, Element> foundPortraits)
        {
            if (currentElement == null) return;
            var memberName = SyndicateMemberNames.FirstOrDefault(name => currentElement.Text != null && currentElement.Text.Equals(name, StringComparison.Ordinal));
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
            return new SyndicateDecision { MemberName = memberName, InterrogateButton = eventDataElement.InterrogateButton, SpecialButton = eventDataElement.SpecialButton, ReleaseButton = eventDataElement.ReleaseButton, InterrogateText = "Interrogate", SpecialText = eventDataElement.EventText ?? "" };
        }

        private string FindNameInChoiceDialog(Element searchRoot)
        {
            if (searchRoot == null) return null;
            if (searchRoot.Text != null)
            {
                var memberName = SyndicateMemberNames.FirstOrDefault(name => searchRoot.Text.Contains(name));
                if (memberName != null) return memberName;
            }
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