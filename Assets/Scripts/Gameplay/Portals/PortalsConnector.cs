using UnityEngine;
using Infrastructure;

namespace Gameplay.Portals
{
    public class PortalsConnector : MonoBehaviour
    {
        [SerializeField] private LevelController levelController;
        [Space]
        [SerializeField] private Portal portalEnter;
        [SerializeField] private PortalRenderer portalEnterRenderer;
        [SerializeField] private Portal portalExit;
        [SerializeField] private PortalRenderer portalExitRenderer;

        public LevelController LevelController => levelController;

        private void Start()
        {
            portalEnterRenderer.SelfPortal = portalEnter;
            portalEnterRenderer.OtherPortal = portalExit;
            portalEnterRenderer.OtherPortalRenderer = portalExitRenderer;
            
            portalExitRenderer.SelfPortal = portalExit;
            portalExitRenderer.OtherPortal = portalEnter;
            portalExitRenderer.OtherPortalRenderer = portalEnterRenderer;
        }

        private void Update()
        {
            if (portalEnter.Active && portalEnter.PlayerReadyToTeleport)
                TryGoForward();
            else if (portalExit.Active && portalExit.PlayerReadyToTeleport)
                TryGoBackwards();
        }

        private void TryGoForward()
        {
            Vector3 localPosition = portalEnter.transform.InverseTransformPoint(levelController.CameraController.MainCamera.transform.position);

            if (localPosition.z > 0f)
            {
                ApplyTeleport(portalEnter.transform, portalExit.transform);
                PostTeleportSetup(portalEnter, portalEnterRenderer, portalExit, portalExitRenderer);
            }
        }

        private void TryGoBackwards()
        {
            Vector3 localPosition = portalExit.transform.InverseTransformPoint(levelController.CameraController.MainCamera.transform.position);

            if (localPosition.z < 0f)
            {
                ApplyTeleport(portalExit.transform, portalEnter.transform);
                PostTeleportSetup(portalExit, portalExitRenderer, portalEnter, portalEnterRenderer);
            }
        }

        private void ApplyTeleport(Transform enterPoint, Transform exitPoint)
        {
            Transform playerCamera = levelController.CameraController.MainCamera.transform;
            
            Quaternion enterPointRot = enterPoint.rotation;
            Quaternion exitPointRot = exitPoint.rotation;

            Quaternion newRot = Quaternion.Inverse(enterPointRot) * playerCamera.transform.rotation;
            newRot = exitPointRot * newRot;
            levelController.CameraController.SetRotation(newRot);
                
            Vector3 newPosition = enterPoint.InverseTransformPoint(levelController.PlayerController.transform.position);
            newPosition = exitPoint.TransformPoint(newPosition);
            levelController.PlayerController.SetPosition(newPosition);
        }

        private void PostTeleportSetup(Portal pEnter, PortalRenderer prEnter, Portal pExit, PortalRenderer prExit)
        {
            pEnter.PlayerReadyToTeleport = false;
            prEnter.Active = false;
            pExit.PlayerReadyToTeleport = true;
            prExit.Active = true;
        }
    }
}
