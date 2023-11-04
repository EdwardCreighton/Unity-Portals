using UnityEngine;

namespace Gameplay.Portals
{
    public class PortalEnterTrigger : MonoBehaviour
    {
        [SerializeField] private Portal portal;
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
                portal.PlayerReadyToTeleport = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
                portal.PlayerReadyToTeleport = false;
        }
    }
}