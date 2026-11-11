using System;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace ef2dg
{
    public class GameScene : Scene
    {
        // Public setter so the serializer can link it correctly
        public GameObject Player { get; set; } = null!;

        // Isolation flag: blocks procedural generation when deserializing a file
        public bool IsLoadedFromFile { get; set; } = false;

        public override void Load(ResourceManager resources, IInputContext input)
        {
            if (input.Keyboards.Count > 0) InputManager.Init(input.Keyboards[0]);

            // When data is restored from JSON, the default player and empty map are not created
            if (IsLoadedFromFile) return;

            Texture texGrass = resources.GetTexture("assets/Tilemap_color1.png");
            Map = new Tilemap(0.5f);
            float hitboxRatio = 64f / 192f;
            
            // --- DEFAULT PLAYER (created only for a new empty scene) ---
            Player = new GameObject() { Position = new Vector2D<float>(1.5f, 1.0f) };
            Player.AlwaysActive = true;
            Player.ParentScene = this;
            
            var pSprite = new SpriteComponent();
            pSprite.Animations["Idle"] = new AnimationState { TexturePath = "assets/player/Warrior_Idle.png", FrameWidth = 192, AnimationSpeed = 12f };
            pSprite.Animations["Attack"] = new AnimationState { TexturePath = "assets/player/Warrior_Attack1.png", FrameWidth = 192, AnimationSpeed = 15f };
            pSprite.Play("Idle", true); 
            Player.AddComponent(pSprite);
            Player.AddComponent(new PhysicsBody(new Vector2D<float>(1.0f * hitboxRatio, 1.0f * hitboxRatio), new Vector2D<float>(0f, 0f), Map));
            
            Player.AddComponent(new PlayerController());
            Player.AddComponent(new CameraFollowComponent { SmoothSpeed = 8.0f });

            Entities.Add(Player);
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
        }

        public override void DrawEditorOverlay(SpriteRenderer renderer, Camera camera)
        {
            if (DebugSettings.ShowMapHitbox || DebugSettings.IsEditorMode)
                Map?.DrawDebug(renderer, camera, new Vector4D<float>(0.0f, 1.0f, 0.0f, 1.0f));

            // =========================================================
            // DRAWING COLLIDERS AND ANCHORS (GIZMOS) IN THE EDITOR
            // =========================================================
            if (DebugSettings.IsEditorMode)
            {
                foreach (var entity in Entities)
                {
                    var phys = entity.GetComponent<PhysicsBody>();
                    var box = entity.GetComponent<BoxCollider>();

                    if (phys != null)
                    {
                        var color = (entity == Player) ? new Vector4D<float>(1.0f, 0.0f, 0.0f, 1.0f) : new Vector4D<float>(1.0f, 1.0f, 0.0f, 1.0f);
                        phys.DrawDebug(renderer, camera, color);
                    }
                    else if (box != null)
                    {
                        var color = box.IsTrigger ? new Vector4D<float>(0.0f, 0.8f, 1.0f, 0.8f) : new Vector4D<float>(0.0f, 0.8f, 0.0f, 0.8f);
                        box.DrawDebug(renderer, camera, color);
                    }
                    else
                    {
                        Vector2D<float> pointSize = new Vector2D<float>(0.2f, 0.2f);
                        Vector4D<float> pointColor = new Vector4D<float>(0.1f, 0.1f, 0.1f, 1.0f);
                        renderer.DrawCollisionBox(entity.Position, pointSize, camera, pointColor);
                    }
                }
            }
            else
            {
                if (DebugSettings.ShowPlayerHitbox)
                {
                    var pPhys = Player?.GetComponent<PhysicsBody>();
                    pPhys?.DrawDebug(renderer, camera, new Vector4D<float>(1.0f, 0.0f, 0.0f, 1.0f));
                }

                if (DebugSettings.ShowEnemyHitbox)
                {
                    foreach (var entity in Entities)
                    {
                        if (entity == Player) continue;
                        var ePhys = entity.GetComponent<PhysicsBody>();
                        ePhys?.DrawDebug(renderer, camera, new Vector4D<float>(1.0f, 1.0f, 0.0f, 1.0f));
                    }
                }
            }

            var pController = Player?.GetComponent<PlayerController>();
            if (Player != null && pController != null && (DebugSettings.ShowAttackZone || DebugSettings.IsEditorMode) && pController.IsAttacking)
            {
                var pSprite = Player.GetComponent<SpriteComponent>();
                if (pSprite != null)
                {
                    float directionOffset = pSprite.FlipX ? -pSprite.ActionHitboxOffset.X : pSprite.ActionHitboxOffset.X;
                    Vector2D<float> boxPos = new Vector2D<float>(Player.Position.X + directionOffset, Player.Position.Y + pSprite.ActionHitboxOffset.Y);
                    renderer.DrawCollisionBox(boxPos, pSprite.ActionHitboxSize, camera, new Vector4D<float>(0.0f, 0.0f, 1.0f, 1.0f));
                }
            }

            if (DebugSettings.ShowAttackZone || DebugSettings.IsEditorMode)
            {
                foreach (var entity in Entities)
                {
                    if (entity == Player) continue;
                    var enemyAI = entity.GetComponent<EnemyAIComponent>();
                    if (enemyAI != null && enemyAI.IsAttacking)
                    {
                        var eSprite = entity.GetComponent<SpriteComponent>();
                        if (eSprite != null)
                        {
                            float directionOffset = eSprite.FlipX ? -eSprite.ActionHitboxOffset.X : eSprite.ActionHitboxOffset.X;
                            Vector2D<float> boxPos = new Vector2D<float>(entity.Position.X + directionOffset, entity.Position.Y + eSprite.ActionHitboxOffset.Y);
                            renderer.DrawCollisionBox(boxPos, eSprite.ActionHitboxSize, camera, new Vector4D<float>(1.0f, 0.5f, 0.0f, 1.0f));
                        }
                    }
                }
            }
        }
    }
}