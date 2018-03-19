using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartButtonController : MonoBehaviour, IPointerClickHandler
{
	private Button _startButton;
	private string _customLevel;
	private int _sessionID;
	[SerializeField] private GameObject _levelSelectionField;
	[SerializeField] private GameObject _sessionIdField;

	private void Start()
	{
		_levelSelectionField.SetActive( false );
	}

	public void OnPointerClick( PointerEventData eventData )
	{
		if ( eventData.button == PointerEventData.InputButton.Left )
		{
			_customLevel = _levelSelectionField.GetComponent<InputField>().text;
			_sessionID = Int32.Parse(_sessionIdField.GetComponent<InputField>().text);
			FindObjectOfType< GameMaster >().StartGame( _customLevel, _sessionID );
		} else if ( eventData.button == PointerEventData.InputButton.Right )
		{
			Debug.Log( _levelSelectionField  );
			_levelSelectionField.SetActive( true );
			_levelSelectionField.GetComponent<InputField>().ActivateInputField();
		}
	}
}
