using UnityEngine;

namespace Gameplay.Portals
{
    public class PortalTrigger : MonoBehaviour
    {
        [SerializeField] private Portal portal;
        [SerializeField] private PortalRenderer portalRenderer;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                portalRenderer.Active = true;
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                portalRenderer.Active = false;
            }
        }
    }
}