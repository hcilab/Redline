using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		PlayerController player = FindObjectOfType< PlayerController >();

		Vector3 newPos = player.transform.position;
		newPos.y = gameObject.transform.position.y;
		gameObject.transform.position = newPos;
	}
}
