using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using UnityEngine.UI;

public class ScoreNumber: ObjectPoolItem, FloatingNumber
{
    [SerializeField] private Animator _animator;

    public void SetNumber( double number )
    {
        var magnitude = ( float ) number / 400f;
        var textField =_animator.GetComponentInChildren< Text >();
        textField.text = number.ToString();
        textField.fontSize = ( int ) Mathf.Clamp( magnitude * 20 + 14, 14f, 60f );
        textField.color = new Color(
                Mathf.Clamp( 1 - magnitude, 0f, 1f ),
                1f,
                Mathf.Clamp( 1 - magnitude, 0f, 1f ),
                1f
            ); 
    }

    public void StartPlayback()
    {
        _animator.enabled = true;
    }

    public void AnimationComplete()
    {
        _animator.enabled = false;
        FindObjectOfType<GameMaster>().GetScoreNumberController().RemoveNumber( this );
    }

    private void OnDestroy()
    {
        Destroy( _animator );
    }
}
