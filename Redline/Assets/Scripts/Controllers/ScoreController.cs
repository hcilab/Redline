using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScoreController : MonoBehaviour
{


	private PlayerController _player;
	private Text _text;

	// Use this for initialization
	void Awake ()
	{
		_text = GetComponentInChildren< Text >();
		_text.text = "Score: 0";

		SceneManager.sceneLoaded += Initialize;
	}

	private void Initialize( Scene arg0, LoadSceneMode arg1 )
	{
		_player = FindObjectOfType<PlayerController>();
	}

	// Update is called once per frame
	void Update ()
	{
		var score = Math.Round(_player.GetScore());
		_text.text = "Score: " + score;
	}
}
