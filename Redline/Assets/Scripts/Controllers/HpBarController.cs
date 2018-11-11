using System;
using Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public abstract class IHPSource : MonoBehaviour
{
	protected bool HasDamageAnimation;
	protected Action DamageAnimation;

	public abstract double GetHealth();

	public void SetDamageAnimation( Action action )
	{
		DamageAnimation = action;
	}
}

public enum AttachmentTypes
{
	ENVIRONMENTAL,
	HUD
}

/// <summary>
/// Controller that can be attached to a HP bar in order for that HP bar to 
/// visualize the health remaining of the attached IHPSource.
/// </summary>
public class HpBarController : MonoBehaviour
{

	[SerializeField] private Gradient _color;
	public IHPSource Source;
	[SerializeField] private HpBarScale _scale;
	[SerializeField] private bool _hasTextField;

	[SerializeField] private GameObject _barContainer;
	private Text _text;
	public static HpBarController Instance = null;
	private RectTransform _bkg, _bar;
	private Animator _animator;
	private int _shakeHash;
	private bool _takeDamage = false;
	[SerializeField] private AttachmentTypes _attachmendType;


	// Use this for initialization
	void Awake ()
	{
		
		Debug.Log("Starting up HP bar.");
		_animator = GetComponent< Animator >();
		_shakeHash = Animator.StringToHash( "Shake" );
		
		_bar = _barContainer.transform.Find("bar") as RectTransform;
		_bkg = _barContainer.transform.Find("bkg") as RectTransform;

		_text = GetComponentInChildren<Text>();

		_bar.GetComponent<Image>().color = _color.Evaluate(1);
		
		_bar.sizeDelta = _bkg.rect.size;

		SceneManager.activeSceneChanged += Initialize;
	}

	private void OnEnable()
	{
		if( Source == null ) Source = FindObjectOfType<IHPSource>();
		Source.SetDamageAnimation( () => { _takeDamage = true; } );
	}

	void Initialize( Scene newScene, Scene oldScene )
	{
		OnEnable();
	}

	// Update is called once per frame
	void Update ()
	{
		if ( !Source ) return;
		if ( _attachmendType == AttachmentTypes.ENVIRONMENTAL )
		{
			Vector3 playerPos = Source.transform.position;
			playerPos = Camera.main.WorldToScreenPoint( playerPos );
			playerPos.y += 100;

			transform.position = playerPos;	
		}
		
		var newWidth = ( float ) _scale.scale( Source.GetHealth() );
		_text.enabled = _hasTextField;

		if ( _hasTextField ) _text.text = Mathf.Round( ( float ) Source.GetHealth() * 100 ) + "%";

		_bar.GetComponent< Image >().color = _color.Evaluate( newWidth );

		_bar.sizeDelta = 
			new Vector2(
				_bkg.rect.width * newWidth,
				_bkg.rect.height
			);

		if ( _takeDamage )
		{
			if( _animator.GetCurrentAnimatorClipInfo( 0 ).GetHashCode() != _shakeHash
			    && !_animator.IsInTransition( 0 ))
				_animator.SetTrigger( _shakeHash );
			_takeDamage = false;
		}

//		_bar.sizeDelta = Vector2.Lerp(
//			_bar.rect.size,
//			new Vector2(
//				_bkg.rect.width * newWidth,
//				_bkg.rect.height
//			),
//			5 * Time.deltaTime
//		);
	}
}
