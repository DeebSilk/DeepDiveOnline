using UnityEngine;

public class SurfaceDetector : MonoBehaviour
{
	[Header("Checks")]
	public Transform origin;
	public float groundDistance = 0.3f;
	public float wallDistance = 0.6f;

	public LayerMask surfaceMask;

	[Header("State")]
	public bool isGrounded;
	public bool wallLeft;
	public bool wallRight;

	void Update()
	{
		isGrounded = Physics.Raycast(origin.position, Vector3.down, groundDistance, surfaceMask);

		wallLeft = Physics.Raycast(origin.position, -transform.right, wallDistance, surfaceMask);
		wallRight = Physics.Raycast(origin.position, transform.right, wallDistance, surfaceMask);
	}
}
