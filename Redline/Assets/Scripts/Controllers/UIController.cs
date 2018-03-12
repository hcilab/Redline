using System.Collections.Generic;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
	[SerializeField] private GameObject _uiPanel;
	public static UIController Instance = null;
	
	private void Awake()
	{
		if ( Instance == null )
			Instance = this;
		else if ( Instance != this ) 
			Destroy( this );

		DontDestroyOnLoad( Instance );
		SceneManager.sceneLoaded += Initialize;
	}

	private void Initialize( Scene arg0, LoadSceneMode arg1 )
	{
		if ( arg0.name != "mainMenu" )
		{
			_uiPanel.SetActive( true );
			FindObjectOfType<GameMaster>().ResetUi();
		}
		else
		{
			_uiPanel.SetActive( false );
		}
	}
}
