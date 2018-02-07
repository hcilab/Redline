using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using UnityEngine.UI;

public class ScoreNumber: FloatingNumber
{

    public void SetNumber( double number )
    {
        var magnitude = ( float ) number / 400f;
        TextField.fontSize = ( int ) Mathf.Clamp( magnitude * 20 + 14, 14f, 60f );
        TextField.color = new Color(
                Mathf.Clamp( 1 - magnitude, 0f, 1f ),
                1f,
                Mathf.Clamp( 1 - magnitude, 0f, 1f ),
                1f
            );
        base.SetNumber( number );
    }

    public override void AnimationComplete()
    {
        FindObjectOfType<GameMaster>( ).GetScoreNumberController().RemoveNumber( this );
    }
}
