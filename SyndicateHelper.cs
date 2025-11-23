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
using System.Windows.Forms;

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
        private class CachedText
        {
            public string Text { get; set; }
            public System.Numerics.Vector2 Size { get; set; }
            public System.Numerics.Vector2 Position { get; set; }
            public Color Color { get; set; }
        }

        private readonly List<Tuple<RectangleF, Color>> _rectanglesToDraw = new();
        private readonly List<Tuple<RectangleF, RectangleF, Color>> _linksToDraw = new();
        private readonly List<StrategicGoal> _strategicGoals = new List<StrategicGoal>();
        private readonly List<string> _debugMessages = new List<string>();
        private readonly HashSet<SyndicateDivision> _targetDivisions = new HashSet<SyndicateDivision>();

        private Dictionary<string, SyndicateMemberState> _boardState = new Dictionary<string, SyndicateMemberState>();
        private int _imprisonedMemberCount = 0;
        private SyndicateStrategy _strategyEvaluator;
        
        private List<EvaluatedChoice> _lastChoices = new List<EvaluatedChoice>();
        private SyndicateDecision _lastDecision = null;

        private readonly List<CachedText> _cachedChoiceScores = new List<CachedText>();
        private readonly List<CachedText> _cachedRewardText = new List<CachedText>();
        private readonly List<RectangleF> _goalRects = new List<RectangleF>();

        private RectangleF _leftButtonRect;
        private RectangleF _rightButtonRect;
        private DateTime _lastClickTime = DateTime.MinValue;
        private bool _isBoardStateDirty = true;

        private static readonly List<string> SyndicateMemberNames = new List<string> { "Aisling", "Cameria", "Elreon", "Gravicius", "Guff", "Haku", "Hillock", "It That Fled", "Janus", "Jorgin", "Korell", "Leo", "Rin", "Riker", "Tora", "Vagan", "Vorici" };

        private void CycleStrategy(int direction)
        {
            var strategyNames = SyndicateStrategies.Strategies.Select(s => s.Name).ToList();
            if (strategyNames.Count == 0) return;

            var currentStrategyName = Settings.StrategyProfile.Value;
            var currentIndex = strategyNames.IndexOf(currentStrategyName);

            if (currentIndex == -1)
            {
                Settings.StrategyProfile.Value = strategyNames[0];
                _isBoardStateDirty = true;
                return;
            }

            var newIndex = (currentIndex + direction + strategyNames.Count) % strategyNames.Count;
            Settings.StrategyProfile.Value = strategyNames[newIndex];
            _isBoardStateDirty = true;
        }

        public override bool Initialise()
        {
            Name = "SyndicateHelper";
            return true;
        }

        public override Job Tick()
        {
            if (!CanRun()) {
                _lastDecision = null;
                return null;
            }

            var betrayalWindow = GameController.IngameState.IngameUi.BetrayalWindow as SyndicatePanel;
            if (betrayalWindow == null || !betrayalWindow.IsVisible)
            {
                _lastDecision = null;
                _isBoardStateDirty = true;
                return null;
            }


            _linksToDraw.Clear();
            _debugMessages.Clear();
            _rectanglesToDraw.Clear();
            _cachedChoiceScores.Clear();
            _cachedRewardText.Clear();
            _goalRects.Clear();

            if (_isBoardStateDirty)
            {
                UpdateBoardAndPrisonState(betrayalWindow);
                var currentStrategy = SyndicateStrategies.Strategies.FirstOrDefault(s => s.Name == Settings.StrategyProfile.Value);
                _strategyEvaluator = new SyndicateStrategy(Settings, _boardState, _imprisonedMemberCount, currentStrategy);

                _strategicGoals.Clear();
                GenerateStrategicGoals(betrayalWindow);
                _isBoardStateDirty = false;
            }

            var eventDataElement = betrayalWindow.BetrayalEventData as BetrayalEventData;
            _lastDecision = eventDataElement != null && eventDataElement.IsVisible ? ParseDecision(eventDataElement) : null;
            
            if (_lastDecision != null)
            {
                ProcessEncounterChoices(eventDataElement);
            }

            ProcessBoardOverlays(betrayalWindow);


            if (Input.IsKeyDown(Keys.LButton) && (DateTime.Now - _lastClickTime).TotalMilliseconds > 200)
            {
                var mousePos = new SharpDX.Vector2(GameController.IngameState.MousePosX, GameController.IngameState.MousePosY);
                if (_leftButtonRect.Contains(mousePos))
                {
                    CycleStrategy(-1);
                    _lastClickTime = DateTime.Now;
                }
                else if (_rightButtonRect.Contains(mousePos))
                {
                    CycleStrategy(1);
                    _lastClickTime = DateTime.Now;
                }
            }

            return null;
        }

        public override void Render()
        {
            if (!CanRun()) return;

            var betrayalWindow = GameController.IngameState.IngameUi.BetrayalWindow as SyndicatePanel;
            if (betrayalWindow == null || !betrayalWindow.IsVisible) return;

            var backgroundColor = new Color((byte)0, (byte)0, (byte)0, (byte)Settings.BackgroundAlpha.Value);
            var advisorBottomY = RenderStrategyAdvisor(betrayalWindow, backgroundColor);

            if (_lastDecision != null)
            {
                ProcessChoiceHighlights();
            }

            foreach (var rect in _rectanglesToDraw) { Graphics.DrawFrame(rect.Item1, rect.Item2, Settings.FrameThickness.Value); }
            foreach (var link in _linksToDraw)
            {
                var goalCenter = new System.Numerics.Vector2(link.Item1.Center.X, link.Item1.Center.Y);
                var buttonCenter = new System.Numerics.Vector2(link.Item2.Center.X, link.Item2.Center.Y);
                Graphics.DrawLine(goalCenter, buttonCenter, Settings.FrameThickness.Value, link.Item3);
            }

            foreach (var cachedText in _cachedRewardText)
            {
                Graphics.DrawTextWithBackground(cachedText.Text, cachedText.Position, cachedText.Color, FontAlign.Left, backgroundColor);
            }

            foreach (var cachedText in _cachedChoiceScores)
            {
                Graphics.DrawTextWithBackground(cachedText.Text, cachedText.Position, cachedText.Color, FontAlign.Left, backgroundColor);
            }

            if (Settings.EnableDebugDrawing.Value)
            {
                var y = advisorBottomY + 20;
                var a = new System.Numerics.Vector2(100, y);
                Graphics.DrawTextWithBackground($"Prison: {_imprisonedMemberCount}/3 slots filled.", a, Color.White, FontAlign.Left, backgroundColor);
                a.Y += 20;
                foreach (var msg in _debugMessages)
                {
                    Graphics.DrawTextWithBackground(msg, a, Color.White, FontAlign.Left, backgroundColor);
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
        
        private void ProcessChoiceHighlights()
        {
            if (_lastDecision == null || _lastChoices.Count == 0) return;

            var highlightedButtonAddresses = new HashSet<long>();

            var sortedGoals = _strategicGoals.OrderBy(g => g.Priority).ToList();
            for (int i = 0; i < sortedGoals.Count && i < _goalRects.Count; i++)
            {
                var goal = sortedGoals[i];
                var goalRect = _goalRects[i];

                bool specialCompletes = ChoiceAccomplishesGoal(_lastDecision.SpecialText, goal.Text, _lastDecision.MemberName, _boardState);
                bool interrogateCompletes = ChoiceAccomplishesGoal("Interrogate", goal.Text, _lastDecision.MemberName, _boardState);

                var buttonToHighlight = specialCompletes ? _lastDecision.SpecialButton : (interrogateCompletes ? _lastDecision.InterrogateButton : null);

                if (buttonToHighlight != null && buttonToHighlight.IsVisible)
                {
                    _rectanglesToDraw.Add(new Tuple<RectangleF, Color>(goalRect, Settings.GoalCompletionColor.Value));
                    _rectanglesToDraw.Add(new Tuple<RectangleF, Color>(buttonToHighlight.GetClientRectCache, Settings.GoalCompletionColor.Value));
                    highlightedButtonAddresses.Add(buttonToHighlight.Address);
                }
            }

            var bestChoice = _lastChoices.OrderByDescending(c => c.Score).FirstOrDefault();
            if (bestChoice.Button == null) return;

            foreach (var choice in _lastChoices)
            {
                var buttonRect = choice.Button?.GetClientRectCache ?? RectangleF.Empty;
                if (buttonRect == RectangleF.Empty) continue;

                var scoreText = $"[{choice.Score}]";
                var textSize = Graphics.MeasureText(scoreText);
                var textPos = new System.Numerics.Vector2(buttonRect.Right + 5, buttonRect.Center.Y - textSize.Y / 2 - 5);

                // Skip recoloring if this button is already highlighted for goal completion
                if (highlightedButtonAddresses.Contains(choice.Button.Address))
                {
                    _cachedChoiceScores.Add(new CachedText { Text = scoreText, Size = textSize, Position = textPos, Color = Settings.GoalCompletionColor.Value });
                    continue;
                }

                var scoreColor = choice.Score > 0 ? Settings.GoodChoiceColor.Value :
                                 choice.Score == 0 ? Settings.NeutralChoiceColor.Value : Settings.BadChoiceColor.Value;

                _cachedChoiceScores.Add(new CachedText { Text = scoreText, Size = textSize, Position = textPos, Color = scoreColor });

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
            var leaders = betrayalWindow.SyndicateLeadersData.Leaders.Where(l => l.Target != null).Select(l => l.Target.Name).ToHashSet();
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


            var relationshipElements = FindRelationshipElements(betrayalWindow);
            foreach (var relElement in relationshipElements)
            {
                var text = relElement.Text;
                var match = Regex.Match(text, "(.+?) (is friends with|is rivals with) (.+)");
                if (match.Success)
                {
                    var member1Name = match.Groups[1].Value.Trim();
                    var relationshipType = match.Groups[2].Value.Trim();
                    var member2Name = match.Groups[3].Value.Trim();

                    if (newBoardState.TryGetValue(member1Name, out var member1State) && newBoardState.TryGetValue(member2Name, out var member2State))
                    {
                        if (relationshipType == "is friends with")
                        {
                            member1State.Friends.Add(member2Name);
                            member2State.Friends.Add(member1Name);
                        }
                        else if (relationshipType == "is rivals with")
                        {
                            member1State.Rivals.Add(member2Name);
                            member2State.Rivals.Add(member1Name);
                        }
                    }
                }
            }
        }

        private bool IsMemberImprisoned(Element element)
        {
            if (element == null || !element.IsVisible) return false;
            if ((element.Text?.Contains("Turn Left") ?? false) || (element.Text?.Contains("Turns Left") ?? false)) return true;
            foreach (var child in element.Children) { if (IsMemberImprisoned(child)) return true; }
            return false;
        }
        
        private float RenderStrategyAdvisor(SyndicatePanel betrayalWindow, Color backgroundColor)
        {
            var drawPos = new System.Numerics.Vector2(100, 100);


            var leftButtonText = "<-";
            var leftButtonSize = Graphics.MeasureText(leftButtonText, 20);
            _leftButtonRect = new RectangleF(drawPos.X, drawPos.Y, leftButtonSize.X + 10, leftButtonSize.Y + 5);
            Graphics.DrawBox(_leftButtonRect, new Color(0, 0, 0, 150));
            Graphics.DrawTextWithBackground(leftButtonText, new System.Numerics.Vector2(drawPos.X + 5, drawPos.Y), Color.White, FontAlign.Left, backgroundColor);


            var strategyName = Settings.StrategyProfile.Value;
            var strategyNameSize = Graphics.MeasureText(strategyName, 20);
            var strategyNamePos = new System.Numerics.Vector2(_leftButtonRect.Right + 10, drawPos.Y);
            Graphics.DrawTextWithBackground(strategyName, strategyNamePos, Color.Orange, FontAlign.Left, backgroundColor);


            var rightButtonText = "->";
            var rightButtonSize = Graphics.MeasureText(rightButtonText, 20);
            _rightButtonRect = new RectangleF(strategyNamePos.X + strategyNameSize.X + 10, drawPos.Y, rightButtonSize.X + 10, rightButtonSize.Y + 5);
            Graphics.DrawBox(_rightButtonRect, new Color(0, 0, 0, 150));
            Graphics.DrawTextWithBackground(rightButtonText, new System.Numerics.Vector2(_rightButtonRect.X + 5, drawPos.Y), Color.White, FontAlign.Left, backgroundColor);

            drawPos.Y += Math.Max(leftButtonSize.Y, strategyNameSize.Y) + 10;

            Graphics.DrawTextWithBackground("Strategy Advisor:", drawPos, Color.White, FontAlign.Left, backgroundColor);
            drawPos.Y += 25;
            
            foreach (var goal in _strategicGoals.OrderBy(g => g.Priority))
            {
                string prefix = $"[{goal.Priority}] ";
                string fullText = prefix + goal.Text;
                var goalRect = new RectangleF(drawPos.X, drawPos.Y, Graphics.MeasureText(fullText).X + 4, Graphics.MeasureText(fullText).Y);
                _goalRects.Add(goalRect);
                
                if (_lastDecision != null)
                {
                    bool specialCompletes = ChoiceAccomplishesGoal(_lastDecision.SpecialText, goal.Text, _lastDecision.MemberName, _boardState);
                    bool interrogateCompletes = ChoiceAccomplishesGoal("Interrogate", goal.Text, _lastDecision.MemberName, _boardState);

                    var buttonToLink = specialCompletes ? _lastDecision.SpecialButton : (interrogateCompletes ? _lastDecision.InterrogateButton : null);
                    if (buttonToLink != null)
                    {
                         _linksToDraw.Add(new Tuple<RectangleF, RectangleF, Color>(goalRect, buttonToLink.GetClientRectCache, Settings.GoalCompletionColor));
                    }
                }
                Graphics.DrawTextWithBackground(fullText, new System.Numerics.Vector2(drawPos.X + 2, drawPos.Y), goal.DisplayColor, FontAlign.Left, backgroundColor);
                drawPos.Y += 20;
            }
            return drawPos.Y;
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
                    _cachedRewardText.Add(new CachedText { Text = rewardText, Position = textPos, Color = textColor });
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

        private List<Element> FindRelationshipElements(SyndicatePanel betrayalWindow)
        {
            var relationshipElements = new List<Element>();
            if (betrayalWindow != null) FindRelationshipElementsRecursive(betrayalWindow, relationshipElements);
            return relationshipElements;
        }

        private void FindRelationshipElementsRecursive(Element currentElement, List<Element> relationshipElements)
        {
            if (currentElement == null) return;

            if (currentElement.Text != null && (currentElement.Text.Contains("is friends with") || currentElement.Text.Contains("is rivals with")))
            {
                relationshipElements.Add(currentElement);
            }

            foreach (var child in currentElement.Children)
            {
                FindRelationshipElementsRecursive(child, relationshipElements);
            }
        }
    }
}