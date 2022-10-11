using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundCalibrator : MonoBehaviour {

	public GameObject StartPanel;
	// Use this for initialization
	public void Setup (int setNumber) {
		//If not training set, deactivate start panel and enable sound calibration
		if ( setNumber > 0 ){
			StartPanel.SetActive(false);
		}
		else{
			FinishCalibration();
		}
	}

	public void FinishCalibration(){
		gameObject.SetActive(false);
		StartPanel.SetActive(true);
	}
}
