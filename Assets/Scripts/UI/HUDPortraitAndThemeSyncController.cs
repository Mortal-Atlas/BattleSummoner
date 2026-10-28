using UnityEngine;
using BattleSummoner.Character;

namespace BattleSummoner.UI
{
    public class HUDPortraitAndThemeSyncController : MonoBehaviour
    {
        public static HUDPortraitAndThemeSyncController Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void SyncPortraitWithSelectedSex(CharacterSex sex)
        {
            var modular = FindFirstObjectByType<ModularProfilePortrait>();
            if (modular != null)
            {
                var config = (sex == CharacterSex.Female)
                    ? ModularProfileConfiguration.CreateCherylPreset()
                    : ModularProfileConfiguration.CreateHeroPreset();
                modular.ApplyConfiguration(config);
            }
        }
    }
}
