using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{


	[SerializeField] private Text _sessionId;

	[SerializeField] private Button _startButton;
	
	private void Awake()
	{
		_startButton.interactable = false;
		SceneManager.sceneLoaded += Initialize;
	}

	private void Initialize( Scene arg0, LoadSceneMode arg1 )
	{
		Debug.Log("init called in main menu"  );
		SetSessionId( FindObjectOfType<GameMaster>().SessionID.ToString() );
	}

	public void SetSessionId( string id )
	{
		Debug.Log( "Setting session ID" );
		_sessionId.text = id;
		_startButton.interactable = true;
	}
}
