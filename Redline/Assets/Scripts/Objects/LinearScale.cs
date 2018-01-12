using System;

[Serializable]
public class LinearScale : Interfaces.HpBarScale {
    public override double scale(double percentageHp)
    {
        return percentageHp;
    }
}
