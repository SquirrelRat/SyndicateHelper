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
    public enum RewardTier { Great, Good, Average, Worst }

    public class RewardInfo { public string Text { get; set; } public RewardTier Tier { get; set; } }

    public class SyndicateMemberState { public string Name { get; set; } public string Rank { get; set; } public SyndicateDivision Division { get; set; } public bool IsLeader { get; set; } }

    public class SyndicateDecision { public string MemberName { get; set; } public Element InterrogateButton { get; set; } public Element ReleaseButton { get; set; } public Element SpecialButton { get; set; } public string InterrogateText { get; set; } public string SpecialText { get; set; } }

    public class SyndicateHelper : BaseSettingsPlugin<SyndicateHelperSettings>
    {
        private readonly List<Tuple<RectangleF, Color>> _rectanglesToDraw = new();
        private readonly List<Tuple<string, Color>> _strategicGoals = new List<Tuple<string, Color>>();
        private string _currentStrategy = "";
        private readonly List<string> _debugMessages = new List<string>();
        private readonly HashSet<SyndicateDivision> _targetDivisions = new HashSet<SyndicateDivision>();

        private static readonly List<string> SyndicateMemberNames = new List<string> { "Aisling", "Cameria", "Elreon", "Gravicius", "Guff", "Haku", "Hillock", "It That Fled", "Janus", "Jorgin", "Korell", "Leo", "Rin", "Riker", "Tora", "Vagan", "Vorici" };

        private static readonly Dictionary<string, Dictionary<SyndicateDivision, RewardInfo>> Rewards = new Dictionary<string, Dictionary<SyndicateDivision, RewardInfo>> {
            ["Aisling"] = new Dictionary<SyndicateDivision, RewardInfo> { [SyndicateDivision.Transportation] = new RewardInfo { Text = "Dbl-Veiled Items", Tier = RewardTier.Good }, [SyndicateDivision.Fortification] = new RewardInfo { Text = "Veiled Exalt", Tier = RewardTier.Worst }, [SyndicateDivision.Research] = new RewardInfo { Text = "Veiled Chaos", Tier = RewardTier.Great }, [SyndicateDivision.Intervention] = new RewardInfo { Text = "Torment Scarabs", Tier = RewardTier.Good } },
            ["Cameria"] = new Dictionary<SyndicateDivision, RewardInfo> { [SyndicateDivision.Transportation] = new RewardInfo { Text = "Abyss Scarabs", Tier = RewardTier.Good }, [SyndicateDivision.Fortification] = new RewardInfo { Text = "Jewel Chest", Tier = RewardTier.Worst }, [SyndicateDivision.Research] = new RewardInfo { Text = "Jewel Craft", Tier = RewardTier.Average }, [SyndicateDivision.Intervention] = new RewardInfo { Text = "Delirium Scarabs", Tier = RewardTier.Great } },
            ["Elreon"] = new Dictionary<SyndicateDivision, RewardInfo> { [SyndicateDivision.Transportation] = new RewardInfo { Text = "Fragments", Tier = RewardTier.Average }, [SyndicateDivision.Fortification] = new RewardInfo { Text = "Corrupt Equip.", Tier = RewardTier.Worst }, [SyndicateDivision.Research] = new RewardInfo { Text = "Tainted Craft", Tier = RewardTier.Average }, [SyndicateDivision.Intervention] = new RewardInfo { Text = "Beyond Scarabs", Tier = RewardTier.Good } },
            ["Gravicius"] = new Dictionary<SyndicateDivision, RewardInfo> { [SyndicateDivision.Transportation] = new RewardInfo { Text = "Div Cards Stack", Tier = RewardTier.Good }, [SyndicateDivision.Fortification] = new RewardInfo { Text = "Div Card Chest", Tier = RewardTier.Worst }, [SyndicateDivision.Research] = new RewardInfo { Text = "Swap Div Card", Tier = RewardTier.Worst }, [SyndicateDivision.Intervention] = new RewardInfo { Text = "Divination Scarabs", Tier = RewardTier.Good } },
            ["Guff"] = new Dictionary<SyndicateDivision, RewardInfo> { [SyndicateDivision.Transportation] = new RewardInfo { Text = "Misc. Currency", Tier = RewardTier.Average }, [SyndicateDivision.Fortification] = new RewardInfo { Text = "Rare Equip.", Tier = RewardTier.Worst }, [SyndicateDivision.Research] = new RewardInfo { Text = "Misc. Craft", Tier = RewardTier.Good }, [SyndicateDivision.Intervention] = new RewardInfo { Text = "Blight Scarabs", Tier = RewardTier.Good } },
            ["Haku"] = new Dictionary<SyndicateDivision, RewardInfo> { [SyndicateDivision.Transportation] = new RewardInfo { Text = "Unique Strongbox", Tier = RewardTier.Average }, [SyndicateDivision.Fortification] = new RewardInfo { Text = "Domination Scarabs", Tier = RewardTier.Average }, [SyndicateDivision.Research] = new RewardInfo { Text = "Influence Craft", Tier = RewardTier.Good }, [SyndicateDivision.Intervention] = new RewardInfo { Text = "Ambush Scarabs", Tier = RewardTier.Good } },
            ["Hillock"] = new Dictionary<SyndicateDivision, RewardInfo> { [SyndicateDivision.Transportation] = new RewardInfo { Text = "Influenced Equip.", Tier = RewardTier.Worst }, [SyndicateDivision.Fortification] = new RewardInfo { Text = "30% Quality", Tier = RewardTier.Great }, [SyndicateDivision.Research] = new RewardInfo { Text = "Eldritch Implicit", Tier = RewardTier.Average }, [SyndicateDivision.Intervention] = new RewardInfo { Text = "Influenced Scarabs", Tier = RewardTier.Good } },
            ["It That Fled"] = new Dictionary<SyndicateDivision, RewardInfo> { [SyndicateDivision.Transportation] = new RewardInfo { Text = "Corrupt Maps", Tier = RewardTier.Average }, [SyndicateDivision.Fortification] = new RewardInfo { Text = "Breachstone Bargain", Tier = RewardTier.Worst }, [SyndicateDivision.Research] = new RewardInfo { Text = "Tainted Sockets", Tier = RewardTier.Good }, [SyndicateDivision.Intervention] = new RewardInfo { Text = "Breach Scarabs", Tier = RewardTier.Good } },
            ["Janus"] = new Dictionary<SyndicateDivision, RewardInfo> { [SyndicateDivision.Transportation] = new RewardInfo { Text = "Gold Piles", Tier = RewardTier.Worst }, [SyndicateDivision.Fortification] = new RewardInfo { Text = "Kaguuran Scarabs", Tier = RewardTier.Worst }, [SyndicateDivision.Research] = new RewardInfo { Text = "Cadiro's Offer", Tier = RewardTier.Great }, [SyndicateDivision.Intervention] = new RewardInfo { Text = "Expedition Scarabs", Tier = RewardTier.Good } },
            ["Jorgin"] = new Dictionary<SyndicateDivision, RewardInfo> { [SyndicateDivision.Transportation] = new RewardInfo { Text = "Sulphite Scarabs", Tier = RewardTier.Good }, [SyndicateDivision.Fortification] = new RewardInfo { Text = "Delve Equip.", Tier = RewardTier.Worst }, [SyndicateDivision.Research] = new RewardInfo { Text = "Talisman Craft", Tier = RewardTier.Good }, [SyndicateDivision.Intervention] = new RewardInfo { Text = "Bestiary Scarabs", Tier = RewardTier.Great } },
            ["Korell"] = new Dictionary<SyndicateDivision, RewardInfo> { [SyndicateDivision.Transportation] = new RewardInfo { Text = "Anarchy Scarabs", Tier = RewardTier.Average }, [SyndicateDivision.Fortification] = new RewardInfo { Text = "Essence Equip.", Tier = RewardTier.Worst }, [SyndicateDivision.Research] = new RewardInfo { Text = "Essence Craft", Tier = RewardTier.Good }, [SyndicateDivision.Intervention] = new RewardInfo { Text = "Essence Scarabs", Tier = RewardTier.Good } },
            ["Leo"] = new Dictionary<SyndicateDivision, RewardInfo> { [SyndicateDivision.Transportation] = new RewardInfo { Text = "Incursion Scarabs", Tier = RewardTier.Average }, [SyndicateDivision.Fortification] = new RewardInfo { Text = "Corrupt Unique", Tier = RewardTier.Worst }, [SyndicateDivision.Research] = new RewardInfo { Text = "Djinn Baal Orb", Tier = RewardTier.Good }, [SyndicateDivision.Intervention] = new RewardInfo { Text = "Ultimatum Scarabs", Tier = RewardTier.Great } },
            ["Riker"] = new Dictionary<SyndicateDivision, RewardInfo> { [SyndicateDivision.Transportation] = new RewardInfo { Text = "Unique Items", Tier = RewardTier.Average }, [SyndicateDivision.Fortification] = new RewardInfo { Text = "Unique Item Chest", Tier = RewardTier.Worst }, [SyndicateDivision.Research] = new RewardInfo { Text = "Ancient Orb", Tier = RewardTier.Good }, [SyndicateDivision.Intervention] = new RewardInfo { Text = "Titanic Scarabs", Tier = RewardTier.Good } },
            ["Rin"] = new Dictionary<SyndicateDivision, RewardInfo> { [SyndicateDivision.Transportation] = new RewardInfo { Text = "Map Currency", Tier = RewardTier.Average }, [SyndicateDivision.Fortification] = new RewardInfo { Text = "Unique Map Chest", Tier = RewardTier.Worst }, [SyndicateDivision.Research] = new RewardInfo { Text = "Map Craft", Tier = RewardTier.Average }, [SyndicateDivision.Intervention] = new RewardInfo { Text = "Cartography Scarabs", Tier = RewardTier.Great } },
            ["Tora"] = new Dictionary<SyndicateDivision, RewardInfo> { [SyndicateDivision.Transportation] = new RewardInfo { Text = "Quality Gems", Tier = RewardTier.Average }, [SyndicateDivision.Fortification] = new RewardInfo { Text = "Gem Chest", Tier = RewardTier.Worst }, [SyndicateDivision.Research] = new RewardInfo { Text = "Gem Craft", Tier = RewardTier.Good }, [SyndicateDivision.Intervention] = new RewardInfo { Text = "Ritual Scarabs", Tier = RewardTier.Great } },
            ["Vagan"] = new Dictionary<SyndicateDivision, RewardInfo> { [SyndicateDivision.Transportation] = new RewardInfo { Text = "Harbinger Scarabs", Tier = RewardTier.Good }, [SyndicateDivision.Fortification] = new RewardInfo { Text = "Incubators", Tier = RewardTier.Worst }, [SyndicateDivision.Research] = new RewardInfo { Text = "Chaos/Fracture", Tier = RewardTier.Average }, [SyndicateDivision.Intervention] = new RewardInfo { Text = "Legion Scarabs", Tier = RewardTier.Great } },
            ["Vorici"] = new Dictionary<SyndicateDivision, RewardInfo> { [SyndicateDivision.Transportation] = new RewardInfo { Text = "Stack of Currency", Tier = RewardTier.Average }, [SyndicateDivision.Fortification] = new RewardInfo { Text = "Socket Crafts", Tier = RewardTier.Average }, [SyndicateDivision.Research] = new RewardInfo { Text = "Socket Color", Tier = RewardTier.Great }, [SyndicateDivision.Intervention] = new RewardInfo { Text = "Harvest Scarabs", Tier = RewardTier.Great } }
        };

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

            if (_currentStrategy != Settings.StrategyProfile.Value)
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

        private void RenderStrategyAdvisor(SyndicatePanel betrayalWindow)
        {
            var drawPos = new System.Numerics.Vector2(100, 150);
            Graphics.DrawText("Strategy Advisor:", drawPos, Color.White, 18);
            drawPos.Y += 25;

            var decision = (betrayalWindow.BetrayalEventData as BetrayalEventData)?.IsVisible == true
                ? ParseDecision(betrayalWindow.BetrayalEventData as BetrayalEventData)
                : null;

            foreach (var goal in _strategicGoals)
            {
                var goalText = goal.Item1;
                var goalColor = goal.Item2;
                var textSize = Graphics.MeasureText(goalText);
                var goalRect = new RectangleF(drawPos.X, drawPos.Y, textSize.X, textSize.Y);

                if (decision != null)
                {
                    var boardState = ParseBoardState(betrayalWindow);
                    var specialButtonRect = decision.SpecialButton.GetClientRectCache;
                    var interrogateButtonRect = decision.InterrogateButton.GetClientRectCache;
                    var lineStart = new System.Numerics.Vector2(goalRect.Right, goalRect.Center.Y);
                    var specialEnd = new System.Numerics.Vector2(specialButtonRect.Center.X, specialButtonRect.Center.Y);
                    var interrogateEnd = new System.Numerics.Vector2(interrogateButtonRect.Center.X, interrogateButtonRect.Center.Y);

                    if (ChoiceAccomplishesGoal(decision.SpecialText, goalText, decision.MemberName, boardState))
                    {
                        Graphics.DrawFrame(goalRect, Settings.GoodChoiceColor.Value, 2);
                        Graphics.DrawLine(lineStart, specialEnd, 2, Settings.GoodChoiceColor.Value);
                    }
                    else if (ChoiceAccomplishesGoal("Interrogate", goalText, decision.MemberName, boardState))
                    {
                        Graphics.DrawFrame(goalRect, Settings.GoodChoiceColor.Value, 2);
                        Graphics.DrawLine(lineStart, interrogateEnd, 2, Settings.GoodChoiceColor.Value);
                    }
                }
                Graphics.DrawText(goalText, drawPos, goalColor);
                drawPos.Y += 20;
            }
        }

        private bool ChoiceAccomplishesGoal(string choiceText, string goalText, string memberName, Dictionary<string, SyndicateMemberState> boardState)
        {
            if (!goalText.Contains(memberName)) return false;
            if (!boardState.TryGetValue(memberName, out var currentState)) return false;

            if (goalText.Contains("Rank up"))
            {
                int rank = currentState.Rank switch { "Sergeant" => 1, "Lieutenant" => 2, "Captain" => 3, _ => 0 };
                // A "rank up" choice can be "Execute" or any bargain that says "+1 rank"
                return choiceText.ToLower().Contains("rank") && rank < 3;
            }

            if (goalText.Contains("Demote") || goalText.Contains("demoted"))
            {
                return choiceText.ToLower().Contains("interrogate");
            }

            if (goalText.StartsWith("Place") || goalText.StartsWith("Move"))
            {
                var match = Regex.Match(goalText, @"(Move|Place) .* to (.+)");
                if (match.Success)
                {
                    var desiredDivision = match.Groups[2].Value;
                    return choiceText.Contains($"moves to {desiredDivision}");
                }
            }
            return false;
        }

        private void GenerateStrategicGoals(SyndicatePanel betrayalWindow)
        {
            if (betrayalWindow.SyndicateStates == null) return;

            var boardState = ParseBoardState(betrayalWindow);
            var targetMembers = Settings.GetType().GetProperties()
                .Where(p => p.PropertyType == typeof(ListNode) && SyndicateMemberNames.Contains(p.Name))
                .Select(p => new { Name = p.Name, Goal = (p.GetValue(Settings) as ListNode).Value })
                .ToList();

            // 1. Generate goals for our main targets
            foreach (var target in targetMembers.Where(t => t.Goal != "None"))
            {
                if (boardState.TryGetValue(target.Name, out var currentState))
                {
                    if (!Enum.TryParse(target.Goal, out SyndicateDivision desiredDivision)) continue;

                    if (currentState.Division != desiredDivision)
                    {
                        // Priority 1: Move the target to the correct division
                        _strategicGoals.Add(new Tuple<string, Color>($"Move {target.Name} to {target.Goal}", Color.Yellow));
                    }
                    else
                    {
                        // Target is in the right place, check for leadership
                        if (currentState.IsLeader)
                        {
                            _strategicGoals.Add(new Tuple<string, Color>($"{target.Name} is leading {target.Goal}. (Optimal)", Color.LimeGreen));
                        }
                        else
                        {
                            // Not the leader yet, needs promotion
                            _strategicGoals.Add(new Tuple<string, Color>($"Rank up {target.Name} to become leader of {target.Goal}", Color.LightGreen));
                        }
                    }
                }
                else
                {
                    // Target member is not on the board at all
                    _strategicGoals.Add(new Tuple<string, Color>($"Place {target.Name} in {target.Goal}", Color.Yellow));
                }
            }

            // 2. Identify and flag blocking leaders
            foreach (var targetDivisionEnum in _targetDivisions)
            {
                var leaderOfDivision = boardState.Values.FirstOrDefault(m => m.Division == targetDivisionEnum && m.IsLeader);
                if (leaderOfDivision != null)
                {
                    // Is this leader one of our targets for this division?
                    bool isTargetLeader = targetMembers.Any(t => t.Name == leaderOfDivision.Name && t.Goal == targetDivisionEnum.ToString());

                    if (!isTargetLeader)
                    {
                        _strategicGoals.Add(new Tuple<string, Color>($"Problem: {leaderOfDivision.Name} is leading {targetDivisionEnum}. Needs to be moved or demoted.", Color.OrangeRed));
                    }
                }
            }

            if (_strategicGoals.Count == 0)
            {
                _strategicGoals.Add(new Tuple<string, Color>("Board is optimal for current strategy.", Color.White));
            }
        }

        private void ProcessEncounterChoices(SyndicatePanel betrayalWindow)
        {
            var eventDataElement = betrayalWindow.BetrayalEventData as BetrayalEventData;
            if (eventDataElement == null) return;
            if (betrayalWindow.SyndicateStates == null)
            {
                AddDebug("SyndicateStates is null. Cannot process choices.");
                return;
            }

            var currentBoardState = ParseBoardState(betrayalWindow);
            var decision = ParseDecision(eventDataElement);
            if (decision == null)
            {
                AddDebug("ParseDecision returned null.");
                return;
            }

            AddDebug($"Decision parsed for: {decision.MemberName}");
            var interrogateColor = EvaluateChoice(decision, "Interrogate", currentBoardState, betrayalWindow);
            var specialColor = EvaluateChoice(decision, decision.SpecialText, currentBoardState, betrayalWindow);

            if (interrogateColor == Settings.BadChoiceColor.Value && specialColor == Settings.BadChoiceColor.Value && decision.ReleaseButton.IsVisible)
            {
                _rectanglesToDraw.Add(new Tuple<RectangleF, Color>(decision.ReleaseButton.GetClientRectCache, Settings.GoodChoiceColor.Value));
            }
            else
            {
                _rectanglesToDraw.Add(new Tuple<RectangleF, Color>(decision.InterrogateButton.GetClientRectCache, interrogateColor));
                _rectanglesToDraw.Add(new Tuple<RectangleF, Color>(decision.SpecialButton.GetClientRectCache, specialColor));
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
                    Rewards.TryGetValue(memberState.Name, out var memberRewards) &&
                    memberRewards.TryGetValue(memberState.Division, out var rewardInfo))
                {
                    var desiredDivision = GetDesiredDivisionForMember(memberState.Name);
                    var rewardText = rewardInfo.Text;
                    Color textColor;

                    bool isTargetReward = memberState.Division.ToString() == desiredDivision && desiredDivision != "None";

                    if (isTargetReward)
                    {
                        textColor = Settings.GoodChoiceColor.Value;
                    }
                    else
                    {
                        textColor = rewardInfo.Tier switch
                        {
                            RewardTier.Great => Color.LimeGreen,
                            RewardTier.Good => Color.Yellow,
                            RewardTier.Average => Color.White,
                            RewardTier.Worst => new Color(255, 80, 80),
                            _ => Color.White
                        };
                    }

                    if (rewardInfo.Tier == RewardTier.Worst && desiredDivision != "None")
                    {
                        rewardText += $" (-> {desiredDivision})";
                    }

                    var rect = portraitElement.GetClientRectCache;
                    var textSize = Graphics.MeasureText(rewardText);
                    var textPos = new System.Numerics.Vector2(rect.Center.X - textSize.X / 2, rect.Bottom + 2);
                    Graphics.DrawTextWithBackground(rewardText, textPos, textColor, FontAlign.Left, new Color(0, 0, 0, 220));
                }
            }
        }

        private void AddDebug(string message)
        {
            if (Settings.EnableDebugDrawing.Value)
                _debugMessages.Add(message);
        }

        private string GetDesiredDivisionForMember(string memberName)
        {
            return memberName switch
            {
                "Aisling" => Settings.Aisling.Value, "Cameria" => Settings.Cameria.Value, "Elreon" => Settings.Elreon.Value,
                "Gravicius" => Settings.Gravicius.Value, "Guff" => Settings.Guff.Value, "Haku" => Settings.Haku.Value,
                "Hillock" => Settings.Hillock.Value, "It That Fled" => Settings.ItThatFled.Value, "Janus" => Settings.Janus.Value,
                "Jorgin" => Settings.Jorgin.Value, "Korell" => Settings.Korell.Value, "Leo" => Settings.Leo.Value,
                "Rin" => Settings.Rin.Value, "Riker" => Settings.Riker.Value, "Tora" => Settings.Tora.Value,
                "Vagan" => Settings.Vagan.Value, "Vorici" => Settings.Vorici.Value,
                _ => "None"
            };
        }

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
            if (memberName != null && !foundPortraits.ContainsKey(memberName))
            {
                if (currentElement.Parent != null)
                {
                    foundPortraits[memberName] = currentElement.Parent;
                    return;
                }
            }
            foreach (var child in currentElement.Children)
            {
                FindPortraitsRecursive(child, foundPortraits);
            }
        }

        private SyndicateDecision ParseDecision(BetrayalEventData eventDataElement)
        {
            var choiceDescriptionText = eventDataElement.EventText;
            var memberName = FindNameInChoiceDialog(eventDataElement);
            if (string.IsNullOrWhiteSpace(memberName))
            {
                AddDebug("Failed to find member name in choice dialog via recursive search.");
                return null;
            }

            return new SyndicateDecision
            {
                MemberName = memberName,
                InterrogateButton = eventDataElement.InterrogateButton,
                SpecialButton = eventDataElement.SpecialButton,
                ReleaseButton = eventDataElement.ReleaseButton,
                InterrogateText = "Interrogate",
                SpecialText = choiceDescriptionText ?? ""
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

        private Color EvaluateChoice(SyndicateDecision decision, string choiceText, Dictionary<string, SyndicateMemberState> boardState, SyndicatePanel betrayalWindow)
        {
            if (choiceText.ToLower().Contains("interrogate"))
            {
                int imprisonedCount = GetImprisonedCount(betrayalWindow);
                AddDebug($"Imprisoned Count: {imprisonedCount}");
                if (imprisonedCount >= 3)
                {
                    AddDebug("Prison is full. Interrogate is BAD.");
                    return Settings.BadChoiceColor.Value;
                }
            }

            if (choiceText.Contains("become trusted") || choiceText.Contains("become rivals")) return Settings.GoodChoiceColor.Value;

            var desiredDivisionStr = GetDesiredDivisionForMember(decision.MemberName);
            if (desiredDivisionStr == "None")
            {
                if (choiceText.Contains("Remove from Syndicate")) return Settings.GoodChoiceColor.Value;
                if (choiceText.ToLower().Contains("interrogate")) return Settings.GoodChoiceColor.Value;

                foreach (var targetDivision in _targetDivisions)
                {
                    if (choiceText.Contains($"moves to {targetDivision}"))
                    {
                        return Settings.BadChoiceColor.Value;
                    }
                }
                if (choiceText.Contains("moves to")) return Settings.GoodChoiceColor.Value;
                return Settings.BadChoiceColor.Value;
            }

            if (!Enum.TryParse(desiredDivisionStr, out SyndicateDivision desiredDivision)) return Settings.NeutralChoiceColor.Value;
            if (!boardState.TryGetValue(decision.MemberName, out var currentState)) return Settings.NeutralChoiceColor.Value;

            int currentRank = currentState.Rank switch { "Sergeant" => 1, "Lieutenant" => 2, "Captain" => 3, _ => 0 };

            if (choiceText.ToLower().Contains("interrogate"))
            {
                if (currentRank <= 1 && currentState.Division != desiredDivision) return Settings.NeutralChoiceColor.Value;
                if (currentState.Division == SyndicateDivision.None) return Settings.BadChoiceColor.Value;
                return currentState.Division != desiredDivision ? Settings.GoodChoiceColor.Value : Settings.BadChoiceColor.Value;
            }

            if (choiceText.Contains("Remove from Syndicate")) return Settings.BadChoiceColor.Value;

            if (choiceText.Contains($"moves to {desiredDivision}")) return Settings.GoodChoiceColor.Value;

            if (currentState.Division == desiredDivision)
            {
                return currentRank < 3 ? Settings.GoodChoiceColor.Value : Settings.NeutralChoiceColor.Value;
            }

            return Settings.BadChoiceColor.Value;
        }

        private int GetImprisonedCount(SyndicatePanel betrayalWindow)
        {
            var searchRoot = betrayalWindow.GetChildFromIndices(0);
            if (searchRoot == null) return 0;
            return CountTurnsLeftRecursive(searchRoot);
        }

        private int CountTurnsLeftRecursive(Element currentElement)
        {
            if (currentElement == null) return 0;
            int count = 0;
            if (currentElement.Text?.Contains("Turns Left") ?? false)
            {
                count++;
            }
            foreach (var child in currentElement.Children)
            {
                count += CountTurnsLeftRecursive(child);
            }
            return count;
        }

        private void ApplyStrategyProfile()
        {
            _targetDivisions.Clear();
            var allNodes = new List<Tuple<string, ListNode>> {
                new Tuple<string, ListNode>("Aisling", Settings.Aisling), new Tuple<string, ListNode>("Cameria", Settings.Cameria),
                new Tuple<string, ListNode>("Elreon", Settings.Elreon), new Tuple<string, ListNode>("Gravicius", Settings.Gravicius),
                new Tuple<string, ListNode>("Guff", Settings.Guff), new Tuple<string, ListNode>("Haku", Settings.Haku),
                new Tuple<string, ListNode>("Hillock", Settings.Hillock), new Tuple<string, ListNode>("It That Fled", Settings.ItThatFled),
                new Tuple<string, ListNode>("Janus", Settings.Janus), new Tuple<string, ListNode>("Jorgin", Settings.Jorgin),
                new Tuple<string, ListNode>("Korell", Settings.Korell), new Tuple<string, ListNode>("Leo", Settings.Leo),
                new Tuple<string, ListNode>("Rin", Settings.Rin), new Tuple<string, ListNode>("Riker", Settings.Riker),
                new Tuple<string, ListNode>("Tora", Settings.Tora), new Tuple<string, ListNode>("Vagan", Settings.Vagan),
                new Tuple<string, ListNode>("Vorici", Settings.Vorici)
            };
            foreach (var node in allNodes) node.Item2.Value = "None";

            var strategy = new Dictionary<string, string>();
            switch (Settings.StrategyProfile.Value)
            {
                case "Crafting Meta (Research)":
                    strategy["Aisling"] = "Research"; strategy["Vorici"] = "Research"; strategy["It That Fled"] = "Research";
                    strategy["Hillock"] = "Fortification"; strategy["Tora"] = "Research";
                    break;
                case "Scarab Farm (Intervention)":
                    strategy["Cameria"] = "Intervention"; strategy["Rin"] = "Intervention"; strategy["Vagan"] = "Intervention";
                    strategy["Riker"] = "Intervention"; strategy["Janus"] = "Intervention"; strategy["Vorici"] = "Intervention";
                    break;
                case "Gamble (Currency/Div)":
                    strategy["Gravicius"] = "Transportation"; strategy["Vorici"] = "Transportation";
                    strategy["Riker"] = "Transportation"; strategy["Tora"] = "Research"; strategy["Rin"] = "Intervention";
                    break;
            }

            foreach (var set in strategy)
            {
                allNodes.First(x => x.Item1 == set.Key).Item2.Value = set.Value;
                if (Enum.TryParse(set.Value, out SyndicateDivision div))
                    _targetDivisions.Add(div);
            }
        }
        
        private Dictionary<string, SyndicateMemberState> ParseBoardState(SyndicatePanel betrayalWindow)
        {
            var boardState = new Dictionary<string, SyndicateMemberState>();
            if (betrayalWindow.SyndicateStates == null) return boardState;

            // Get a list of current leaders' names for quick lookup.
            var leaders = betrayalWindow.SyndicateLeadersData.Leaders.Select(l => l.Target.Name).ToHashSet();

            foreach (var memberState in betrayalWindow.SyndicateStates)
            {
                var memberName = memberState.Target.Name;
                var rank = memberState.Rank.Name;
                var job = memberState.Job.Name;

                if (string.IsNullOrWhiteSpace(memberName)) continue;

                if (Enum.TryParse(job, out SyndicateDivision division) || job == "None")
                {
                    boardState[memberName] = new SyndicateMemberState
                    {
                        Name = memberName,
                        Rank = rank,
                        Division = (job == "None") ? SyndicateDivision.None : division,
                        IsLeader = leaders.Contains(memberName) // Set the leader flag
                    };
                }
            }
            return boardState;
        }

        private bool CanRun()
        {
            if (!Settings.Enable.Value) return false;
            if (GameController.IsLoading) return false;
            return GameController.IngameState.InGame;
        }
    }
}
