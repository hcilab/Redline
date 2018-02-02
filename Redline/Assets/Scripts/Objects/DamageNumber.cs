using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

public class DamageNumber : ObjectPoolItem, FloatingNumber
{
    [SerializeField] private Animator _animator;

    public void SetNumber(double number)
    {
        var magnitude = ( float ) number / 100f;
        var textField =_animator.GetComponentInChildren< Text >();
        textField.text = number.ToString();
        textField.fontSize = (int) Mathf.Clamp( (float) number / 100 * 20 + 20, 15f, 50f );
        textField.color = new Color(
            1f,
            Mathf.Clamp( 1 - magnitude, 0f, 1f),
            Mathf.Clamp( 1-magnitude, 0f, 1f ),
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
        FindObjectOfType<GameMaster>().GetDamageNumberController().RemoveNumber(this);
    }

    private void OnDestroy()
    {
        Destroy( _animator );
    }
}
