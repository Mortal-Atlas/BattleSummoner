using UnityEngine;

public class EnchantmentForgeThreeStrike : MonoBehaviour
{
    public enum ForgeOutcome
    {
        Accepted,
        Strike1_Deflection,
        Strike2_FractureWarning,
        Strike3_CatastrophicDetonation
    }

    [Header("Anvil State")]
    public int strikeCounter = 0;
    public int safeCapacitySockets = 3;

    public ForgeOutcome AttemptEnchant(LivingGearSystem weapon, int currentSocketCount, PlayerProgression player)
    {
        if (currentSocketCount < safeCapacitySockets)
        {
            strikeCounter = 0;
            return ForgeOutcome.Accepted;
        }

        strikeCounter++;

        switch (strikeCounter)
        {
            case 1:
                return ForgeOutcome.Strike1_Deflection;

            case 2:
                // Strike 2: Halves current temper and inflicts fracture debt
                if (weapon != null)
                {
                    weapon.currentTemper *= 0.5f;
                    weapon.fractureDebt += 20f;
                }
                return ForgeOutcome.Strike2_FractureWarning;

            case 3:
            default:
                // Strike 3: Detonation
                if (weapon != null)
                {
                    weapon.currentTemper = 0f;
                    weapon.fractureDebt = weapon.maxTemper;
                }
                if (player != null)
                {
                    player.TakeDirectDamage(350f);
                    SystemBroadcastManager.Instance?.TriggerAchievement("ach_secret_anvil_suicide", player.playerName);
                }
                strikeCounter = 0;
                return ForgeOutcome.Strike3_CatastrophicDetonation;
        }
    }

    public void StabilizeWeaponMatrix()
    {
        strikeCounter = 0;
    }
}
