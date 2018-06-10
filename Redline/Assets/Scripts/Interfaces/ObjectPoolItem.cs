using UnityEngine;

public abstract class ObjectPoolItem : MonoBehaviour
{

    public virtual void Disable()
    {
        enabled = false;
    }

    public virtual void Enable()
    {
        enabled = true;
    }
}
