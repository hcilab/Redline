using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicBehaviour : MonoBehaviour {
	
	private AudioSource _audioSource;
	private AudioClip[] clips;

	private void Awake()
    {
		_audioSource = GetComponent<AudioSource>();
    }
	void Start()
    {
		clips = Resources.LoadAll<AudioClip>("Music");
	}
	// Use this for initialization
	public void PlayMusic()
	{
		if (_audioSource.isPlaying) return;
		_audioSource.Play();
	}

	void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
			ChangeSong(0);
        }
		else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
			ChangeSong(1);
        }
		else if (Input.GetKeyDown(KeyCode.Alpha3))
		{
			ChangeSong(2);
		}
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

	private void ChangeSong(uint songIndex)
    {
		_audioSource.Stop();
		_audioSource.clip = clips[songIndex];
		_audioSource.Play();
	}
}
