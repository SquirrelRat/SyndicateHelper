using System.Collections.Generic;

namespace SyndicateHelper
{
    // --- SHARED DATA DEFINITIONS ---
    // By placing these here, they are available to all other files in the SyndicateHelper namespace.

    public enum SyndicateDivision { None, Transportation, Fortification, Research, Intervention }
    public enum RewardTier { Great, Good, Average, Worst }
    public class RewardInfo { public string Text { get; set; } public RewardTier Tier { get; set; } }
    public class SyndicateMemberState { public string Name { get; set; } public string Rank { get; set; } public SyndicateDivision Division { get; set; } public bool IsLeader { get; set; } }
    public struct MemberGoal { public SyndicateDivision Division; public bool IsPrimaryLeader; }

    // --- REWARD DATA ---
    public static class SyndicateRewardData
    {
        public static readonly Dictionary<string, Dictionary<SyndicateDivision, RewardInfo>> Rewards = new()
        {
            ["Aisling"] = new Dictionary<SyndicateDivision, RewardInfo>
            {
                [SyndicateDivision.Transportation] = new() { Text = "Dbl-Veiled Items",   Tier = RewardTier.Good },
                [SyndicateDivision.Fortification]  = new() { Text = "Veiled Exalt",       Tier = RewardTier.Worst },
                [SyndicateDivision.Research]       = new() { Text = "Veiled Chaos",       Tier = RewardTier.Great },
                [SyndicateDivision.Intervention]   = new() { Text = "Torment Scarabs",    Tier = RewardTier.Good }
            },
            ["Cameria"] = new Dictionary<SyndicateDivision, RewardInfo>
            {
                [SyndicateDivision.Transportation] = new() { Text = "Abyss Scarabs",      Tier = RewardTier.Good },
                [SyndicateDivision.Fortification]  = new() { Text = "Jewel Chest",        Tier = RewardTier.Worst },
                [SyndicateDivision.Research]       = new() { Text = "Jewel Craft",        Tier = RewardTier.Average },
                [SyndicateDivision.Intervention]   = new() { Text = "Delirium Scarabs",   Tier = RewardTier.Great }
            },
            ["Elreon"] = new Dictionary<SyndicateDivision, RewardInfo>
            {
                [SyndicateDivision.Transportation] = new() { Text = "Fragments",          Tier = RewardTier.Average },
                [SyndicateDivision.Fortification]  = new() { Text = "Corrupt Equip.",     Tier = RewardTier.Worst },
                [SyndicateDivision.Research]       = new() { Text = "Tainted Craft",      Tier = RewardTier.Average },
                [SyndicateDivision.Intervention]   = new() { Text = "Beyond Scarabs",     Tier = RewardTier.Good }
            },
            ["Gravicius"] = new Dictionary<SyndicateDivision, RewardInfo>
            {
                [SyndicateDivision.Transportation] = new() { Text = "Div Cards Stack",    Tier = RewardTier.Good },
                [SyndicateDivision.Fortification]  = new() { Text = "Div Card Chest",     Tier = RewardTier.Worst },
                [SyndicateDivision.Research]       = new() { Text = "Swap Div Card",      Tier = RewardTier.Worst },
                [SyndicateDivision.Intervention]   = new() { Text = "Divination Scarabs", Tier = RewardTier.Good }
            },
            ["Guff"] = new Dictionary<SyndicateDivision, RewardInfo>
            {
                [SyndicateDivision.Transportation] = new() { Text = "Misc. Currency",     Tier = RewardTier.Average },
                [SyndicateDivision.Fortification]  = new() { Text = "Rare Equip.",        Tier = RewardTier.Worst },
                [SyndicateDivision.Research]       = new() { Text = "Misc. Craft",        Tier = RewardTier.Good },
                [SyndicateDivision.Intervention]   = new() { Text = "Blight Scarabs",     Tier = RewardTier.Good }
            },
            ["Haku"] = new Dictionary<SyndicateDivision, RewardInfo>
            {
                [SyndicateDivision.Transportation] = new() { Text = "Unique Strongbox",   Tier = RewardTier.Average },
                [SyndicateDivision.Fortification]  = new() { Text = "Domination Scarabs", Tier = RewardTier.Average },
                [SyndicateDivision.Research]       = new() { Text = "Influence Craft",    Tier = RewardTier.Good },
                [SyndicateDivision.Intervention]   = new() { Text = "Ambush Scarabs",     Tier = RewardTier.Good }
            },
            ["Hillock"] = new Dictionary<SyndicateDivision, RewardInfo>
            {
                [SyndicateDivision.Transportation] = new() { Text = "Influenced Equip.",  Tier = RewardTier.Worst },
                [SyndicateDivision.Fortification]  = new() { Text = "30% Quality",        Tier = RewardTier.Great },
                [SyndicateDivision.Research]       = new() { Text = "Eldritch Implicit",  Tier = RewardTier.Average },
                [SyndicateDivision.Intervention]   = new() { Text = "Influenced Scarabs", Tier = RewardTier.Good }
            },
            ["It That Fled"] = new Dictionary<SyndicateDivision, RewardInfo>
            {
                [SyndicateDivision.Transportation] = new() { Text = "Corrupt Maps",       Tier = RewardTier.Average },
                [SyndicateDivision.Fortification]  = new() { Text = "Breachstone Bargain",Tier = RewardTier.Worst },
                [SyndicateDivision.Research]       = new() { Text = "Tainted Sockets",    Tier = RewardTier.Good },
                [SyndicateDivision.Intervention]   = new() { Text = "Breach Scarabs",     Tier = RewardTier.Good }
            },
            ["Janus"] = new Dictionary<SyndicateDivision, RewardInfo>
            {
                [SyndicateDivision.Transportation] = new() { Text = "Gold Piles",         Tier = RewardTier.Worst },
                [SyndicateDivision.Fortification]  = new() { Text = "Kaguuran Scarabs",   Tier = RewardTier.Worst },
                [SyndicateDivision.Research]       = new() { Text = "Cadiro's Offer",     Tier = RewardTier.Great },
                [SyndicateDivision.Intervention]   = new() { Text = "Expedition Scarabs", Tier = RewardTier.Good }
            },
            ["Jorgin"] = new Dictionary<SyndicateDivision, RewardInfo>
            {
                [SyndicateDivision.Transportation] = new() { Text = "Sulphite Scarabs",   Tier = RewardTier.Good },
                [SyndicateDivision.Fortification]  = new() { Text = "Delve Equip.",       Tier = RewardTier.Worst },
                [SyndicateDivision.Research]       = new() { Text = "Talisman Craft",     Tier = RewardTier.Good },
                [SyndicateDivision.Intervention]   = new() { Text = "Bestiary Scarabs",   Tier = RewardTier.Great }
            },
            ["Korell"] = new Dictionary<SyndicateDivision, RewardInfo>
            {
                [SyndicateDivision.Transportation] = new() { Text = "Anarchy Scarabs",    Tier = RewardTier.Average },
                [SyndicateDivision.Fortification]  = new() { Text = "Essence Equip.",     Tier = RewardTier.Worst },
                [SyndicateDivision.Research]       = new() { Text = "Essence Craft",      Tier = RewardTier.Good },
                [SyndicateDivision.Intervention]   = new() { Text = "Essence Scarabs",    Tier = RewardTier.Good }
            },
            ["Leo"] = new Dictionary<SyndicateDivision, RewardInfo>
            {
                [SyndicateDivision.Transportation] = new() { Text = "Incursion Scarabs",  Tier = RewardTier.Average },
                [SyndicateDivision.Fortification]  = new() { Text = "Corrupt Unique",     Tier = RewardTier.Worst },
                [SyndicateDivision.Research]       = new() { Text = "Djinn Baal Orb",     Tier = RewardTier.Good },
                [SyndicateDivision.Intervention]   = new() { Text = "Ultimatum Scarabs",  Tier = RewardTier.Great }
            },
            ["Riker"] = new Dictionary<SyndicateDivision, RewardInfo>
            {
                [SyndicateDivision.Transportation] = new() { Text = "Unique Items",       Tier = RewardTier.Average },
                [SyndicateDivision.Fortification]  = new() { Text = "Unique Item Chest",  Tier = RewardTier.Worst },
                [SyndicateDivision.Research]       = new() { Text = "Ancient Orb",        Tier = RewardTier.Good },
                [SyndicateDivision.Intervention]   = new() { Text = "Titanic Scarabs",    Tier = RewardTier.Good }
            },
            ["Rin"] = new Dictionary<SyndicateDivision, RewardInfo>
            {
                [SyndicateDivision.Transportation] = new() { Text = "Map Currency",       Tier = RewardTier.Average },
                [SyndicateDivision.Fortification]  = new() { Text = "Unique Map Chest",   Tier = RewardTier.Worst },
                [SyndicateDivision.Research]       = new() { Text = "Map Craft",          Tier = RewardTier.Average },
                [SyndicateDivision.Intervention]   = new() { Text = "Cartography Scarabs",Tier = RewardTier.Great }
            },
            ["Tora"] = new Dictionary<SyndicateDivision, RewardInfo>
            {
                [SyndicateDivision.Transportation] = new() { Text = "Quality Gems",       Tier = RewardTier.Average },
                [SyndicateDivision.Fortification]  = new() { Text = "Gem Chest",          Tier = RewardTier.Worst },
                [SyndicateDivision.Research]       = new() { Text = "Gem Craft",          Tier = RewardTier.Good },
                [SyndicateDivision.Intervention]   = new() { Text = "Ritual Scarabs",     Tier = RewardTier.Great }
            },
            ["Vagan"] = new Dictionary<SyndicateDivision, RewardInfo>
            {
                [SyndicateDivision.Transportation] = new() { Text = "Harbinger Scarabs",  Tier = RewardTier.Good },
                [SyndicateDivision.Fortification]  = new() { Text = "Incubators",         Tier = RewardTier.Worst },
                [SyndicateDivision.Research]       = new() { Text = "Chaos/Fracture",     Tier = RewardTier.Average },
                [SyndicateDivision.Intervention]   = new() { Text = "Legion Scarabs",     Tier = RewardTier.Great }
            },
            ["Vorici"] = new Dictionary<SyndicateDivision, RewardInfo>
            {
                [SyndicateDivision.Transportation] = new() { Text = "Stack of Currency",  Tier = RewardTier.Average },
                [SyndicateDivision.Fortification]  = new() { Text = "Socket Crafts",      Tier = RewardTier.Average },
                [SyndicateDivision.Research]       = new() { Text = "Socket Color",       Tier = RewardTier.Great },
                [SyndicateDivision.Intervention]   = new() { Text = "Harvest Scarabs",    Tier = RewardTier.Great }
            }
        };
    }
}
