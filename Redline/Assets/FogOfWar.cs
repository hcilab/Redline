using UnityEngine;

public class FogOfWar : MonoBehaviour
{
	[SerializeField] private MonoBehaviour _fog;
	[SerializeField] private int _width = 100;
	[SerializeField] private int _height = 100;
	[SerializeField] private PlayerController _player;
	private GridController _fowGrid;
	
	// Use this for initialization
	void Start ()
	{
		_fowGrid = new GridController( _width, _height, new []{ _fog } , gameObject );
		_fowGrid.InitVariable<bool>( "visible", (item) =>
		{
			var pos = _fowGrid.GetPosition( item._gridCoords );
			var isVisible = _player.HasLineOfSight( new Vector3(
				pos.x,
				1,
				pos.y ) );

			FogController fog = item.GetPayload< FogController >( 0 );

			fog.SetVisibily( isVisible );
			
			return isVisible;
		} );
	}

	void Update()
	{
		_fowGrid.UpdateVariable<bool>( "visible" );
	}
}