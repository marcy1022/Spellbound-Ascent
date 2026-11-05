namespace SpellboundAscent.Utils
{
    public static class Constants
    {
        public const int SCREEN_WIDTH = 1080;
        public const int SCREEN_HEIGHT = 1920;

        public const int MAX_HAND_SIZE = 10;
        public const int BASE_DRAW = 5;
        public const int BASE_ENERGY = 3;

        public const int MAX_THORNS_STACK = 15;
        public const int MAX_POTION_SLOTS = 3;

        public const int MAP_FLOORS_PER_ACT = 15;
        public const int MIN_NODES_PER_FLOOR = 2;
        public const int MAX_NODES_PER_FLOOR = 4;

        public const int TOTAL_ACTS = 3;
    }

    public static class StatusKeys
    {
        public const string Weak = "weak";
        public const string Vulnerable = "vulnerable";
        public const string Burn = "burn";
        public const string Poison = "poison";
        public const string Strength = "strength";
        public const string Dexterity = "dexterity";
        public const string Thorns = "thorns";
        public const string Haste = "haste";
    }

    public static class IntentType
    {
        public const string Attack = "attack";
        public const string Defend = "defend";
        public const string Buff = "buff";
        public const string Debuff = "debuff";
        public const string Special = "special";
    }
}
