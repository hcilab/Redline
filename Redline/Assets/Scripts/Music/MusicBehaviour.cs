using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicBehaviour : MonoBehaviour {
	
	public AudioClip calmMusic;
	public AudioClip tenseMusic;
	private AudioSource _audioSource;
	//private AudioClip[] clips;

	private void Awake()
    {
		_audioSource = GetComponent<AudioSource>();
    }
	void Start()
    {
		//clips = Resources.LoadAll<AudioClip>("Music");
	}
	// Use this for initialization
	public void PlayMusic()
	{
		if (_audioSource.isPlaying) return;
		_audioSource.Play();
	}
	
	// Update is called once per frame
	public void StopMusic()
	{
		_audioSource.Stop();
	}

	public void MuffleMusic()
    {

    }

	public void UnMuffleMusic()
    {

    }

	public void ChangeSong(uint songIndex)
    {
		if(songIndex < 0 || songIndex > 1) return;
		StopMusic();
		if(songIndex == 0){
			_audioSource.clip = calmMusic;
		}
		else if(songIndex == 1){
			_audioSource.clip = tenseMusic;
		}
		//_audioSource.clip = clips[songIndex];
		PlayMusic();
	}
}
