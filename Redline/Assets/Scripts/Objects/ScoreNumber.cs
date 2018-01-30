using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

public class ScoreNumber: ObjectPoolItem, FloatingNumber
{
    [SerializeField] private Animator _animator;

    public void setText(string text)
    {
        _animator.GetComponentInChildren<Text>().text = text;
    }

    public void startPlayback()
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
