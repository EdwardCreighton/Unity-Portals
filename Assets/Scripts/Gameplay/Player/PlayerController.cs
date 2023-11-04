using UnityEngine;
using Infrastructure;

namespace Gameplay.Player
{
	public class PlayerController : MonoBehaviour
	{
		[SerializeField] private LevelController levelController;
		[Space]
		[SerializeField] private CharacterController characterController;
		[SerializeField] private float speed = 3f;

		private void Update()
		{
			Move();
		}

		public void SetPosition(Vector3 newPosition)
		{
			characterController.enabled = false;
			transform.position = newPosition;
			characterController.enabled = true;
		}

		private void Move()
		{
			Vector3 moveInput = Vector3.zero;

			moveInput.x = Input.GetAxisRaw("Horizontal");
			moveInput.z = Input.GetAxisRaw("Vertical");

			Vector3 moveDirectionWorld = levelController.CameraController.MainCamera.transform.TransformDirection(moveInput);
			float magnitude = moveDirectionWorld.magnitude;
			moveDirectionWorld.y = 0f;
			moveDirectionWorld.Normalize();
			moveDirectionWorld *= magnitude;

			moveDirectionWorld *= speed;

			moveDirectionWorld += Vector3.down * 10f;
			
			characterController.Move(moveDirectionWorld * Time.deltaTime);
		}
	}
}