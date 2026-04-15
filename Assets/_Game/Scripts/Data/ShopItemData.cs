using UnityEngine;

namespace SunlessReach.Data
{
    public enum ShopItemEffect
    {
        AddMaxHeart,
        AddAttackDamage,
        AddMaxSouls
    }

    [CreateAssetMenu(fileName = "ShopItemData", menuName = "SunlessReach/ShopItemData")]
    public class ShopItemData : ScriptableObject
    {
        public string itemName = "Item";
        [TextArea] public string description = "";
        public int cost = 100;
        public ShopItemEffect effectType;
        public int effectAmount = 1;
        public int maxPurchases = 1;
    }
}
