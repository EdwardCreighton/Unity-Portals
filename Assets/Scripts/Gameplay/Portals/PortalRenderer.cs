using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Gameplay.Portals
{
    public class PortalRenderer : MonoBehaviour
    {
        [Serializable]
        private class LOD
        {
            [SerializeField] private float distance = 5f;
            [SerializeField] private RenderTexture renderTexture;

            public bool CheckDistance(float normDistanceToPortal)
            {
                return normDistanceToPortal < distance;
            }
            
            public void SetLODLevel(ref RenderTexture rt)
            {
                rt = renderTexture;
            }
        }
        
        private const float MeshTypeThresholdDistance = 3f;

        [SerializeField] private PortalsConnector portalsConnector;
        [field:SerializeField] public bool Active { get; set; } = true;
        
        [SerializeField] protected Camera renderCamera;
        [Space]
        [SerializeField] private Transform portalMeshHolder;
        [SerializeField] private MeshRenderer flatMeshRenderer;
        [SerializeField] private MeshRenderer volumetricMeshRenderer;
        [Space]
        [SerializeField] private List<LOD> lods;
        
        
        public Portal SelfPortal { get; set; }
        public Portal OtherPortal { get; set; }
        public PortalRenderer OtherPortalRenderer { get; set; }
        
        private bool _isVisible;
        private Vector3 _distanceToPortalCamera;
        
        private float _fov;
        private float _lastEyeSeparation;
        private Matrix4x4 _projectionMatrix;
        private Matrix4x4 _viewMatrix;
        private Quaternion _rotation;
        
        private Camera _playerCamera;
        private RenderTexture _activeRenderTexture;
        private LOD _currentLOD;
        
        private void Start()
        {
            _playerCamera = portalsConnector.LevelController.CameraController.MainCamera;
            
            RenderPipelineManager.beginCameraRendering += InitialRender;
            RenderPipelineManager.beginCameraRendering += Render;
        }

        private void InitialRender(ScriptableRenderContext context, UnityEngine.Camera queuedCamera)
        {
            if (!renderCamera)
                return;
            
            if (queuedCamera != _playerCamera)
                return;

            HandleLOD(1000f);
            HandleMeshRenderer(1000f);
            
            ComputeDistanceToPortalCamera();

            HandleLOD(_distanceToPortalCamera.magnitude);
            HandleMeshRenderer(_distanceToPortalCamera.magnitude);
            
            float normDistanceToPortal = Vector3.Dot(_distanceToPortalCamera, portalMeshHolder.transform.forward);
            renderCamera.nearClipPlane = Mathf.Abs(normDistanceToPortal);
            
            ComputeProjectionMatrix();
            
            SetTexture();

            UniversalRenderPipeline.RenderSingleCamera(context, renderCamera);
            RenderPipelineManager.beginCameraRendering -= InitialRender;
        }
        
        private void Render(ScriptableRenderContext context, Camera queuedCamera)
        {
	        if (!renderCamera)
                return;

	        if (!Active)
                return;
	        
	        if (queuedCamera != _playerCamera)
		        return;

            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(_playerCamera);
            _isVisible = GeometryUtility.TestPlanesAABB(frustumPlanes, flatMeshRenderer.bounds); // Portals are not movable!

            if (!_isVisible)
                return;
            
            renderCamera.ResetProjectionMatrix();
            renderCamera.ResetCullingMatrix();

            PlaceAndRotateCamera();
            ComputeDistanceToPortalCamera();
            HandleLOD(_distanceToPortalCamera.magnitude);
            HandleMeshRenderer(_distanceToPortalCamera.magnitude);

            if (flatMeshRenderer.gameObject.activeSelf)
            {
	            float normDistanceToPortal = Vector3.Dot(_distanceToPortalCamera, portalMeshHolder.transform.forward);
	            renderCamera.nearClipPlane = Math.Abs(normDistanceToPortal);
	            
	            ComputeProjectionMatrix();
            }
            else
            {
	            Vector3 playerCamPosCached = _playerCamera.transform.position;
	            Quaternion playerCamRotCached = _playerCamera.transform.rotation;

	            _playerCamera.transform.position = renderCamera.transform.position;
	            _playerCamera.transform.rotation = renderCamera.transform.rotation;

	            renderCamera.projectionMatrix = new Matrix4x4(
		            _playerCamera.projectionMatrix.GetColumn(0),
		            _playerCamera.projectionMatrix.GetColumn(1),
		            _playerCamera.projectionMatrix.GetColumn(2),
		            _playerCamera.projectionMatrix.GetColumn(3));
	            
	            float normDistanceToPortal = Vector3.Dot(_distanceToPortalCamera, portalMeshHolder.transform.forward);
	            
	            renderCamera.nearClipPlane = Math.Abs(normDistanceToPortal);

	            renderCamera.cullingMatrix = new Matrix4x4(
		            _playerCamera.cullingMatrix.GetColumn(0),
		            _playerCamera.cullingMatrix.GetColumn(1),
		            _playerCamera.cullingMatrix.GetColumn(2),
		            _playerCamera.cullingMatrix.GetColumn(3));

	            _playerCamera.transform.position = playerCamPosCached;
	            _playerCamera.transform.rotation = playerCamRotCached;
            }
            
            SetTexture();

            UniversalRenderPipeline.RenderSingleCamera(context, renderCamera);
        }
        
        private void HandleLOD(float distance)
        {
            if (lods.Count == 0) return;
            
            int targetIndex = 0;
            
            for (int i = 0; i < lods.Count; i++)
            {
                if (!lods[i].CheckDistance(distance))
                {
                    if (i == 0)
                    {
                        targetIndex = 0;
                        break;
                    }
                    
                    continue;
                }

                targetIndex = i;
            }
            
            if (_currentLOD != lods[targetIndex])
            {
                _currentLOD = lods[targetIndex];
                _currentLOD.SetLODLevel(ref _activeRenderTexture);
            }
        }

        private void HandleMeshRenderer(float distanceToPortal)
        {
            if (distanceToPortal > MeshTypeThresholdDistance && !flatMeshRenderer.gameObject.activeSelf)
            {
                flatMeshRenderer.gameObject.SetActive(true);
                volumetricMeshRenderer.gameObject.SetActive(false);
            }
            else if (distanceToPortal <= MeshTypeThresholdDistance && !volumetricMeshRenderer.gameObject.activeSelf)
            {
                volumetricMeshRenderer.gameObject.SetActive(true);
                flatMeshRenderer.gameObject.SetActive(false);
            }
        }

        private void PlaceAndRotateCamera()
        {
	        Matrix4x4 localToWorldMatrix = OtherPortal.transform.localToWorldMatrix * transform.worldToLocalMatrix * _playerCamera.transform.localToWorldMatrix;
	        Vector3 renderPosition = localToWorldMatrix.GetColumn(3);
	        Quaternion renderRotation = localToWorldMatrix.rotation;
	        
	        renderCamera.transform.SetPositionAndRotation(renderPosition, renderRotation);
	        renderCamera.ResetWorldToCameraMatrix();
        }

        // Algorithm I found online. Unfortunately, I don't remember the author's name of this part
        private void ComputeProjectionMatrix()
        {
	        Transform windowTransform = OtherPortalRenderer.flatMeshRenderer.transform;

	        Vector3[] quadCorners = new Vector3[4];
	        
	        Vector3 toPortalCamDirection = renderCamera.transform.position - windowTransform.position;
			
			if (Vector3.Dot(toPortalCamDirection, windowTransform.forward) < 0f)
			{
				quadCorners[0] = windowTransform.TransformPoint( -0.5f, -0.5f, 0 );
				quadCorners[1] = windowTransform.TransformPoint( -0.5f, 0.5f, 0 );
				quadCorners[2] = windowTransform.TransformPoint( 0.5f, 0.5f, 0 );
				quadCorners[3] = windowTransform.TransformPoint( 0.5f, -0.5f, 0 );
			}
			else
			{
				quadCorners[0] = windowTransform.TransformPoint( 0.5f, -0.5f, 0 );
				quadCorners[1] = windowTransform.TransformPoint( 0.5f, 0.5f, 0 );
				quadCorners[2] = windowTransform.TransformPoint( -0.5f, 0.5f, 0 );
				quadCorners[3] = windowTransform.TransformPoint( -0.5f, -0.5f, 0 );
			}
			
			if (renderCamera.stereoEnabled && renderCamera.stereoTargetEye == StereoTargetEyeMask.Both)
			{
				Vector3 camForward = windowTransform.position - renderCamera.transform.position;
        	    float windowDist = camForward.magnitude;
        	    camForward /= windowDist; // Normalize
        	    Vector3 camUp = Vector3.Cross( camForward, windowTransform.right ).normalized;
        	    Vector3 camRight = Vector3.Cross( windowTransform.up, camForward ).normalized;
        	    Vector3 eyeOffset = camRight * (renderCamera.stereoSeparation * 0.5f);
        	    ComputeOffAxisPerspective( quadCorners, renderCamera.transform.position - eyeOffset, renderCamera.nearClipPlane, renderCamera.farClipPlane, renderCamera.aspect, ref _projectionMatrix, ref _viewMatrix, ref _rotation, ref _fov );
                renderCamera.SetStereoViewMatrix( Camera.StereoscopicEye.Left, _viewMatrix );
                renderCamera.SetStereoProjectionMatrix( Camera.StereoscopicEye.Left, _projectionMatrix );
        	    ComputeOffAxisPerspective( quadCorners, renderCamera.transform.position + eyeOffset, renderCamera.nearClipPlane, renderCamera.farClipPlane, renderCamera.aspect, ref _projectionMatrix, ref _viewMatrix, ref _rotation, ref _fov );
                renderCamera.SetStereoViewMatrix( Camera.StereoscopicEye.Right, _viewMatrix );
                renderCamera.SetStereoProjectionMatrix( Camera.StereoscopicEye.Right, _projectionMatrix );
        	    ComputeOffAxisPerspective( quadCorners, renderCamera.transform.position, renderCamera.nearClipPlane, renderCamera.farClipPlane, renderCamera.aspect, ref _projectionMatrix, ref _viewMatrix, ref _rotation, ref _fov );
                renderCamera.fieldOfView = _fov;
                renderCamera.stereoConvergence = windowDist; // Mostly for overwriting user input.
                renderCamera.transform.rotation = _rotation;
        	}
			else
			{
        	    ComputeOffAxisPerspective( quadCorners, renderCamera.transform.position, renderCamera.nearClipPlane, renderCamera.farClipPlane, renderCamera.aspect, ref _projectionMatrix, ref _viewMatrix, ref _rotation, ref _fov );
                renderCamera.projectionMatrix = _projectionMatrix;
                renderCamera.nonJitteredProjectionMatrix = _projectionMatrix;
                renderCamera.worldToCameraMatrix = _viewMatrix;
                renderCamera.fieldOfView = _fov;
                renderCamera.transform.rotation = _rotation;
        	}
		}
		
        // Algorithm I found online. Unfortunately, I don't remember the author's name of this part
		private void ComputeOffAxisPerspective(Vector3[] corners, Vector3 eyePos, float near, float far, float aspect, ref Matrix4x4 projectionMatrix, ref Matrix4x4 viewMatrix, ref Quaternion rotation, ref float fov)
		{
			Vector3 pa = corners[0]; // Lower left
			Vector3 pb = corners[3]; // Lower right
			Vector3 pc = corners[1]; // Upper left
	
			Vector3 va; // From pe to pa
			Vector3 vb; // From pe to pb
			Vector3 vc; // From pe to pc
			Vector3 vr; // Right axis of screen
			Vector3 vu; // Up axis of screen
			Vector3 vn; // Normal vector of screen
	
			float l; // Distance to left screen edge
			float r; // Distance to right screen edge
			float b; // Distance to bottom screen edge
			float t; // Distance to top screen edge
			float d; // Distance from eye to screen 
	
			float temp;
	
			vr = pb - pa;
			vu = pc - pa;
			float vrMag = vr.magnitude;
			float vuMag = vu.magnitude;
			vr /= vrMag; // Normalize
			vu /= vuMag; // Normalize
			vn = -Vector3.Cross( vr, vu );
			vn.Normalize();

			va = pa - eyePos;
			vb = pb - eyePos;
			vc = pc - eyePos;
	
			d = -Vector3.Dot( va, vn );
			temp = near / d;
			l = Vector3.Dot( vr, va ) * temp;
			r = Vector3.Dot( vr, vb ) * temp;
			b = Vector3.Dot( vu, va ) * temp;
			t = Vector3.Dot( vu, vc ) * temp;
	
			temp =  1 / (r-l);
			projectionMatrix[0,0] = 2f * near * temp; 
			projectionMatrix[0,1] = 0; 
			projectionMatrix[0,2] = (r+l) * temp; 
			projectionMatrix[0,3] = 0;
	
			temp =  1 / (t-b);
			projectionMatrix[1,0] = 0; 
			projectionMatrix[1,1] = 2f * near * temp; 
			projectionMatrix[1,2] = (t+b) * temp; 
			projectionMatrix[1,3] = 0;
	
			temp =  1 / (near-far);
			projectionMatrix[2,0] = 0;         
			projectionMatrix[2,1] = 0; 
			projectionMatrix[2,2] = (far+near) * temp; 
			projectionMatrix[2,3] = 2f * far * near * temp;
	
			projectionMatrix[3,0] = 0;         
			projectionMatrix[3,1] = 0; 
			projectionMatrix[3,2] = -1;        
			projectionMatrix[3,3] = 0;		
	
			// The original paper puts everything into the projection 
			// matrix (i.e. sets it to p * rm * tm and the other 
			// matrix to the identity), but this doesn't appear to 
			// work with Unity's shadow maps.
	
			viewMatrix[0,0] = vr.x; 
			viewMatrix[0,1] = vr.y; 
			viewMatrix[0,2] = vr.z; 
			viewMatrix[0,3] = ( vr.x * -eyePos.x ) + ( vr.y * -eyePos.y ) + ( vr.z * -eyePos.z );	
	
			viewMatrix[1,0] = vu.x; 
			viewMatrix[1,1] = vu.y; 
			viewMatrix[1,2] = vu.z; 
			viewMatrix[1,3] = ( vu.x * -eyePos.x ) + ( vu.y * -eyePos.y ) + ( vu.z * -eyePos.z );
	
			viewMatrix[2,0] = vn.x; 
			viewMatrix[2,1] = vn.y; 
			viewMatrix[2,2] = vn.z; 
			viewMatrix[2,3] = ( vn.x * -eyePos.x ) + ( vn.y * -eyePos.y ) + ( vn.z * -eyePos.z );
	
			viewMatrix[3,0] = 0;  
			viewMatrix[3,1] = 0;  
			viewMatrix[3,2] = 0;  
			viewMatrix[3,3] = 1;
	
			// Rotation and fov is needed for culling to work.
			rotation.SetLookRotation( ( 0.5f * ( pb + pc ) - eyePos ), vu );
	
			// Set field of view to a conservative estimate to make frustum big enough.
			fov = Mathf.Rad2Deg / aspect * Mathf.Atan( ( vrMag + vuMag ) / va.magnitude );
		}

		private void ComputeDistanceToPortalCamera()
		{
			_distanceToPortalCamera = renderCamera.transform.position - OtherPortalRenderer.flatMeshRenderer.transform.position;
		}

		private void SetTexture()
		{
			renderCamera.targetTexture = _activeRenderTexture;
			flatMeshRenderer.material.mainTexture = _activeRenderTexture;
			volumetricMeshRenderer.material.mainTexture = _activeRenderTexture;
		}
    }
}