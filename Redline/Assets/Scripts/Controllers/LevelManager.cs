using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
	[SerializeField] private PlayerController _player;
	[SerializeField] private FireSystemController _fireSystem;
	private GameMaster _gameMaster;
	
	public PlayerController Player
	{
		get { return _player; }
	}

	public FireSystemController FireSystem
	{
		get { return _fireSystem; }
	}

	public GameMaster GameMaster
	{
		get { return _gameMaster; }
	}
	
	// Use this for initialization
	void Start ()
	{
		SceneManager.sceneLoaded += InitializeLevel;
	}

	private void InitializeLevel( Scene arg0, LoadSceneMode arg1 )
	{
		_gameMaster = FindObjectOfType< GameMaster >();
		
		GameMaster.RegisterLevel( this );
	}
}
