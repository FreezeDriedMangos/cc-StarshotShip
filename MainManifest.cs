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
    [HarmonyPatch]
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

        public static List<int> PartsDrawOrder_NoScaffold = new() { 4, 5, 3, 0, 1, 2 };
        public static List<int> PartsDrawOrder_Scaffold = new()   { 5, 6, 4, 3, 0, 1, 2 }; // intentionally don't draw the scaffolding, it just looks weird next to the telescope

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
                "starshot_chassis",
                "starshot_chassis_trimmed",
                "starshot_cannon",
                "starshot_cockpit",
                "starshot_missiles",
                "starshot_telescopeLeft",
                "starshot_telescopeCenter",
                "starshot_telescopeRight",
                "starshot_scaffolding"
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
            double yOffset = 5;
            RegisterPart(registry, "cannon", "starshot_cannon", Enum.Parse<PType>("cannon"), new Vec() { x = 0, y = yOffset });
            RegisterPart(registry, "cockpit", "starshot_cockpit", Enum.Parse<PType>("cockpit"), new Vec() { x = 0, y = yOffset });
            RegisterPart(registry, "missiles", "starshot_missiles", Enum.Parse<PType>("missiles"), new Vec() { x = 0, y = yOffset });
            RegisterPart(registry, "telescopeLeft", "starshot_telescopeLeft", Enum.Parse<PType>("comms"), new Vec() { x = -5, y = yOffset } );
            RegisterPart(registry, "telescopeCenter", "starshot_telescopeCenter", Enum.Parse<PType>("comms"), new Vec() { x = -23, y = yOffset } );
            RegisterPart(registry, "telescopeRight", "starshot_telescopeRight", Enum.Parse<PType>("comms"), new Vec() { x = -5, y = yOffset } );
            RegisterPart(registry, "scaffolding", "starshot_scaffolding", Enum.Parse<PType>("empty"), new Vec() { x = 0, y = yOffset } );
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
                sprites["starshot_chassis_trimmed"],
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
                nativeStartingArtifacts: new Type[] { typeof(ShieldPrep), typeof(RadarSubwoofer) }
            );

            //starshotShip.AddLocalisation("Starshot", "This sciense vessel was originally intended to launch probes into deep space using its laser array. Some \"creative engineering\" has converted the laser array into a laser cannon.");
            starshotShip.AddLocalisation("Starshot", "This sciense vessel built to withstand repeated metorite strikes. As its pilot, don't be afraid to take a hit.");
            registry.RegisterStartership(starshotShip);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Ship), nameof(Ship.DrawTopLayer))]
        public static bool DrawTopLayer(Ship __instance, G g, Vec v, Vec worldPos)
        {
            if (__instance.key != MainManifest.Instance.Name + ".Starter")
            {
                return true;
            }

            //Vec vec = worldPos + new Vec(__instance.parts.Count * 16 / 2); // ship center

            if (__instance.parts.Count < 6 || __instance.parts.Count > 8)
            {
                // fallback case
                return true;
            }

            var order = __instance.parts.Count == 6 ? PartsDrawOrder_NoScaffold : PartsDrawOrder_Scaffold;

            foreach (int i in order)
            {
                Part part = __instance.parts[i];
                var partxSwoose = 0; // Mutil.MoveTowards(part.xSwoose, 0.0, g.dt * 10.0);
                Vec vec2 = worldPos + new Vec((double)(i * 16) + part.offset.x + partxSwoose * 16.0, -32.0 + (__instance.isPlayerShip ? part.offset.y : (1.0 + (0.0 - part.offset.y))));
                Vec vec3 = v + vec2;

                double num3 = 1.0;
                Vec vec4 = vec3 + new Vec(-1.0, -1.0 + (double)(__instance.isPlayerShip ? 6 : (-6)) * part.pulse).round();

                Color? color2 = new Color(1.0, 1.0, 1.0, num3);
                Spr? spr = (part.active ? DB.parts : DB.partsOff).GetOrNull(part.skin ?? part.type.Key());
                Draw.Sprite(spr, vec4.x, vec4.y, part.flip, !__instance.isPlayerShip, 0.0, null, null, null, null, color2);
            }

            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Events), nameof(Events.AddScaffold))]
        public static void AddScaffold(State s, ref List<Choice> __result)
        {
            if (s.ship.key != MainManifest.Instance.Name + ".Starter") return;

            // tell the scaffolding event what the scaffolding for this ship should look like
            ((__result[0].actions[0] as AShipUpgrades).actions[0] as AInsertPart).part.skin = "";
        }
    }
}
