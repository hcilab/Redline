using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class VolumeControl : MonoBehaviour {

	public AudioMixer _mixer;

	void Start(){
		_mixer.SetFloat("MusicVolume", Mathf.Log10(0.55f)*20);
	}

	//Change music volume
	public void SetLevel (float sliderValue)
    {
		_mixer.SetFloat("MusicVolume", Mathf.Log10(sliderValue)*20);
    }
}
