using System.ComponentModel;
using Interfaces;
using UnityEditor;
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
	public PlayerController Player;
	[SerializeField] private HpBarScale _scale;
	[SerializeField] private bool _hasTextField;

	private RectTransform _bar, _bkg;
	private Text _text;
	public static HpBarController Instance = null;


	// Use this for initialization
	void Awake ()
	{
		
		Debug.Log("Starting up HP bar.");
		_bar = transform.Find("bar") as RectTransform;
		_bkg = transform.Find("bkg") as RectTransform;

		_text = GetComponentInChildren<Text>();

		_bar.GetComponent<Image>().color = _color.Evaluate(1);
		
		if( _bkg && _bar ) _bar.sizeDelta = _bkg.sizeDelta;

		SceneManager.activeSceneChanged += Initialize;
	}

	private void OnEnable()
	{
		Player = FindObjectOfType<PlayerController>();
	}

	void Initialize( Scene newScene, Scene oldScene )
	{
		OnEnable();
	}

	// Update is called once per frame
	void Update ()
	{
		_text.enabled = _hasTextField;

		if ( _hasTextField ) _text.text = Mathf.Round( ( float ) Player.GetHealth() * 100 ) + "%";

		_bar.GetComponent< Image >().color = _color.Evaluate( ( float ) Player.GetHealth() );

		_bar.sizeDelta = Vector2.Lerp(
			_bar.sizeDelta,
			new Vector2(
				( float ) ( _bkg.sizeDelta.x * _scale.scale( Player.GetHealth() ) ),
				_bar.sizeDelta.y
			),
			3 * Time.deltaTime
		);
	}
}
