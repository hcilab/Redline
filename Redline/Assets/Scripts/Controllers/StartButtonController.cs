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
	private GameMaster _gameMaster;
	[SerializeField] private GameObject _levelSelectionField;

	private void Awake()
	{
		SceneManager.sceneLoaded += Initialize;
	}

	private void Initialize( Scene arg0, LoadSceneMode arg1 )
	{
		_startButton = GetComponent< Button >();
		_levelSelectionField.SetActive( false );
		_gameMaster = FindObjectOfType< GameMaster >();
	}

	public void OnPointerClick( PointerEventData eventData )
	{
		if ( _startButton.interactable && eventData.button == PointerEventData.InputButton.Left )
		{
			_customLevel = _levelSelectionField.GetComponent<InputField>().text;
			_gameMaster.NextLevel( _customLevel );
		} else if ( eventData.button == PointerEventData.InputButton.Right )
		{
			Debug.Log( _levelSelectionField  );
			_customLevel = _gameMaster.CurrentLevel.ToString();
			_levelSelectionField.GetComponent< InputField >().text = _customLevel;
			_levelSelectionField.SetActive( true );
			_levelSelectionField.GetComponent<InputField>().ActivateInputField();
			_startButton.interactable = true;
		}
	}

	public void ValidateInput()
	{
		var lvlCount = _gameMaster.LevelCount;
		var inputLvl = Int32.Parse( _levelSelectionField.GetComponent< InputField >().text );
		if ( inputLvl > 0 && inputLvl <= lvlCount )
		{
			_startButton.interactable = true;
		}
		else _startButton.interactable = false;
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= Initialize;
	}
}
