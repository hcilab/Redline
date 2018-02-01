using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

public class DamageNumber : ObjectPoolItem, FloatingNumber
{
    [SerializeField] private Animator _animator;

    public void SetText(string text)
    {
        _animator.GetComponentInChildren<Text>().text = text;
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
