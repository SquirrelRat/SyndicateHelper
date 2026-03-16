// SyndicateHelper.cs
// Main plugin class for the SyndicateHelper ExileAPI plugin.
// Provides UI overlays, decision scoring, and strategy guidance for the Betrayal/Syndicate mechanic in Path of Exile.

using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Numerics;
using System.Windows.Forms;
using Vector2 = System.Numerics.Vector2;

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

        private static readonly HashSet<string> SyndicateMemberNames = new HashSet<string>
        {
            "Aisling", "Cameria", "Elreon", "Gravicius", "Guff", "Haku", "Hillock",
            "It That Fled", "Janus", "Jorgin", "Korell", "Leo", "Rin", "Riker",
            "Tora", "Vagan", "Vorici"
        };

        private Dictionary<string, Element> _cachedPortraitElements = new Dictionary<string, Element>();
        private List<Element> _cachedRelationshipElements = new List<Element>();
        private bool _uiElementsDirty = true;

        private int _selectedSettingsTab = 0;

        private void CycleStrategy(int direction)
        {
            var strategyNames = SyndicateStrategies.Strategies.Select(s => s.Name).ToList();
            if (strategyNames.Count == 0) return;

            var currentStrategyName = Settings.StrategyProfile.Value;
            var currentIndex = strategyNames.IndexOf(currentStrategyName);

            if (currentIndex == -1)
            {
                Settings.StrategyProfile.Value = strategyNames[0];
                Settings.ApplyStrategyGoals(Settings.StrategyProfile.Value);
                _isBoardStateDirty = true;
                return;
            }

            var newIndex = (currentIndex + direction + strategyNames.Count) % strategyNames.Count;
            Settings.StrategyProfile.Value = strategyNames[newIndex];
            Settings.ApplyStrategyGoals(Settings.StrategyProfile.Value);
            _isBoardStateDirty = true;
        }

        public override bool Initialise()
        {
            Name = "SyndicateHelper";
            return true;
        }

        public override void OnUnload()
        {
            if (Settings?.StrategyProfile != null)
            {
                Settings.StrategyProfile.OnValueSelected -= Settings_ApplyStrategyGoals;
            }
            base.OnUnload();
        }

        public override void DrawSettings()
        {
            string[] settingTabs =
            {
                "Visual Style",
                "Syndicate Strategies",
                "UI Settings"
            };

            if (ImGui.BeginChild("LeftSidebar", new Vector2(150, ImGui.GetContentRegionAvail().Y), ImGuiChildFlags.Border, ImGuiWindowFlags.None))
            {
                for (var i = 0; i < settingTabs.Length; i++)
                {
                    if (ImGui.Selectable(settingTabs[i], _selectedSettingsTab == i))
                    {
                        _selectedSettingsTab = i;
                    }
                }
            }
            ImGui.EndChild();

            ImGui.SameLine();

            ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 5.0f);
            var contentRegionArea = ImGui.GetContentRegionAvail();
            if (ImGui.BeginChild("RightPanel", contentRegionArea, ImGuiChildFlags.Border, ImGuiWindowFlags.None))
            {
                switch (settingTabs[_selectedSettingsTab])
                {
                    case "Visual Style":
                        DrawVisualStyleTab();
                        break;
                    case "Syndicate Strategies":
                        DrawSyndicateStrategiesTab();
                        break;
                    case "UI Settings":
                        DrawUISettingsTab();
                        break;
                }
            }
            ImGui.PopStyleVar();
            ImGui.EndChild();
        }

        private void DrawVisualStyleTab()
        {
            ImGui.Text("Background Alpha");
            var bgAlpha = Settings.BackgroundAlpha.Value;
            if (ImGui.SliderInt("##BackgroundAlpha", ref bgAlpha, Settings.BackgroundAlpha.Min, Settings.BackgroundAlpha.Max))
            {
                Settings.BackgroundAlpha.Value = bgAlpha;
            }

            ImGui.Text("Frame Thickness");
            var frameThickness = Settings.FrameThickness.Value;
            if (ImGui.SliderInt("##FrameThickness", ref frameThickness, Settings.FrameThickness.Min, Settings.FrameThickness.Max))
            {
                Settings.FrameThickness.Value = frameThickness;
            }

            ImGui.Separator();

            ImGui.Text("Good Choice");
            var goodColor = Settings.GoodChoiceColor.Value.ToImguiVec4();
            if (ImGui.ColorEdit4("##GoodChoice", ref goodColor))
            {
                Settings.GoodChoiceColor.Value = new SharpDX.Color((byte)(goodColor.X * 255), (byte)(goodColor.Y * 255), (byte)(goodColor.Z * 255), (byte)(goodColor.W * 255));
            }

            ImGui.Text("Goal Completion");
            var goalColor = Settings.GoalCompletionColor.Value.ToImguiVec4();
            if (ImGui.ColorEdit4("##GoalCompletion", ref goalColor))
            {
                Settings.GoalCompletionColor.Value = new SharpDX.Color((byte)(goalColor.X * 255), (byte)(goalColor.Y * 255), (byte)(goalColor.Z * 255), (byte)(goalColor.W * 255));
            }

            ImGui.Text("Neutral Choice");
            var neutralColor = Settings.NeutralChoiceColor.Value.ToImguiVec4();
            if (ImGui.ColorEdit4("##NeutralChoice", ref neutralColor))
            {
                Settings.NeutralChoiceColor.Value = new SharpDX.Color((byte)(neutralColor.X * 255), (byte)(neutralColor.Y * 255), (byte)(neutralColor.Z * 255), (byte)(neutralColor.W * 255));
            }

            ImGui.Text("Bad Choice");
            var badColor = Settings.BadChoiceColor.Value.ToImguiVec4();
            if (ImGui.ColorEdit4("##BadChoice", ref badColor))
            {
                Settings.BadChoiceColor.Value = new SharpDX.Color((byte)(badColor.X * 255), (byte)(badColor.Y * 255), (byte)(badColor.Z * 255), (byte)(badColor.W * 255));
            }
        }

        private void DrawSyndicateStrategiesTab()
        {
            ImGui.Text("Strategy Profile");
            var newProfile = ImGuiExtension.ComboBox("##Profile", Settings.StrategyProfile.Value,
                Settings.StrategyProfile.Values, out var profileSelected, ImGuiComboFlags.HeightLarge);
            if (profileSelected)
            {
                Settings.StrategyProfile.Value = newProfile;
                Settings.ApplyStrategyGoals(Settings.StrategyProfile.Value);
                _isBoardStateDirty = true;
            }

            ImGui.Separator();

            if (Settings.StrategyProfile.Value == "Relationship-Based")
            {
                DrawRelationshipSettings();
                ImGui.Separator();
            }

            DrawMemberGoalsTab();
        }

        private void DrawRelationshipSettings()
        {
            ImGui.Text("Relationship Configuration");
            ImGui.Separator();

            ImGui.Text("Opposed Divisions");
            var opposedDivisions = Settings.OpposedDivisions.Value;
            if (ImGui.InputText("##OpposedDivisions", ref opposedDivisions, 256))
            {
                Settings.OpposedDivisions.Value = opposedDivisions;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Comma-separated pairs of divisions that should NOT have relationships\n(e.g., 'Transportation-Research,Fortification-Intervention')");
            }

            ImGui.Text("Allied Divisions");
            var alliedDivisions = Settings.AlliedDivisions.Value;
            if (ImGui.InputText("##AlliedDivisions", ref alliedDivisions, 256))
            {
                Settings.AlliedDivisions.Value = alliedDivisions;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Comma-separated pairs of divisions that SHOULD have relationships\n(e.g., 'Fortification-Transportation,Intervention-Research')");
            }

            ImGui.Text("Relationship Score Modifier");
            var relationshipModifier = Settings.RelationshipScoreModifier.Value;
            if (ImGui.SliderInt("##RelationshipModifier", ref relationshipModifier, 
                Settings.RelationshipScoreModifier.Min, Settings.RelationshipScoreModifier.Max))
            {
                Settings.RelationshipScoreModifier.Value = relationshipModifier;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Score multiplier for choices that affect relationships (0-100%)");
            }
        }

        private void DrawUISettingsTab()
        {
            ImGui.Text("Show Goal Info");
            var showGoalInfo = Settings.ShowGoalInfo.Value;
            if (ImGui.Checkbox("##ShowGoalInfo", ref showGoalInfo))
            {
                Settings.ShowGoalInfo.Value = showGoalInfo;
            }

            ImGui.Text("Show Action Buttons");
            var showButtons = Settings.ShowButtons.Value;
            if (ImGui.Checkbox("##ShowButtons", ref showButtons))
            {
                Settings.ShowButtons.Value = showButtons;
            }

            ImGui.Text("Show Curve Connections");
            var showCurves = Settings.ShowCurves.Value;
            if (ImGui.Checkbox("##ShowCurves", ref showCurves))
            {
                Settings.ShowCurves.Value = showCurves;
            }

            ImGui.Separator();

            ImGui.Text("Enable Debug Drawing");
            var enableDebug = Settings.EnableDebugDrawing.Value;
            if (ImGui.Checkbox("##EnableDebug", ref enableDebug))
            {
                Settings.EnableDebugDrawing.Value = enableDebug;
            }

            ImGui.Text("Draw Portraits");
            var drawPortraits = Settings.DrawPortraits.Value;
            if (ImGui.Checkbox("##DrawPortraits", ref drawPortraits))
            {
                Settings.DrawPortraits.Value = drawPortraits;
            }

            ImGui.Text("Draw Relationships");
            var drawRelations = Settings.DrawRelations.Value;
            if (ImGui.Checkbox("##DrawRelations", ref drawRelations))
            {
                Settings.DrawRelations.Value = drawRelations;
            }
        }

        private void DrawMemberGoalsTab()
        {
            ImGui.Text("Fortification Members");
            ImGui.Separator();
            Settings.Aisling.Value = ImGuiExtension.ComboBox("Aisling##Fort", Settings.Aisling.Value, Settings.Aisling.Values, out var _);
            Settings.Cameria.Value = ImGuiExtension.ComboBox("Cameria##Fort", Settings.Cameria.Value, Settings.Cameria.Values, out var _);
            Settings.Elreon.Value = ImGuiExtension.ComboBox("Elreon##Fort", Settings.Elreon.Value, Settings.Elreon.Values, out var _);
            Settings.Gravicius.Value = ImGuiExtension.ComboBox("Gravicius##Fort", Settings.Gravicius.Value, Settings.Gravicius.Values, out var _);

            ImGui.Spacing();
            ImGui.Text("Research Members");
            ImGui.Separator();
            Settings.Guff.Value = ImGuiExtension.ComboBox("Guff##Res", Settings.Guff.Value, Settings.Guff.Values, out var _);
            Settings.Haku.Value = ImGuiExtension.ComboBox("Haku##Res", Settings.Haku.Value, Settings.Haku.Values, out var _);
            Settings.Hillock.Value = ImGuiExtension.ComboBox("Hillock##Res", Settings.Hillock.Value, Settings.Hillock.Values, out var _);
            Settings.ItThatFled.Value = ImGuiExtension.ComboBox("It That Fled##Res", Settings.ItThatFled.Value, Settings.ItThatFled.Values, out var _);

            ImGui.Spacing();
            ImGui.Text("Intervention Members");
            ImGui.Separator();
            Settings.Janus.Value = ImGuiExtension.ComboBox("Janus##Int", Settings.Janus.Value, Settings.Janus.Values, out var _);
            Settings.Jorgin.Value = ImGuiExtension.ComboBox("Jorgin##Int", Settings.Jorgin.Value, Settings.Jorgin.Values, out var _);
            Settings.Korell.Value = ImGuiExtension.ComboBox("Korell##Int", Settings.Korell.Value, Settings.Korell.Values, out var _);
            Settings.Leo.Value = ImGuiExtension.ComboBox("Leo##Int", Settings.Leo.Value, Settings.Leo.Values, out var _);

            ImGui.Spacing();
            ImGui.Text("Transportation Members");
            ImGui.Separator();
            Settings.Rin.Value = ImGuiExtension.ComboBox("Rin##Trans", Settings.Rin.Value, Settings.Rin.Values, out var _);
            Settings.Riker.Value = ImGuiExtension.ComboBox("Riker##Trans", Settings.Riker.Value, Settings.Riker.Values, out var _);
            Settings.Tora.Value = ImGuiExtension.ComboBox("Tora##Trans", Settings.Tora.Value, Settings.Tora.Values, out var _);
            Settings.Vagan.Value = ImGuiExtension.ComboBox("Vagan##Trans", Settings.Vagan.Value, Settings.Vagan.Values, out var _);
            Settings.Vorici.Value = ImGuiExtension.ComboBox("Vorici##Trans", Settings.Vorici.Value, Settings.Vorici.Values, out var _);
        }

        private void Settings_ApplyStrategyGoals(string value)
        {
            Settings.ApplyStrategyGoals(value);
            _isBoardStateDirty = true;
        }

        public override Job Tick()
        {
            if (!CanRun()) {
                _lastDecision = null;
                return null;
            }

            try
            {
            #pragma warning disable CS0618
            var betrayalWindow = GameController.IngameState?.IngameUi?.BetrayalWindow as SyndicatePanel;
            #pragma warning restore CS0618
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

            if (_uiElementsDirty)
            {
                UpdateUIElementCache(betrayalWindow);
                _uiElementsDirty = false;
            }

            var eventDataElement = betrayalWindow.BetrayalEventData as BetrayalEventData;
            _lastDecision = eventDataElement != null && eventDataElement.IsVisible ? ParseDecision(eventDataElement) : null;
            
            if (_lastDecision != null)
            {
                ProcessEncounterChoices(eventDataElement);
            }

            ProcessBoardOverlays(betrayalWindow);


            if (Input.IsKeyDown(Keys.LButton) && (DateTime.Now - _lastClickTime).TotalMilliseconds > SyndicateHelperConstants.MouseClickDebounceMs)
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
            catch (Exception ex)
            {
                LogError($"SyndicateHelper Tick error: {ex.Message}");
                return null;
            }
        }

        public override void Render()
        {
            if (!CanRun()) return;

            try
            {
                #pragma warning disable CS0618
                var betrayalWindow = GameController.IngameState?.IngameUi?.BetrayalWindow as SyndicatePanel;
                #pragma warning restore CS0618
                if (betrayalWindow == null || !betrayalWindow.IsVisible) return;

                var backgroundColor = new Color((byte)0, (byte)0, (byte)0, (byte)Settings.BackgroundAlpha.Value);
                var advisorBottomY = RenderStrategyAdvisor(betrayalWindow, backgroundColor);

            if (_lastDecision != null)
            {
                ProcessChoiceHighlights();
            }

            if (Settings.ShowButtons.Value)
            {
                foreach (var rect in _rectanglesToDraw) { Graphics.DrawFrame(rect.Item1, rect.Item2, Settings.FrameThickness.Value); }

                if (Settings.ShowCurves.Value)
                {
                    foreach (var link in _linksToDraw)
                    {
                        var goalAnchor = new System.Numerics.Vector2(link.Item1.Right, link.Item1.Top);
                        var buttonAnchor = new System.Numerics.Vector2(link.Item2.Left, link.Item2.Center.Y);
                        SyndicateHelperUtility.DrawBezierCurve(
                            goalAnchor,
                            buttonAnchor,
                            Settings.FrameThickness.Value,
                            link.Item3,
                            Graphics.DrawLine);
                    }
                }
            }

            if (Settings.ShowGoalInfo.Value)
            {
                foreach (var cachedText in _cachedRewardText)
                {
                    Graphics.DrawTextWithBackground(cachedText.Text, cachedText.Position, cachedText.Color, FontAlign.Left, backgroundColor);
                }
            }

            if (Settings.ShowButtons.Value)
            {
                foreach (var cachedText in _cachedChoiceScores)
                {
                    Graphics.DrawTextWithBackground(cachedText.Text, cachedText.Position, cachedText.Color, FontAlign.Left, backgroundColor);
                }
            }

            if (Settings.EnableDebugDrawing.Value)
            {
                var y = advisorBottomY + SyndicateHelperConstants.DebugDrawPositionY;
                var a = new System.Numerics.Vector2(SyndicateHelperConstants.DebugDrawPositionX, y);

                Graphics.DrawTextWithBackground(
                    $"Prison: {_imprisonedMemberCount}/{SyndicateHelperConstants.MaxPrisonSlots} slots filled.",
                    a, Color.White, FontAlign.Left, backgroundColor);
                a.Y += SyndicateHelperConstants.DebugLineSpacing;

                foreach (var msg in _debugMessages)
                {
                    Graphics.DrawTextWithBackground(msg, a, Color.White, FontAlign.Left, backgroundColor);
                    a.Y += SyndicateHelperConstants.DebugLineSpacing;
                }

                if (Settings.DrawPortraits.Value)
                {
                    foreach (var portrait in _cachedPortraitElements.Values)
                    {
                        if (portrait?.GetClientRectCache != null)
                        {
                            Graphics.DrawFrame(portrait.GetClientRectCache, Color.Cyan, 1);
                        }
                    }
                }

                if (Settings.DrawRelations.Value)
                {
                    foreach (var relation in _cachedRelationshipElements)
                    {
                        if (relation?.GetClientRectCache != null)
                        {
                            Graphics.DrawFrame(relation.GetClientRectCache, Color.Magenta, 1);
                        }
                    }
                }
            }
            }
            catch (Exception ex)
            {
                LogError($"SyndicateHelper Render error: {ex.Message}");
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
                var textSize = Graphics.MeasureText(scoreText, SyndicateHelperConstants.DefaultFontSize);
                var textPos = new System.Numerics.Vector2(
                    buttonRect.Right + SyndicateHelperConstants.ScoreTextOffsetX,
                    buttonRect.Center.Y - textSize.Y / 2 - SyndicateHelperConstants.ScoreTextOffsetY);

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

            if (betrayalWindow?.SyndicateStates == null)
            {
                return;
            }

            var leaders = betrayalWindow.SyndicateLeadersData?.Leaders?
                .Where(l => l?.Target != null)
                .Select(l => l.Target.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToHashSet();

            if (leaders == null)
            {
                leaders = new HashSet<string>();
            }

            foreach (var memberState in betrayalWindow.SyndicateStates)
            {
                var memberName = memberState?.Target?.Name;
                if (string.IsNullOrWhiteSpace(memberName)) continue;

                var rankName = memberState?.Rank?.Name;
                var jobName = memberState?.Job?.Name;

                if (Enum.TryParse(jobName, out SyndicateDivision division) ||
                    jobName == "None" ||
                    !string.IsNullOrWhiteSpace(rankName))
                {
                    var state = new SyndicateMemberState
                    {
                        Name = memberName,
                        Rank = rankName ?? string.Empty,
                        Division = division,
                        IsLeader = leaders.Contains(memberName)
                    };
                    newBoardState[memberName] = state;

                    if (IsMemberImprisoned(memberState?.UIElement)) prisonCount++;
                }
            }

            _boardState = newBoardState;
            _imprisonedMemberCount = prisonCount;

            foreach (var relElement in _cachedRelationshipElements)
            {
                var text = relElement?.Text;
                if (string.IsNullOrWhiteSpace(text)) continue;

                var match = Regex.Match(text, @"(.+?)\s+(is friends with|is rivals with)\s+(.+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var member1Name = match.Groups[1].Value.Trim();
                    var relationshipType = match.Groups[2].Value.Trim();
                    var member2Name = match.Groups[3].Value.Trim();

                    if (newBoardState.TryGetValue(member1Name, out var member1State) &&
                        newBoardState.TryGetValue(member2Name, out var member2State))
                    {
                        if (relationshipType.Equals("is friends with", StringComparison.OrdinalIgnoreCase))
                        {
                            member1State.Friends.Add(member2Name);
                            member2State.Friends.Add(member1Name);
                        }
                        else if (relationshipType.Equals("is rivals with", StringComparison.OrdinalIgnoreCase))
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

            var text = SyndicateHelperUtility.GetElementTextSafely(element);
            if (text.Contains("Turn Left", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("Turns Left", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            foreach (var child in element?.Children)
            {
                if (IsMemberImprisoned(child)) return true;
            }
            return false;
        }
        
        private float RenderStrategyAdvisor(SyndicatePanel betrayalWindow, Color backgroundColor)
        {
            var drawPos = new System.Numerics.Vector2(SyndicateHelperConstants.DefaultDrawPositionX, SyndicateHelperConstants.DefaultDrawPositionY);

            var leftButtonText = "<-";
            var leftButtonSize = Graphics.MeasureText(leftButtonText, SyndicateHelperConstants.DefaultFontSize);
            var leftButtonPos = drawPos;
            _leftButtonRect = new RectangleF(drawPos.X, drawPos.Y, leftButtonSize.X + SyndicateHelperConstants.ButtonPadding, leftButtonSize.Y + SyndicateHelperConstants.ButtonVerticalPadding);
            Graphics.DrawBox(_leftButtonRect, new Color((byte)0, (byte)0, (byte)0, SyndicateHelperConstants.ButtonBackgroundAlpha));
            Graphics.DrawTextWithBackground(leftButtonText, leftButtonPos, Color.White, FontAlign.Center, backgroundColor);

            var rightButtonText = "->";
            var rightButtonSize = Graphics.MeasureText(rightButtonText, SyndicateHelperConstants.DefaultFontSize);
            var rightButtonPos = new System.Numerics.Vector2(drawPos.X + SyndicateHelperConstants.ButtonPadding + leftButtonSize.X + SyndicateHelperConstants.ButtonHorizontalSpacing, drawPos.Y);
            _rightButtonRect = new RectangleF(rightButtonPos.X, drawPos.Y, rightButtonSize.X + SyndicateHelperConstants.ButtonPadding, rightButtonSize.Y + SyndicateHelperConstants.ButtonVerticalPadding);
            Graphics.DrawBox(_rightButtonRect, new Color((byte)0, (byte)0, (byte)0, SyndicateHelperConstants.ButtonBackgroundAlpha));
            Graphics.DrawTextWithBackground(rightButtonText, rightButtonPos, Color.White, FontAlign.Center, backgroundColor);

            var strategyName = Settings.StrategyProfile.Value ?? "Custom";
            var strategyNameSize = Graphics.MeasureText(strategyName, SyndicateHelperConstants.DefaultFontSize);
            var strategyNamePos = new System.Numerics.Vector2(_rightButtonRect.Right + SyndicateHelperConstants.ButtonHorizontalSpacing, drawPos.Y);
            Graphics.DrawTextWithBackground(strategyName, strategyNamePos, Color.Orange, FontAlign.Left, backgroundColor);

            drawPos.Y += Math.Max(leftButtonSize.Y, rightButtonSize.Y) + SyndicateHelperConstants.VerticalSpacing;

            Graphics.DrawTextWithBackground("Strategy Advisor:", drawPos, Color.White, FontAlign.Left, backgroundColor);
            drawPos.Y += SyndicateHelperConstants.GoalLineSpacing;
            
            var sortedGoals = _strategicGoals.OrderBy(g => g.Priority).ToList();
            foreach (var goal in sortedGoals)
            {
                string prefix = $"[{goal.Priority}] ";
                string fullText = prefix + goal.Text;
                var textSize = Graphics.MeasureText(fullText, SyndicateHelperConstants.DefaultFontSize);
                var goalRect = new RectangleF(
                    drawPos.X - SyndicateHelperConstants.GoalFrameBorderPadding,
                    drawPos.Y - SyndicateHelperConstants.GoalFrameBorderPadding,
                    textSize.X + SyndicateHelperConstants.TextPadding * 2 + SyndicateHelperConstants.GoalFrameBorderPadding * 2,
                    textSize.Y + SyndicateHelperConstants.GoalFrameBorderPadding * 2
                );
                _goalRects.Add(goalRect);

                if (_lastDecision != null)
                {
                    bool specialCompletes = ChoiceAccomplishesGoal(_lastDecision.SpecialText, goal.Text, _lastDecision.MemberName, _boardState);
                    bool interrogateCompletes = ChoiceAccomplishesGoal("Interrogate", goal.Text, _lastDecision.MemberName, _boardState);

                    var buttonToLink = specialCompletes ? _lastDecision.SpecialButton : (interrogateCompletes ? _lastDecision.InterrogateButton : null);
                    if (buttonToLink != null)
                    {
                         _linksToDraw.Add(new Tuple<RectangleF, RectangleF, Color>(goalRect, buttonToLink.GetClientRectCache, Settings.GoalCompletionColor.Value));
                    }
                }
                Graphics.DrawTextWithBackground(fullText, new System.Numerics.Vector2(drawPos.X + SyndicateHelperConstants.TextPadding, drawPos.Y), goal.DisplayColor, FontAlign.Left, backgroundColor);
                drawPos.Y += SyndicateHelperConstants.LineSpacing;
            }
            return drawPos.Y;
        }
        
        private void GenerateStrategicGoals(SyndicatePanel betrayalWindow)
        {
            if (_boardState.Count == 0) return;

            var allGoals = SyndicateMemberNames
                .Select(name => new { Name = name, GoalString = SyndicateHelperUtility.GetDesiredDivisionForMember(name, Settings) })
                .ToDictionary(g => g.Name, g => SyndicateHelperUtility.ParseGoal(g.GoalString));

            var allDivisions = Enum.GetValues(typeof(SyndicateDivision))
                .Cast<SyndicateDivision>()
                .Where(d => d != SyndicateDivision.None)
                .ToList();

            var leadersByDivision = _boardState.Values
                .Where(m => m.IsLeader)
                .ToDictionary(m => m.Division, m => m);

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
            if (betrayalWindow?.SyndicateStates == null) return;

            foreach (var memberState in _boardState.Values)
            {
                if (!_cachedPortraitElements.TryGetValue(memberState.Name, out var portraitElement))
                    continue;

                if (!SyndicateRewardData.Rewards.TryGetValue(memberState.Name, out var memberRewards))
                    continue;

                if (!memberRewards.TryGetValue(memberState.Division, out var rewardInfo))
                    continue;

                var desiredGoal = SyndicateHelperUtility.ParseGoal(
                    SyndicateHelperUtility.GetDesiredDivisionForMember(memberState.Name, Settings));
                var desiredDivision = desiredGoal.Division.ToString();
                var rewardText = rewardInfo.Text;
                bool isTargetReward = memberState.Division.ToString() == desiredDivision && desiredDivision != "None";

                var textColor = isTargetReward ? Settings.GoodChoiceColor.Value : rewardInfo.Tier switch
                {
                    RewardTier.Great => Color.LimeGreen,
                    RewardTier.Good => Color.Yellow,
                    RewardTier.Average => Color.White,
                    RewardTier.Worst => new Color(255, 80, 80),
                    _ => Color.White
                };

                if (rewardInfo.Tier == RewardTier.Worst &&
                    desiredDivision != "None" &&
                    desiredDivision != memberState.Division.ToString())
                {
                    rewardText += $" (-> {desiredDivision})";
                }

                var rect = portraitElement.GetClientRectCache;
                var textSize = Graphics.MeasureText(rewardText, SyndicateHelperConstants.DefaultFontSize);
                var textPos = new System.Numerics.Vector2(rect.Center.X - textSize.X / 2, rect.Bottom + 2);
                _cachedRewardText.Add(new CachedText { Text = rewardText, Position = textPos, Color = textColor });
            }
        }
        
        private bool ChoiceAccomplishesGoal(string choiceText, string goalText, string memberInDecision, Dictionary<string, SyndicateMemberState> boardState)
        {
            if (string.IsNullOrEmpty(choiceText) || string.IsNullOrEmpty(goalText)) return false;
            if (goalText.Contains("Rank up"))
            {
                var parts = goalText.Split(' ');
                if (parts.Length < 3) return false;
                var targetMember = parts[2];
                return memberInDecision == targetMember && choiceText.IndexOf("rank", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            if (goalText.StartsWith("Problem:"))
            {
                var parts = goalText.Split(' ');
                if (parts.Length < 2) return false;
                var targetMember = parts[1];
                return memberInDecision == targetMember && choiceText.Equals("Interrogate", StringComparison.OrdinalIgnoreCase);
            }
             if (goalText.Contains("Establish a leader"))
            {
                var parts = goalText.Split(' ');
                if (parts.Length < 5) return false;
                var divisionName = parts[4];
                return choiceText.Contains("rank", StringComparison.OrdinalIgnoreCase) && boardState.TryGetValue(memberInDecision, out var state) && state.Division.ToString() == divisionName;
            }
            if (goalText.StartsWith("Move") || goalText.StartsWith("Place"))
            {
                var match = Regex.Match(goalText, @"(Move|Place) (.+?) to (.+?)( |to|$)");
                if (match.Success && match.Groups.Count >= 4)
                {
                    var targetMember = match.Groups[2].Value.Trim();
                    var desiredDivision = match.Groups[3].Value.Trim();
                    return choiceText.Contains(targetMember, StringComparison.OrdinalIgnoreCase) && choiceText.Contains($"moves to {desiredDivision}", StringComparison.OrdinalIgnoreCase);
                }
            }
            return false;
        }



        private void AddDebug(string message)
        {
            if (!Settings.EnableDebugDrawing.Value) return;

            if (_debugMessages.Count >= SyndicateHelperConstants.MaxDebugMessages)
            {
                _debugMessages.RemoveAt(0);
            }
            _debugMessages.Add(message);
        }

        private void FindPortraitsRecursive(Element currentElement, Dictionary<string, Element> foundPortraits)
        {
            if (currentElement == null) return;

            var text = SyndicateHelperUtility.GetElementTextSafely(currentElement);
            var memberName = SyndicateMemberNames.FirstOrDefault(name =>
                text.Equals(name, StringComparison.Ordinal));

            if (memberName != null &&
                !foundPortraits.ContainsKey(memberName) &&
                currentElement.Parent != null)
            {
                foundPortraits[memberName] = currentElement.Parent;
                return;
            }

            foreach (var child in currentElement.Children)
            {
                FindPortraitsRecursive(child, foundPortraits);
            }
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
        
        private bool CanRun() => Settings?.Enable?.Value == true && !GameController.IsLoading && GameController.IngameState?.InGame == true;

        public override void AreaChange(AreaInstance area)
        {
            _isBoardStateDirty = true;
            _uiElementsDirty = true;
            base.AreaChange(area);
        }

        private void UpdateUIElementCache(SyndicatePanel betrayalWindow)
        {
            if (betrayalWindow == null)
            {
                _cachedPortraitElements.Clear();
                _cachedRelationshipElements.Clear();
                return;
            }

            _cachedPortraitElements.Clear();
            _cachedRelationshipElements.Clear();

            FindPortraitsRecursive(betrayalWindow, _cachedPortraitElements);
            FindRelationshipElementsRecursive(betrayalWindow, _cachedRelationshipElements);
        }

        private List<Element> FindRelationshipElements(SyndicatePanel betrayalWindow)
        {
            var relationshipElements = new List<Element>();
            if (betrayalWindow != null) FindRelationshipElementsRecursive(betrayalWindow, relationshipElements);
            return relationshipElements;
        }

        private void FindRelationshipElementsRecursive(Element currentElement, List<Element> relationshipElements)
        {
            if (currentElement == null) return;

            var text = SyndicateHelperUtility.GetElementTextSafely(currentElement);
            if (text.Contains("is friends with", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("is rivals with", StringComparison.OrdinalIgnoreCase))
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