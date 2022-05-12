using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventSystemController : MonoBehaviour {

	public static EventSystemController Instance = null;

	public void Awake()
	{
		if (Instance == null)
			Instance = this;
		else if (Instance != this)
			Destroy(this);

		DontDestroyOnLoad(Instance);
	}
}
