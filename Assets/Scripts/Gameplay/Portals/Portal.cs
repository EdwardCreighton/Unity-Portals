using UnityEngine;

namespace Gameplay.Portals
{
    public class Portal : MonoBehaviour
    {
        [field: SerializeField]
        public bool Active { get; set; } = true;
        public bool PlayerReadyToTeleport { get; set; }
    }
}