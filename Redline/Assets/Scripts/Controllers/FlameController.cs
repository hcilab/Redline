//TODO write flame controller

using UnityEngine;

public class FlameController : ObjectPoolItem
{
    public override void Disable()
    {
        var emission = GetComponent< ParticleSystem >().emission;
        emission.enabled = false;
        transform.position = Camera.main.transform.position + 
                             Vector3.Cross( Vector3.up, new Vector3(100,100,100) );
        base.Disable();
    }

    public override void Enable()
    {
        var emission = GetComponent< ParticleSystem >().emission;
        emission.enabled = true;
        base.Enable();        
    }
}
