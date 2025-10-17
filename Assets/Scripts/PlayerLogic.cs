using UnityEngine;

public class PlayerLogic : MonoBehaviour
{
	[SerializeField] private Transform[] yarnSlots;
	[SerializeField] private KeyCode deployKey = KeyCode.E;

	private bool IsSlotOccupied(Transform slot)
	{
		return slot != null && slot.childCount > 0;
	}

	private int GetFirstFreeSlotIndex()
	{
		for (int i = 0; i < yarnSlots.Length; i++)
		{
			if (yarnSlots[i] != null && !IsSlotOccupied(yarnSlots[i]))
			{
				return i;
			}
		}
		return -1;
	}

	private void TryPickupYarn(GameObject yarn)
	{
		if (yarn == null) return;

		// Update score and time
		if (GameMaster.Instance != null)
		{
			GameMaster.Instance.AddScore(1);
			GameMaster.Instance.OneShotAudioEvent(GameMaster.Instance.collectEvent);
		}

		int slotIndex = GetFirstFreeSlotIndex();
		if (slotIndex >= 0)
		{
			Transform slot = yarnSlots[slotIndex];
			yarn.transform.SetParent(slot, worldPositionStays: true);
			yarn.transform.position = slot.position;
			yarn.transform.rotation = slot.rotation;
			// Disable physics on the picked yarn
			var rb = yarn.GetComponent<Rigidbody>();
			if (rb != null)
			{
				rb.velocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
				rb.isKinematic = true;
				rb.detectCollisions = false;
			}
			var colliders = yarn.GetComponentsInChildren<Collider>(true);
			for (int i = 0; i < colliders.Length; i++)
			{
				colliders[i].enabled = false;
			}
		}
		else
		{
			// No free slots; destroy the yarn
			Destroy(yarn);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Yarn"))
		{
			TryPickupYarn(other.gameObject);
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (collision.collider.CompareTag("Yarn"))
		{
			TryPickupYarn(collision.collider.gameObject);
		}
	}

	void Update()
	{
		if (Input.GetKeyDown(deployKey))
		{
			DeployDecoy();
		}
	}

	private void DeployDecoy()
	{
		int carriedCount = 0;
		for (int i = 0; i < yarnSlots.Length; i++)
		{
			if (yarnSlots[i] != null)
			{
				carriedCount += yarnSlots[i].childCount;
			}
		}
		if (carriedCount < 3) return; // nothing to deploy

		// Destroy up to the last three yarns, starting from the last slot backwards
		int removed = 0;
		for (int i = yarnSlots.Length - 1; i >= 0 && removed < 3; i--)
		{
			if (yarnSlots[i] == null) continue;
			for (int c = yarnSlots[i].childCount - 1; c >= 0 && removed < 3; c--)
			{
				Destroy(yarnSlots[i].GetChild(c).gameObject);
				removed++;
			}
		}

		// Spawn decoy via GameMaster
		if (GameMaster.Instance != null)
		{
			GameMaster.Instance.SpawnDecoy(transform.position);
		}
	}
}


