using UnityEngine;
using SunlessReach.Core;

namespace SunlessReach.Pickups
{
    public class TreasurePickup : MonoBehaviour
    {
        [SerializeField] private int goldAmount = 50;
        [SerializeField] private AudioClip pickupClip;

        private bool _collected;

        private void OnTriggerEnter(Collider other)
        {
            if (_collected) return;
            if (!other.CompareTag("Player")) return;

            _collected = true;

            var gameState = FindAnyObjectByType<GameManager>()?.GameState;
            if (gameState != null)
            {
                gameState.AddGold(goldAmount);
                gameState.secretsCollected++;
            }

            UiSfx.Play(pickupClip);
            Destroy(gameObject);
        }
    }
}
