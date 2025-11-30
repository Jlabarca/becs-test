using ME.BECS;

namespace Volt.Views {

    using ME.BECS.Views;
    using Components;
    using UnityEngine;
    
    public class SelectionRectView : EntityView {

        public UnityEngine.LineRenderer lineRenderer;
        public Vector3 offset = new Vector3(0f, 0.01f, 0f);
        
        protected override void ApplyState(in EntRO ent) {
            
            base.ApplyState(in ent);

            var rect = ent.Read<SelectionRectComponent>();
            this.lineRenderer.positionCount = 4;
            this.lineRenderer.SetPosition(0, (Vector3)rect.p1 + this.offset);
            this.lineRenderer.SetPosition(1, (Vector3)rect.p2 + this.offset);
            this.lineRenderer.SetPosition(2, (Vector3)rect.p3 + this.offset);
            this.lineRenderer.SetPosition(3, (Vector3)rect.p4 + this.offset);

        }

    }

}