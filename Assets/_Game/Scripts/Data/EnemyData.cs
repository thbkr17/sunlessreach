using UnityEngine;

namespace SunlessReach.Data
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "SunlessReach/EnemyData")]
    public class EnemyData : ScriptableObject
    {
        public string enemyName = "Enemy";
        public int maxHealth = 3;
        public int contactDamage = 1;
        public float moveSpeed = 3f;
        public float detectionRange = 8f;
        public float attackRange = 1.5f;

        [Header("Knockback")]
        public float knockbackForce = 5f;
        public float knockbackDuration = 0.2f;
    }
}
