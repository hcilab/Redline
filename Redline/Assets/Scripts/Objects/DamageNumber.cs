using UnityEngine;

public class DamageNumber : FloatingNumber
{

    public void SetNumber(double number)
    {
        var magnitude = ( float ) number / 100f;
        
        TextField.fontSize = (int) Mathf.Clamp( (float) number / 100 * 20 + 20, 15f, 50f );
        TextField.color = new Color(
            1f,
            Mathf.Clamp( 1 - magnitude, 0f, 1f),
            Mathf.Clamp( 1 - magnitude, 0f, 1f ),
            1f
        );
        base.SetNumber( number );
    }

    public override void AnimationComplete()
    {
        FindObjectOfType<GameMaster>().GetDamageNumberController().RemoveNumber( this );
    }
}
