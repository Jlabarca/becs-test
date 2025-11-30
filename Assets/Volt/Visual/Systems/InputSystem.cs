using UnityEngine;
using Unity.Burst;
using ME.BECS;
using ME.BECS.Network;
using ME.BECS.Players;
using ME.BECS.Trees;
using ME.BECS.Commands;
using ME.BECS.Pathfinding;
using ME.BECS.Units;
using ME.BECS.FixedPoint; // use FixedPoint instead of Unity.Mathematics
using ME.BECS.Network.Markers;

namespace Volt {
    
    using Components;
    
    public struct InputSystem : IStart, IUpdate {
        
        public static InputSystem Default => new InputSystem() { activePlayer = 1u };

        public Config selectionRectConfig;
        public uint activePlayer;
        
        private float3? dragStartPoint;
        private Ent selectionRect;

        public void OnStart(ref SystemContext context) {
            
            context.dependsOn.Complete();
            
            // this method must be called from transport connector do determine which player is active now
            SetActivePlayer(this.activePlayer);
            
        }
        
        public void OnUpdate(ref SystemContext context) {
            
            context.dependsOn.Complete();
            
            this.DebugPlayerChange();
            this.UpdateDemoInput(in context);
            
        }
        
        private void DebugPlayerChange() {
            
            // here we can switch player for debug purposes
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Alpha1) == true) {
                SetActivePlayer(1u);
            } else if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Alpha2) == true) {
                SetActivePlayer(2u);
            }
            
        }
        
        private void UpdateDemoInput(in SystemContext context) {
            
            // Select units
            if (Input.GetMouseButtonDown(0) == true) {
                var camera = Camera.main;
                var ray = camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, 1000f, -1) == true) {
                    var point = (float3)hit.point;
                    this.selectionRect = Ent.New(context);
                    this.selectionRectConfig.Apply(this.selectionRect);
                    this.dragStartPoint = point;
                }
            }

            if (this.selectionRect.IsAlive() == true) {
                var camera = Camera.main;
                var ray = camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, 1000f, -1) == true) {
                    var point = (float3)hit.point;
                    var p1 = this.dragStartPoint.Value;
                    var p3 = point;
                    var p2 = new float3(p1.x, 0f, p3.z);
                    var p4 = new float3(p3.x, 0f, p1.z);
                    this.selectionRect.Set(new SelectionRectComponent() {
                        p1 = p1,
                        p2 = p2,
                        p3 = p3,
                        p4 = p4,
                    });
                }
            }

            if (Input.GetMouseButtonUp(0) == true && this.selectionRect.IsAlive() == true) {
                var camera = Camera.main;
                var ray = camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, 1000f, -1) == true) {
                    var point = (float3)hit.point;
                    var world = LogicWorld.World;
                    {
                        world.SendNetworkEvent(new SelectUnitData() {
                            from = this.dragStartPoint.Value,
                            to = point,
                            ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl),
                            shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
                        }, SelectUnitAction);
                    }
                    this.dragStartPoint = null;
                }
                this.selectionRect.Destroy();
            }
            
            // Move units
            if (Input.GetMouseButtonDown(1) == true) {
                var camera = Camera.main;
                var ray = camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, 1000f, -1) == true) {
                    var point = (float3)hit.point;
                    var world = LogicWorld.World;
                    world.SendNetworkEvent(new CommandUnitData() {
                        position = point,
                    }, CommandUnitAction);
                }
            }
            
        }
        
        [NetworkMethod]
        [AOT.MonoPInvokeCallback(typeof(NetworkMethodDelegate))]
        public static void CommandUnitAction(in InputData data, ref SystemContext context) {
            
            context.dependsOn.Complete();
            
            var world = context.world;
            var commandUnitData = data.GetData<CommandUnitData>();
            var playerId = data.PlayerId;
            
            var owner = world.GetSystem<PlayersSystem>().GetPlayerEntity(playerId);
            if (owner.readCurrentSelection.IsAlive() == false) return;
            var buildGraphSystem = world.GetSystem<BuildGraphSystem>();
            
            // here we can check move vs attack command
            CommandsUtils.SetCommand(buildGraphSystem, owner.readCurrentSelection.GetAspect<UnitSelectionGroupAspect>(), new CommandMove() {
                targetPosition = commandUnitData.position,
            }, context);
            
        }
        
        [NetworkMethod]
        [AOT.MonoPInvokeCallback(typeof(NetworkMethodDelegate))]
        public static void SelectUnitAction(in InputData data, ref SystemContext context) {
            
            context.dependsOn.Complete();
            
            var world = context.world;
            var selectUnitData = data.GetData<SelectUnitData>();
            var playerId = data.PlayerId;
            
            var owner = world.GetSystem<PlayersSystem>().GetPlayerEntity(playerId);
            var qt = world.GetSystem<QuadTreeInsertSystem>();
            
            var p1 = selectUnitData.from;
            var p3 = selectUnitData.to;
            UnitSelectionTempGroupAspect group;
            if (math.lengthsq(p1 - p3) <= 0.01f) {
                group = UnitUtils.CreateSelectionGroupByTypeInPointTemp(qt, owner.readUnitsTreeIndex, p1, maxRange: 1f, jobInfo: context);
            } else {
                var p2 = new float3(p1.x, 0f, p3.z);
                var p4 = new float3(p3.x, 0f, p1.z);
                group = UnitUtils.CreateSelectionGroupByRectTemp(qt, owner.readUnitsTreeIndex, p1, p2, p3, p4, jobInfo: context);
            }

            // here we can use other methods like a ByTypeInRect or similar
            UnitUtils.SetSelectionGroup(owner, group, selectUnitData.shift, selectUnitData.ctrl, context);
            
        }
        
        public static void SetActivePlayer(uint activePlayer) {
            var players = LogicWorld.World.GetSystem<PlayersSystem>();
            var player = players.GetPlayers()[activePlayer];
            PlayerUtils.SetActivePlayer(player.GetAspect<PlayerAspect>());
            LogicInitializer.GetNetworkModule().SetLocalPlayerId(activePlayer);
        }
        
    }
    
}