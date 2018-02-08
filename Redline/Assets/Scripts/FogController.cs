using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogController : MonoBehaviour
{

	private LineRenderer _lineRenderer;
	
	public void SetVisibily( bool isVisible )
	{
		if ( isVisible )
		{
			_lineRenderer = gameObject.GetComponent< LineRenderer >();
			if ( !_lineRenderer ) _lineRenderer = gameObject.AddComponent< LineRenderer >();

			_lineRenderer.startWidth = .1f;
			_lineRenderer.endWidth = .1f;

			_lineRenderer.positionCount = 4;
			_lineRenderer.useWorldSpace = false;
			_lineRenderer.startColor = Color.magenta;
			_lineRenderer.endColor = Color.magenta;

			_lineRenderer.SetPositions( new[]
			{
				new Vector3( 0, 1, 0 ),
				new Vector3( 1, 1, 1 ),
				new Vector3( 0, 1, 1 ),
				new Vector3( 1, 1, 0 )
			} );
		}
		else
		{
			if ( _lineRenderer )
			{
				_lineRenderer.positionCount = 0;
				_lineRenderer.SetPositions( new[]{ new Vector3(0,0,0) } );
			}
		}
	}
}
