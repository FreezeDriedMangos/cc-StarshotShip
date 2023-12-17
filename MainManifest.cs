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
    public class MainManifest : IModManifest, ISpriteManifest
    {
        public static MainManifest Instance;

        public IEnumerable<DependencyEntry> Dependencies => new DependencyEntry[0];

        public DirectoryInfo? GameRootFolder { get; set; }
        public Microsoft.Extensions.Logging.ILogger? Logger { get; set; }
        public DirectoryInfo? ModRootFolder { get; set; }

        public string Name => GetType().Namespace;

        public static Dictionary<string, ExternalSprite> sprites = new Dictionary<string, ExternalSprite>();
        public static Dictionary<string, ExternalPart> parts = new Dictionary<string, ExternalPart>();
        public static Dictionary<string, ExternalArtifact> artifacts = new Dictionary<string, ExternalArtifact>();

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
            parts.Add("cannon", new ExternalPart(
                "parchment.armada.Trinity.Cannon",
                new Part()
                {
                    active = false,
                    damageModifier = PDamMod.none,
                    type = PType.cannon,
                },
                sprites["trinity_cannon"],
                sprites["trinity_cannon_off"]
            ));
            registry.RegisterPart(parts["cannon"]);

            addPart("cockpit", "trinity_cockpit", PType.cockpit, false, registry);
            addPart("missiles", "trinity_missiles", PType.missiles, false, registry);
            addPart("scaffold", "trinity_scaffold", PType.empty, false, registry);
        }

        private void RegisterPart(string name, string sprite, PType type, bool flip, IShipPartRegistry registry)
        {
            parts.Add(name, new ExternalPart(
            "parchment.armada.trinity." + name,
            new Part() { active = true, damageModifier = PDamMod.none, type = type, flip = flip },
            sprites[sprite] ?? throw new Exception()));
            registry.RegisterPart(parts[name]);
        }
    }
}
