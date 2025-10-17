using UnityEngine;

public class Decoy : MonoBehaviour
{
	public bool hasBeenHandled = false;

	private Rigidbody cachedRigidbody;
	private Collider[] cachedColliders;

	void Awake()
	{
		cachedRigidbody = GetComponent<Rigidbody>();
		cachedColliders = GetComponentsInChildren<Collider>(true);
	}

	public void DisablePhysics()
	{
		if (cachedRigidbody != null)
		{
			cachedRigidbody.velocity = Vector3.zero;
			cachedRigidbody.angularVelocity = Vector3.zero;
			cachedRigidbody.isKinematic = true;
			cachedRigidbody.detectCollisions = false;
		}
		if (cachedColliders != null)
		{
			for (int i = 0; i < cachedColliders.Length; i++)
			{
				cachedColliders[i].enabled = false;
			}
		}
	}

	public void EnablePhysics()
	{
		if (cachedRigidbody != null)
		{
			cachedRigidbody.isKinematic = false;
			cachedRigidbody.detectCollisions = true;
		}
		if (cachedColliders != null)
		{
			for (int i = 0; i < cachedColliders.Length; i++)
			{
				cachedColliders[i].enabled = true;
			}
		}
	}
}


