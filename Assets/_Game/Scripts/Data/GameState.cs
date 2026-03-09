using System.Collections.Generic;
using UnityEngine;

namespace SunlessReach.Data
{
    [CreateAssetMenu(fileName = "GameState", menuName = "SunlessReach/GameState")]
    public class GameState : ScriptableObject
    {
        [Header("UI")]
        public AudioClip uiClickClip;
        public AudioClip uiScrollClip;

        [Header("Fast Travel")]
        public List<string> unlockedWarps = new List<string>();

        public bool IsWarpUnlocked(string id) => !string.IsNullOrEmpty(id) && unlockedWarps.Contains(id);

        public void UnlockWarp(string id)
        {
            if (string.IsNullOrEmpty(id) || unlockedWarps.Contains(id)) return;
            unlockedWarps.Add(id);
        }

        [Header("Health")]
        public int maxHearts = 5;
        public int currentHearts = 5;

        [Header("Souls")]
        public int currentSouls = 0;
        public int maxSouls = 9;
        public int soulsPerHeal = 3;

        [Header("Gold")]
        public int currentGold = 0;

        [Header("Combat")]
        public int attackDamage = 1;

        [Header("Abilities")]
        public bool hasDash = false;
        public bool hasDoubleJump = false;

        [Header("Checkpoint")]
        public Vector3 lastCheckpointPosition = Vector3.zero;

        [Header("Shop")]
        public int heartShardsOwned = 0;
        public bool hasSharpenedBlade = false;
        public bool hasSoulVessel = false;

        [Header("Secrets")]
        public int secretsCollected = 0;
        public int secretsTotal = 5;

        [Header("Enemies")]
        public int enemiesDefeated = 0;
        public int enemiesTotal = 1;   // set at scene start by GameManager

        [Header("Run Stats")]
        public int deathCount = 0;
        public float playTime = 0f;

        public void ResetToDefaults()
        {
            unlockedWarps = new List<string> { "spawn" };
            enemiesDefeated = 0;
            deathCount = 0;
            playTime = 0f;
            maxHearts = 5;
            currentHearts = 5;
            currentSouls = 0;
            maxSouls = 9;
            currentGold = 0;
            soulsPerHeal = 3;
            attackDamage = 1;
            hasDash = false;
            hasDoubleJump = false;
            lastCheckpointPosition = Vector3.zero;
            heartShardsOwned = 0;
            hasSharpenedBlade = false;
            hasSoulVessel = false;
            secretsCollected = 0;
        }

        public void AddGold(int amount)
        {
            currentGold += amount;
            Core.EventBus.RaiseMoneyChanged(currentGold);
        }

        public bool TrySpendGold(int amount)
        {
            if (currentGold < amount) return false;
            currentGold -= amount;
            Core.EventBus.RaiseMoneyChanged(currentGold);
            return true;
        }

        public void AddSouls(int amount)
        {
            currentSouls = Mathf.Min(currentSouls + amount, maxSouls);
            Core.EventBus.RaiseSoulsChanged(currentSouls, maxSouls);
        }

        public bool TryHeal()
        {
            if (currentSouls < soulsPerHeal || currentHearts >= maxHearts) return false;
            currentSouls -= soulsPerHeal;
            currentHearts = Mathf.Min(currentHearts + 1, maxHearts);
            Core.EventBus.RaiseSoulsChanged(currentSouls, maxSouls);
            Core.EventBus.RaiseHealthChanged(currentHearts, maxHearts);
            return true;
        }
    }
}
