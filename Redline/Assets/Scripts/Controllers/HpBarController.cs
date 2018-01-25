using Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controller that can be attached to a HP bar in order for that HP bar to 
/// visualize the health remaining of the attached PlayerController.
/// </summary>
public class HpBarController : MonoBehaviour
{

	[SerializeField] private Gradient _color;
	private PlayerController _player;
	[SerializeField] private HpBarScale _scale;
	[SerializeField] private bool _hasTextField;

	private RectTransform _bar, _bkg;
	private Text _text;


	// Use this for initialization
	void Awake ()
	{
		_bar = transform.Find("bar") as RectTransform;
		_bkg = transform.Find("bkg") as RectTransform;

		_text = _bkg.GetComponentInChildren<Text>();

		_bar.GetComponent<Image>().color = _color.Evaluate(1);
		
		if( _bkg && _bar ) _bar.sizeDelta = _bkg.sizeDelta;
		
		Debug.Log("Initialized HP bar.");

		SceneManager.sceneLoaded += Initialize;
	}

	private void Initialize( Scene arg0, LoadSceneMode arg1 )
	{
		_player = FindObjectOfType<PlayerController>();
	}

	// Update is called once per frame
	void Update ()
	{
		_text.enabled = _hasTextField;

		if (_hasTextField) _text.text = Mathf.Round( (float)_player.GetHealth() * 100) + "%";

		_bar.GetComponent<Image>().color = _color.Evaluate((float) _player.GetHealth());
		
		_bar.sizeDelta = Vector2.Lerp( 
			_bar.sizeDelta,
			new Vector2(
				(float) (_bkg.sizeDelta.x * _scale.scale(_player.GetHealth())),
				_bar.sizeDelta.y
			), 
			3 * Time.deltaTime
			);
	}
}
