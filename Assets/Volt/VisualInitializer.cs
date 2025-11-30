using UnityEngine;
using ME.BECS;
using Unity.Burst;

namespace Volt {
    
    public class VisualWorld {
        public static readonly SharedStatic<World> world = SharedStatic<World>.GetOrCreate<VisualWorld>();
        public static World World => world.Data;
    }
    
    public class VisualInitializer : WorldInitializer {

        protected override void Awake() {
            
            base.Awake();
            VisualWorld.world.Data = this.world;
            
        }

    }
    
}