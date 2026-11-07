using System;
using System.Text.RegularExpressions;
using Unity.VisualScripting;

[Serializable]
public enum Stat
{
    SwingStrength, SwingSpeed, ResourceYield,
    Armour, MovementSpeed, CarryCapacity,
    Sharpness, BitWidth,
}

public static class StatExtensions
{
    public static string ToDisplayString(this Stat stat)
    {
        return Regex.Replace(stat.ToString(), "(\\B[A-Z])", " $1");
    }
}