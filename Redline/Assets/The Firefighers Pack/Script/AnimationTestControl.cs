using UnityEngine;
using System.Collections;

public class AnimationTestControl : MonoBehaviour
{
    private Animation _animation;
    // Use this for initialization
    void Start()
    {
        _animation = GetComponent< Animation >();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            if (Input.GetKey(KeyCode.LeftShift))
                _animation.CrossFade("Run");
            else
                _animation.CrossFade("Walk");
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Input.GetKey(KeyCode.LeftShift))
                _animation.CrossFade("JumpA");
            else
                _animation.CrossFade("JumpB");
        }
        else if (Input.GetMouseButtonDown(0))
            _animation.CrossFade("Dodge");
        else if (Input.GetMouseButtonDown(1))
            _animation.CrossFade("Damage");
        else if (Input.GetMouseButtonDown(2))
            _animation.CrossFade("Death");
        if (!_animation.isPlaying)
            _animation.CrossFade("Idle");
    }
}
