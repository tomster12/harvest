using System;
using System.Text.RegularExpressions;
using Unity.VisualScripting;

[Serializable]
public enum Stat
{
    // Tool stats
    SwingDamage, SwingSpeed, ResourceYield,

    // Equipment stats
    Armour, MovementSpeed, CarryCapacity,
}

public static class StatExtensions
{
    public static string ToDisplayString(this Stat stat)
    {
        return Regex.Replace(stat.ToString(), "(\\B[A-Z])", " $1");
    }
}