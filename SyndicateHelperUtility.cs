// SyndicateHelperUtility.cs
// Shared utility classes and constants for the SyndicateHelper plugin.
// Contains helper methods for parsing goals, drawing Bezier curves, and safe element access.

using System;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Elements;

namespace SyndicateHelper
{
    public static class SyndicateHelperConstants
    {
        public const int ButtonPadding = 10;
        public const int ButtonVerticalPadding = 5;
        public const int ButtonHorizontalSpacing = 10;
        public const int VerticalSpacing = 10;
        public const int LineSpacing = 20;
        public const int GoalLineSpacing = 25;
        public const int DebugLineSpacing = 20;

        public const int DefaultFontSize = 20;

        public const int MouseClickDebounceMs = 200;

        public const int TextPadding = 2;
        public const int ScoreTextOffsetX = 5;
        public const int ScoreTextOffsetY = 5;
        public const int GoalFrameBorderPadding = 2;

        public const byte DefaultBackgroundAlpha = 166;
        public const byte ButtonBackgroundAlpha = 150;

        public const int MaxPrisonSlots = 3;

        public const int PenaltyLeaderInterrogation = -200;
        public const int PenaltyPrisonFull = -100;
        public const int BaseInterrogateScore = 10;
        public const int InterrogateRankMultiplier = 10;
        public const int ScoreGainItemMap = 20;
        public const int ScoreGainItemVeiledItem = 20;
        public const int ScoreDownrankRivalsUprankMyDivision = 50;
        public const int ScoreStealIntelligence = 20;
        public const int ScoreRemoveNpcNeutral = 40;
        public const int ScoreRemoveNpcWithGoal = -60;

        public const int DefaultDrawPositionX = 100;
        public const int DefaultDrawPositionY = 100;
        public const int DebugDrawPositionX = 100;
        public const int DebugDrawPositionY = 20;

        public const int MaxDebugMessages = 100;

        public const float BezierControlPointOffset = 100f;
        public const float BezierMinimumCurveRadius = 50f;
        public const int BezierSegmentCount = 32;
    }

    public static class SyndicateHelperUtility
    {
        public static MemberGoal ParseGoal(string goal)
        {
            if (string.IsNullOrEmpty(goal) || goal == "None")
                return new MemberGoal { Division = SyndicateDivision.None, IsPrimaryLeader = false };

            var isLeader = goal.Contains("(Leader)");
            var divisionName = goal.Replace(" (Leader)", "").Trim();

            if (System.Enum.TryParse(divisionName, out SyndicateDivision division))
                return new MemberGoal { Division = division, IsPrimaryLeader = isLeader };

            return new MemberGoal { Division = SyndicateDivision.None, IsPrimaryLeader = false };
        }

        public static string GetDesiredDivisionForMember(string memberName, SyndicateHelperSettings settings)
        {
            if (settings == null || string.IsNullOrWhiteSpace(memberName))
                return "None";

            var property = settings.GetType().GetProperty(memberName);
            if (property == null)
                return "None";

            var listNode = property.GetValue(settings) as ExileCore.Shared.Nodes.ListNode;
            return listNode?.Value ?? "None";
        }

        public static bool IsValidSyndicateMember(string name, System.Collections.Generic.HashSet<string> memberNames)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return memberNames.Contains(name);
        }

        public static void DrawBezierCurve(
            System.Numerics.Vector2 start,
            System.Numerics.Vector2 end,
            float thickness,
            SharpDX.Color color,
            Action<System.Numerics.Vector2, System.Numerics.Vector2, float, SharpDX.Color> drawLineAction)
        {
            var horizontalDistance = System.MathF.Abs(end.X - start.X);
            var verticalDistance = System.MathF.Abs(end.Y - start.Y);

            var controlOffset = System.MathF.Max(horizontalDistance * 0.5f, SyndicateHelperConstants.BezierControlPointOffset);
            controlOffset = System.MathF.Max(controlOffset, SyndicateHelperConstants.BezierMinimumCurveRadius);

            var cp1 = new System.Numerics.Vector2(
                start.X + controlOffset,
                start.Y);

            var cp2 = new System.Numerics.Vector2(
                end.X - controlOffset,
                end.Y);

            System.Numerics.Vector2 prevPoint = start;
            for (int i = 1; i <= SyndicateHelperConstants.BezierSegmentCount; i++)
            {
                var t = i / (float)SyndicateHelperConstants.BezierSegmentCount;

                var oneMinusT = 1 - t;
                var oneMinusTCubed = oneMinusT * oneMinusT * oneMinusT;
                var tCubed = t * t * t;

                var x = oneMinusTCubed * start.X +
                        3 * oneMinusT * oneMinusT * t * cp1.X +
                        3 * oneMinusT * t * t * cp2.X +
                        tCubed * end.X;

                var y = oneMinusTCubed * start.Y +
                        3 * oneMinusT * oneMinusT * t * cp1.Y +
                        3 * oneMinusT * t * t * cp2.Y +
                        tCubed * end.Y;

                var currentPoint = new System.Numerics.Vector2(x, y);
                drawLineAction(prevPoint, currentPoint, thickness, color);
                prevPoint = currentPoint;
            }
        }

        public static string GetElementTextSafely(Element element)
        {
            return element?.Text ?? string.Empty;
        }
    }
}
