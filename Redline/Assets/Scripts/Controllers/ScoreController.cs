using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreController : MonoBehaviour
{


	[SerializeField] private PlayerController player;
	private Text _text;

	// Use this for initialization
	void Start ()
	{
		_text = GetComponentInChildren< Text >();
		_text.text = "Score: 0";
	}
	
	// Update is called once per frame
	void Update ()
	{
		var score = Math.Round(player.GetScore());
		_text.text = "Score: " + score;
	}
}
