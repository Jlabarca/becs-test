using ME.BECS;

namespace Volt.Views {

    using ME.BECS.Views;
    using ME.BECS.Units;
    using ME.BECS.Players;
    
    public class UnitView : EntityView {

        public UnityEngine.GameObject selectedContainer;
        public UnityEngine.Material p1Material;
        public UnityEngine.Material p2Material;
        public UnityEngine.Renderer[] renderers;
        
        protected override void ApplyState(in EntRO ent) {
            
            base.ApplyState(in ent);

            var unit = ent.GetAspect<UnitAspect>();
            var selectionGroup = unit.readUnitSelectionGroup;
            this.selectedContainer.SetActive(selectionGroup.IsAlive() == true && PlayerUtils.GetActivePlayer().ent == unit.readOwner);

            var currentOwnerIndex = ent.Read<OwnerComponent>().ent.GetAspect<PlayerAspect>().readIndex;
            foreach (var renderer in this.renderers) {
                renderer.sharedMaterial = currentOwnerIndex == 1u ? this.p1Material : this.p2Material;
            }

        }

    }

}