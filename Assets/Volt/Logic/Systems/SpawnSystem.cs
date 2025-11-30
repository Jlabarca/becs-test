using UnityEngine;
using ME.BECS;
using ME.BECS.Network;
using ME.BECS.Players;
using ME.BECS.Trees;
using Unity.Burst;
using ME.BECS.FixedPoint;
using ME.BECS.Transforms;

namespace Volt {
    
    public struct SpawnSystem : IStart {

        public Config unitConfig;
        public Config unitAttackConfig;
        public uint amount;
        
        public void OnStart(ref SystemContext context) {
            
            // we need to complete dependencies to be sure all previous jobs are done
            context.dependsOn.Complete();

            var owner1 = context.world.GetSystem<PlayersSystem>().GetPlayerEntity(1u);
            var owner2 = context.world.GetSystem<PlayersSystem>().GetPlayerEntity(2u);
            var agentType = context.world.GetSystem<ME.BECS.Pathfinding.BuildGraphSystem>().GetAgentProperties(1u);
            for (uint i = 0u; i < this.amount; ++i) {
                var owner = owner1;
                var startPos = new float3(10f, 0f, 10f);
                if (i >= this.amount / 2u) {
                    owner = owner2;
                    startPos = new float3(20f, 0f, 20f);
                }
                var unit = ME.BECS.Units.UnitUtils.CreateUnit(agentType, owner.readUnitsTreeIndex, context);
                PlayerUtils.SetOwner(unit.ent, owner);
                this.unitConfig.Apply(unit.ent);
                {
                    var q = unit.ent.Set<QuadTreeQueryAspect>();
                    q.query.treeMask = owner.unitsTreeMask;
                    q.query.ignoreSelf = true;
                    q.query.ignoreSorting = true;
                    q.query.rangeSqr = 2f * 2f;
                    q.query.nearestCount = 5;
                }
                {
                    var sensor = ME.BECS.Attack.AttackUtils.CreateAttackSensor(owner.readUnitsOthersTreeMask, this.unitAttackConfig, context);
                    sensor.SetParent(unit.ent);
                }
                var tr = unit.ent.Set<TransformAspect>();
                tr.position = new float3(startPos.x + unit.ent.GetRandomValue(), 0f, startPos.z + unit.ent.GetRandomValue());
            }
            
        }

    }
    
}