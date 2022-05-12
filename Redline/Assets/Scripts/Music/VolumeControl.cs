using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class VolumeControl : MonoBehaviour {

	public AudioMixer _mixer;

	//Change music volume
	public void SetLevel (float sliderValue)
    {
		_mixer.SetFloat("MusicVolume", Mathf.Log10(sliderValue)*20);
    }
}
