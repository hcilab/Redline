using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NarrativeController : MonoBehaviour {
	private bool _narrativeCompmlete = false;

	private void Update()
	{
		if ( gameObject.activeSelf && Input.anyKeyDown && _narrativeCompmlete )
			FindObjectOfType< MainMenuController >().StartGame();
	}

	public void OnNarrativeComplete()
	{
		_narrativeCompmlete = true;
	}
}
