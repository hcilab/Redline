//TODO write flame controller
public class FlameController : ObjectPoolItem
{

	private float _intensity = 0;
	
	// Use this for initialization
	void Start () {
		//set up particle system
	}

	private void Update()
	{
//		float newIntensity = GameMaster.getFireSystemController().getIntensity( this.getGridCoordinates() )
	}

	public void SetIntensity( float s )
	{
		_intensity = s;
	}

	public float GetIntensity()
	{
		return _intensity;
	}
}
