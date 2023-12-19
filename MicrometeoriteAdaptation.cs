using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace clay.StarshotShip
{
    [CardMeta(deck = Deck.colorless, rarity = Rarity.common, upgradesTo = new Upgrade[] { Upgrade.A, Upgrade.B })]
    public class MicrometeoriteAdaptation : Card
    {
        public override List<CardAction> GetActions(State s, Combat c)
        {
            return new()
            {
                new AStatus()
                {
                    status = Enum.Parse<Status>("tempShield"),
                    statusAmount = (upgrade == Upgrade.B ? 2 : 1),
                    targetPlayer = true
                }
            };
        }

        public override CardData GetData(State state) => new CardData
        {
            cost = 0,
            retain = (upgrade == Upgrade.A ? true : false),
        };

    }
}
