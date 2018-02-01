using UnityEngine;

public class OffsetScale : Interfaces.HpBarScale
{

    [SerializeField] private double offset = 2;
    
    public override double scale(double percentageHp)
    {
        double index = percentageHp * 10 + 1;
        return (
                   (index - 1) * (index + offset) / 2f
               ) / (
                   ( 11 - 1 ) * ( 11 + offset ) / 2f
                   );
    }
}
