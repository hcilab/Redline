using UnityEngine;

public class FogController : MonoBehaviour
{

	private SpriteRenderer _fogSprite;
	private LineRenderer _lineRenderer;

	private void Awake()
	{
		_fogSprite = GetComponent< SpriteRenderer >();
		
		_lineRenderer = gameObject.GetComponent< LineRenderer >();
		if ( !_lineRenderer ) _lineRenderer = gameObject.AddComponent< LineRenderer >();
	}

	private void Start()
	{
		var scale = transform.localScale;
//		scale = scale * 1.2F;
		transform.localScale = scale;
	}

	public void SetVisibily( bool isVisible )
	{
		var pos = transform.position;
		pos.y = 11f;
		transform.position = pos;
		
		if ( isVisible )
		{
			_fogSprite.enabled = false;
			

//			_lineRenderer.startWidth = .1f;
//			_lineRenderer.endWidth = .1f;
//
//			_lineRenderer.positionCount = 4;
//			_lineRenderer.useWorldSpace = false;
//			_lineRenderer.startColor = Color.magenta;
//			_lineRenderer.endColor = Color.magenta;
//
//			_lineRenderer.SetPositions( new[]
//			{
//				new Vector3( 0, 1, 0 ),
//				new Vector3( 1, 1, 1 ),
//				new Vector3( 0, 1, 1 ),
//				new Vector3( 1, 1, 0 )
//			} );
		}
		else
		{
			_fogSprite.enabled = true;
//			if ( _lineRenderer )
//			{
//				_lineRenderer.positionCount = 0;
//				_lineRenderer.SetPositions( new[]{ new Vector3(0,0,0) } );
//			}
		}
	}
}
