using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimerController : MonoBehaviour
{

	private Text _textField;
	private GameMaster _gameMaster;
	
	// Use this for initialization
	void Start ()
	{
		_textField = GameObject.Find( "Timer" ).GetComponent<Text>();
		_gameMaster = FindObjectOfType< GameMaster >();
	}
	
	// Update is called once per frame
	void Update ()
	{
		_textField.text = _gameMaster.GetTimeRemaining();
	}
}
