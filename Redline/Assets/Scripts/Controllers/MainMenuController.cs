using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{


	[SerializeField] private Text _sessionId;

	[SerializeField] private Button _startButton;

	private void Awake()
	{
		_startButton.interactable = false;
	}

	public void SetSessionId( string id )
	{
		_sessionId.text = id;
		_startButton.interactable = true;
	}
}
