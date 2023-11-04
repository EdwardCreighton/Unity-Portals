using UnityEngine;
using Services.Camera;
using Gameplay.Player;

namespace Infrastructure
{
	public class LevelController : MonoBehaviour
	{
		[SerializeField] private CameraController cameraController;
		[SerializeField] private PlayerController playerController;
		
		public CameraController CameraController => cameraController;

		public PlayerController PlayerController => playerController;

		private void Start()
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
	}
}