using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyAfterTimer : MonoBehaviour
{
	[SerializeField] private float time = 1f;
	void Start() => StartCoroutine(DelayedDestruction());
	private IEnumerator DelayedDestruction()
	{
		float t = 0.0f;

		while (t < time)
		{
			t += Time.deltaTime;
			yield return null;
		}

		Destroy(gameObject);
	}
}