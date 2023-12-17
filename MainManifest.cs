using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CobaltCoreModding.Definitions;
using CobaltCoreModding.Definitions.ExternalItems;
using CobaltCoreModding.Definitions.ModContactPoints;
using CobaltCoreModding.Definitions.ModManifests;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using Shockah.Shared;

namespace clay.StarshotShip
{
    public class MainManifest : IModManifest, ISpriteManifest, IArtifactManifest, IShipPartManifest, IShipManifest, IStartershipManifest
    {
        public static MainManifest Instance;

        public IEnumerable<DependencyEntry> Dependencies => new DependencyEntry[0];

        public DirectoryInfo? GameRootFolder { get; set; }
        public Microsoft.Extensions.Logging.ILogger? Logger { get; set; }
        public DirectoryInfo? ModRootFolder { get; set; }

        public string Name => GetType().Namespace;

        public static Dictionary<string, ExternalSprite> sprites = new Dictionary<string, ExternalSprite>();
        public static Dictionary<string, ExternalPart> parts = new Dictionary<string, ExternalPart>();
        public static ExternalShip starshot;
        public static ExternalArtifact micrometeoriteAdaptation;

        public void BootMod(IModLoaderContact contact)
        {
            Instance = this;
            var harmony = new Harmony(Name);
            harmony.PatchAll();
        }

        public void LoadManifest(ISpriteRegistry artRegistry)
        {
            var filenames = new string[] {
                "micrometeorite_adaptation",
                "starshot_cannon",
                "starshot_chassis",
                "starshot_cockpit",
                "starshot_missiles",
                "starshot_telescopeLeft",
                "starshot_telescopeCenter",
                "starshot_telescopeRight",
            };

            foreach (var filename in filenames)
            {
                var filepath = Path.Combine(ModRootFolder?.FullName ?? "", "sprites", Path.Combine(filename.Split('/')) + ".png");
                var sprite = new ExternalSprite(Name + ".sprites." + filename, new FileInfo(filepath));
                sprites[filename] = sprite;

                if (!artRegistry.RegisterArt(sprite)) throw new Exception("Error registering sprite " + filename);
            }
        }
        
        public void LoadManifest(IShipPartRegistry registry)
        {
            RegisterPart(registry, "cannon", "starshot_cannon", Enum.Parse<PType>("cannon"), new Vec());
            RegisterPart(registry, "cockpit", "starshot_cockpit", Enum.Parse<PType>("cockpit"), new Vec());
            RegisterPart(registry, "missiles", "starshot_missiles", Enum.Parse<PType>("missiles"), new Vec());
            RegisterPart(registry, "telescopeLeft", "starshot_telescopeLeft", Enum.Parse<PType>("comms"), new Vec() { x = -5, y = 0 } );
            RegisterPart(registry, "telescopeCenter", "starshot_telescopeCenter", Enum.Parse<PType>("comms"), new Vec() { x = -23, y = 0 } );
            RegisterPart(registry, "telescopeRight", "starshot_telescopeRight", Enum.Parse<PType>("comms"), new Vec() { x = -5, y = 0 } );
            //RegisterPart(registry, "scaffold", "trinity_scaffold", PType.empty);
        }

        private void RegisterPart(IShipPartRegistry registry, string name, string sprite, PType type, Vec offset)
        {
            parts.Add(name, new ExternalPart(
                Name + ".part." + name,
                new Part() { type = type, offset = offset },
                sprites[sprite] ?? throw new Exception()
            ));
            registry.RegisterPart(parts[name]);
        }

        public void LoadManifest(IArtifactRegistry registry)
        {
            micrometeoriteAdaptation = new ExternalArtifact(Name + "artifacts.MicrometeoriteAdaptation", typeof(MicrometeoriteAdaptation), sprites["micrometeorite_adaptation"]);
            micrometeoriteAdaptation.AddLocalisation("MICROMETEORITE ADAPTATION", "The first time you are hit each turn, gain one energy next turn. The first time you take hull damage each turn, gain one additional energy next turn.");
            registry.RegisterArtifact(micrometeoriteAdaptation);
        }

        public void LoadManifest(IShipRegistry shipRegistry)
        {
            starshot = new ExternalShip(Name + ".Ship",
                new Ship()
                {
                    baseDraw = 5,
                    baseEnergy = 2,
                    heatTrigger = 3,
                    heatMin = 0,
                    hull = 10,
                    hullMax = 10,
                    shieldMaxBase = 5
                },
                new ExternalPart[] {
                    parts["missiles"],
                    parts["cockpit"],
                    parts["cannon"],
                    parts["telescopeLeft"],
                    parts["telescopeCenter"],
                    parts["telescopeRight"]
                },
                sprites["starshot_chassis"] ?? throw new Exception(),
                null
            );
            shipRegistry.RegisterShip(starshot);
        }

        public void LoadManifest(IStartershipRegistry registry)
        {
            if (starshot == null)
                return;
            var starshotShip = new ExternalStarterShip(Name + ".Starter",
                starshot.GlobalName,
                startingArtifacts: new ExternalArtifact[] { micrometeoriteAdaptation },
                exclusiveArtifacts: new ExternalArtifact[] {},
                nativeStartingArtifacts: new Type[] { typeof(ShieldPrep) }
            );

            //starshotShip.AddLocalisation("Starshot", "This sciense vessel was originally intended to launch probes into deep space using its laser array. Some \"creative engineering\" has converted the laser array into a laser cannon.");
            starshotShip.AddLocalisation("Starshot", "This sciense vessel built to withstand repeated metorite strikes. As its pilot, don't be afraid to take a hit.");
            registry.RegisterStartership(starshotShip);
        }
    }
}
