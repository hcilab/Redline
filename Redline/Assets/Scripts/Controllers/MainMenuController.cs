using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{


	[SerializeField] private Text _sessionId;

	[SerializeField] private Text _mTurkId;

	[SerializeField] private Button _startButton;

	[SerializeField] private RectTransform _narrative;
	private string _levelToLoad;

	private void Awake()
	{
		_startButton.interactable = false;
		SceneManager.sceneLoaded += Initialize;
	}

	private void Initialize( Scene arg0, LoadSceneMode arg1 )
	{
		Debug.Log("init called in main menu"  );
		_narrative.gameObject.SetActive( false );
		SetSessionId( FindObjectOfType<GameMaster>().SessionID.ToString() );
	}

	private void Update()
	{
		if( _narrative.gameObject.activeSelf && Input.anyKeyDown )
			FindObjectOfType<GameMaster>().NextLevel( _levelToLoad );
	}

	public void SetSessionId( string id )
	{
		Debug.Log( "Setting session ID" );
		_sessionId.text = id;
		_startButton.interactable = true;
	}

	public void ShowNarrative( string desiredLevel )
	{
		_levelToLoad = desiredLevel;
		_narrative.gameObject.SetActive( true );
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= Initialize;
	}
}
