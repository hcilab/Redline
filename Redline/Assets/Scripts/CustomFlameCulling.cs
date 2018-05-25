using UnityEngine;

public class CustomFlameCulling : MonoBehaviour
{

	public float cullingRadius = 10;
	public ParticleSystem target;

	private CullingGroup _cullingGroup;
	
	// Use this for initialization
	void Start () {

		target = GetComponent< ParticleSystem >();
		
		_cullingGroup = new CullingGroup();
		_cullingGroup.targetCamera = Camera.main;
		_cullingGroup.SetBoundingSpheres( new BoundingSphere[]
		{
			new BoundingSphere( transform.position, cullingRadius) 
		} );
		_cullingGroup.SetBoundingSphereCount( 1 );
		_cullingGroup.onStateChanged += OnStateChanged;
	}

	void OnStateChanged( CullingGroupEvent sphere )
	{
		if ( sphere.isVisible )
		{
			target.Play( true );
		}
		else
		{
			target.Pause();
		}
	}

	private void OnDestroy()
	{
		if ( _cullingGroup != null )
		{
			_cullingGroup.Dispose();
			_cullingGroup = null;
		}
	}
}
