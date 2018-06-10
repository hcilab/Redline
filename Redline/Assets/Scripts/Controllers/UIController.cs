using UnityEngine;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
	public static UIController Instance = null;
	
	public void Awake()
	{
		if ( Instance == null )
			Instance = this;
		else if ( Instance != this ) 
			Destroy( this );

		DontDestroyOnLoad( Instance );
	}
}
