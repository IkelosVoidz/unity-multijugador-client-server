using System;

public static class AbilityFactory
{
    public static IAbility CreateAbility(Ability type)
    {
        return type switch
        {
            Ability.ABILITY_RANGED => new RangedAbility(),
            Ability.ABILITY_MELEE => new MeleeAbility(),
            _ => throw new ArgumentException("Unknown ability type")
        };
    }
}
