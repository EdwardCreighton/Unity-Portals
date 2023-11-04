using UnityEngine;

namespace Services.Camera
{
	public class CameraController : MonoBehaviour
	{
		[SerializeField] private UnityEngine.Camera mainCamera;
		[Space]
		[SerializeField] private Transform followTarget;
		[Space]
		[SerializeField] private float yawSensitivity = 10f;
		[SerializeField] private float pitchSensitivity = 5f;
		[SerializeField] private bool yAxisInvert = true;

		public UnityEngine.Camera MainCamera => mainCamera;

		private Vector2 eulerAngles;

		private void LateUpdate()
		{
			Place();
			Rotate();
		}

		public void SetRotation(Quaternion newRotation)
		{
			mainCamera.transform.rotation = newRotation;
			eulerAngles = mainCamera.transform.eulerAngles;
		}

		private void Rotate()
		{
			Vector2 mouseInput = Vector3.zero;

			mouseInput.x = Input.GetAxisRaw("Mouse X");
			mouseInput.y = Input.GetAxisRaw("Mouse Y");
			
			if (mouseInput.sqrMagnitude < 0.01f) return;

			eulerAngles.y += mouseInput.x * yawSensitivity * Time.deltaTime;
			eulerAngles.x += mouseInput.y * pitchSensitivity * Time.deltaTime * (yAxisInvert ? -1f : 1f);
			
			if (eulerAngles.y > 360f)
			{
				eulerAngles.y -= 360f;
			}
			else if (eulerAngles.y < 0f)
			{
				eulerAngles.y += 360f;
			}

			eulerAngles.x = Mathf.Clamp(eulerAngles.x, -85f, 85f);

			mainCamera.transform.localRotation = Quaternion.Euler(eulerAngles.x, eulerAngles.y, 0f);;
		}

		private void Place()
		{
			mainCamera.transform.position = followTarget.position;
		}
	}
}