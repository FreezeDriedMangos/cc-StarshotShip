using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace clay.StarshotShip
{
    [ArtifactMeta(owner = Deck.colorless, pools = new ArtifactPool[] { ArtifactPool.EventOnly }, unremovable = true)]
    public class MicrometeoriteAdaptation : Artifact
    {
        private bool EnergyReward1 = false;
        private bool EnergyReward2 = false;

        public override void OnTurnStart(State state, Combat combat)
        {
            EnergyReward1 = false;
            EnergyReward2 = false;
        }

        public override void OnPlayerTakeNormalDamage(State state, Combat combat, int rawAmount, Part? part)
        {
            if (EnergyReward1) return;
            EnergyReward1 = true;

            MainManifest.Instance.Logger.LogInformation("Player was hit! Awarding energy");

            combat.QueueImmediate(new AStatus
            {
                status = Enum.Parse<Status>("energyNextTurn"),
                targetPlayer = true,
                statusAmount = 1,
                mode = Enum.Parse<AStatusMode>("Add"),
                artifactPulse = Key()
            });
        }

        public override void OnPlayerLoseHull(State state, Combat combat, int amount)
        {
            this.OnPlayerTakeNormalDamage(state, combat, 0, null); // for when the player takes hull damage that bypasses the normal damage check

            if (EnergyReward2) return;
            EnergyReward2 = true;

            combat.QueueImmediate(new AStatus
            {
                status = Enum.Parse<Status>("energyNextTurn"),
                targetPlayer = true,
                statusAmount = 1,
                mode = Enum.Parse<AStatusMode>("Add"),
                artifactPulse = Key()
            });
        }
    }
}
