using ME.BECS;
using ME.BECS.Network;
using ME.BECS.FixedPoint;

namespace Volt {
    
    public struct SelectUnitData : IPackageData {
        
        public float3 from;
        public float3 to;
        public bbool ctrl;
        public bbool shift;
        
        public void Serialize(ref StreamBufferWriter writer) {
            writer.Write(this.from);
            writer.Write(this.to);
            writer.Write(this.ctrl);
            writer.Write(this.shift);
        }
        
        public void Deserialize(ref StreamBufferReader reader) {
            reader.Read(ref this.from);
            reader.Read(ref this.to);
            reader.Read(ref this.ctrl);
            reader.Read(ref this.shift);
        }
        
    }
    
}