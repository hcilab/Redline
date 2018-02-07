using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogController : MonoBehaviour
{

	private bool _isVisible;
	private LineRenderer _lineRenderer;
	
	// Update is called once per frame
	void Update () {
		if ( _isVisible )
		{
			_lineRenderer = gameObject.GetComponent< LineRenderer >();
			if ( !_lineRenderer ) _lineRenderer = gameObject.AddComponent< LineRenderer >();

			_lineRenderer.startWidth = .1f;
			_lineRenderer.endWidth = .1f;

			_lineRenderer.positionCount = 5;
			_lineRenderer.useWorldSpace = false;
			
			_lineRenderer.SetPositions( new []
			{
				new Vector3( -5, 1, -5),
				new Vector3( -5, 1, 5 ),
				new Vector3( 5, 1, 5 ),
				new Vector3( 5, 1, -5 ),
				new Vector3( -5, 1, -5)
			} );
		}
	}

	public void SetVisibily( bool isVisible )
	{
		_isVisible = isVisible;
	}
}
