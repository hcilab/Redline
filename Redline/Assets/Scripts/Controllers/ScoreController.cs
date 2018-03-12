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
	void Start ()
	{
		Debug.Log("Starting up score controller");
		_player = FindObjectOfType<PlayerController>();
		_text = GetComponentInChildren< Text >();
		_text.text = "Score: 0";
	}

	// Update is called once per frame
	void Update ()
	{
		var score = Math.Round(_player.GetScore());
		_text.text = "Score: " + score;	
	}
}
