using UnityEngine;

namespace MoonShadow
{
    public sealed class HazardFloor : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<KyleController2D>(out var player))
                player.FallIntoHazard();
        }
    }
}

