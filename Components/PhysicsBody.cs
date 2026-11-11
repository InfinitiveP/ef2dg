using Silk.NET.Maths;

namespace ef2dg
{
    public class PhysicsBody : Component
    {
        // Хранит триггеры, внутри которых мы СЕЙЧАС находимся
        private System.Collections.Generic.HashSet<GameObject> _activeTriggers = new System.Collections.Generic.HashSet<GameObject>();
        public Vector2D<float> HitboxSize { get; set; }
        public Vector2D<float> HitboxOffset { get; set; }
        public Vector2D<float> Velocity { get; set; } = new Vector2D<float>(0, 0);
        public float GravityScale { get; set; } = 1.0f; // 1 = Платформер, 0 = Вид сверху
        public bool IsGrounded { get; set; } = false;
        
        // НОВОЕ: Галочка в Инспекторе для включения/выключения системы Step-Up
        public bool EnableStepUp { get; set; } = true;

        public Tilemap Map { get; set; } = null!; 

        public PhysicsBody(Vector2D<float> size, Vector2D<float> offset, Tilemap map)
        {
            HitboxSize = size;
            HitboxOffset = offset;
            Map = map; 
        }

        public PhysicsBody() { } 

        public override void FixedUpdate(float deltaTime)
        {
            if (Map == null || Owner?.ParentScene == null) return;
            
            var pos = Owner.Position;
            Velocity = new Vector2D<float>(Velocity.X, Velocity.Y - (15.0f * GravityScale) * deltaTime);
            
            float skin = 0.03f;

            //ПЕРЕВІРКА ПО ОСІ X (З КРОКОМ)
            float nextX = pos.X + Velocity.X * deltaTime;
            float pLeftX = nextX + HitboxOffset.X - HitboxSize.X / 2f;
            float pRightX = nextX + HitboxOffset.X + HitboxSize.X / 2f;
            
            float pTopX = pos.Y + HitboxOffset.Y + HitboxSize.Y / 2f - skin;
            float pBottomX = pos.Y + HitboxOffset.Y - HitboxSize.Y / 2f + skin; 

            float stepUp = 0f;
            
            // У режимі виду зверху (GravityScale == 0) зашагування не потрібне, 
            // воно викликає прилипання до стін.
            if (EnableStepUp && GravityScale > 0)
            {
                var nextFloor = Map.GetSlopeFloorY(pLeftX, pRightX, pBottomX, true);
                
                if (nextFloor.HasValue && nextFloor.Value.Y > pBottomX)
                {
                    float lift = nextFloor.Value.Y - pBottomX;
                    
                    if (nextFloor.Value.IsSlope)
                    {
                        if (lift <= Map.TileSizeInWorld * 1.5f) 
                        {
                            stepUp = lift + skin;
                        }
                    }
                    else
                    {
                        if (lift <= Map.TileSizeInWorld * 0.1f) 
                        {
                            stepUp = lift + skin;
                        }
                    }
                }
            }

            if (!Map.HasCollision(pLeftX, pRightX, pTopX + stepUp, pBottomX + stepUp)) 
            {
                pos.X = nextX;
                if (stepUp > 0 && IsGrounded) pos.Y += stepUp;
            }
            else
            {
                Velocity = new Vector2D<float>(0, Velocity.Y);
            }

            // --- 2. ПРОВЕРКА ПО ОСИ Y (ПОЛ И ПОТОЛОК) ---
            float nextY = pos.Y + Velocity.Y * deltaTime;
            float pLeftY = pos.X + HitboxOffset.X - HitboxSize.X / 2f + skin;
            float pRightY = pos.X + HitboxOffset.X + HitboxSize.X / 2f - skin;
            float pTopY = nextY + HitboxOffset.Y + HitboxSize.Y / 2f;
            float pBottomY = nextY + HitboxOffset.Y - HitboxSize.Y / 2f;

            bool wasGrounded = IsGrounded;
            IsGrounded = false;
            
            bool resolvedFloor = false;

            // Прилипание к полу/склонам также работает только для платформера
            if (EnableStepUp && GravityScale > 0)
            {
                var floorUnder = Map.GetSlopeFloorY(pLeftY, pRightY, pBottomY, true);
                
                if (floorUnder.HasValue)
                {
                    float floorY = floorUnder.Value.Y; 
                    float distToFloor = pBottomY - floorY;
                    
                    if ((distToFloor <= 0 && Velocity.Y <= 0) || (wasGrounded && distToFloor <= Map.TileSizeInWorld * 0.5f))
                    {
                        pos.Y = floorY + HitboxSize.Y / 2f - HitboxOffset.Y;
                        if (Velocity.Y < 0) Velocity = new Vector2D<float>(Velocity.X, 0);
                        IsGrounded = true;
                        resolvedFloor = true; 
                    }
                }
            }

            // Если мы не прилипли к полу (или находимся в режиме вида сверху), проверяем чистую коллизию
            if (!resolvedFloor)
            {
                if (!Map.HasCollision(pLeftY, pRightY, pTopY, pBottomY))
                {
                    pos.Y = nextY;
                }
                else
                {
                    if (Velocity.Y < 0) IsGrounded = true;
                    Velocity = new Vector2D<float>(Velocity.X, 0); 
                }
            }

            Owner.Position = pos;

            // =======================================================
            // --- 3. СКАНИРОВАНИЕ ТРИГГЕРОВ (С поддержкой Stay) ---
            // =======================================================
            if (Owner.ParentScene != null)
            {
                float myLeft = pos.X + HitboxOffset.X - HitboxSize.X / 2f;
                float myRight = pos.X + HitboxOffset.X + HitboxSize.X / 2f;
                float myBottom = pos.Y + HitboxOffset.Y - HitboxSize.Y / 2f;
                float myTop = pos.Y + HitboxOffset.Y + HitboxSize.Y / 2f;

                var currentOverlaps = new System.Collections.Generic.HashSet<GameObject>();

                foreach (var otherEntity in Owner.ParentScene.Entities)
                {
                    if (otherEntity == Owner) continue; 

                    var box = otherEntity.GetComponent<BoxCollider>();
                    
                    if (box != null && box.IsTrigger)
                    {
                        float otherLeft = otherEntity.Position.X + box.Offset.X - box.Size.X / 2f;
                        float otherRight = otherEntity.Position.X + box.Offset.X + box.Size.X / 2f;
                        float otherBottom = otherEntity.Position.Y + box.Offset.Y - box.Size.Y / 2f;
                        float otherTop = otherEntity.Position.Y + box.Offset.Y + box.Size.Y / 2f;

                        if (myLeft <= otherRight && myRight >= otherLeft &&
                            myBottom <= otherTop && myTop >= otherBottom)
                        {
                            currentOverlaps.Add(otherEntity);

                            if (!_activeTriggers.Contains(otherEntity))
                            {
                                _activeTriggers.Add(otherEntity);
                                
                                otherEntity.OnTriggerEnter(Owner); 
                                Owner.OnTriggerEnter(otherEntity); 
                            }
                            else
                            {
                                otherEntity.OnTriggerStay(Owner);
                                Owner.OnTriggerStay(otherEntity);
                            }
                        }
                    }
                }

                var tempTriggers = new System.Collections.Generic.List<GameObject>(_activeTriggers);
                foreach (var activeTrigger in tempTriggers)
                {
                    if (!currentOverlaps.Contains(activeTrigger))
                    {
                        _activeTriggers.Remove(activeTrigger);
                        activeTrigger.OnTriggerExit(Owner);
                        Owner.OnTriggerExit(activeTrigger);
                    }
                }
            }
        }

        public void DrawDebug(SpriteRenderer renderer, Camera camera, Silk.NET.Maths.Vector4D<float> color)
        {
            var boxPos = new Silk.NET.Maths.Vector2D<float>(Owner.Position.X + HitboxOffset.X, Owner.Position.Y + HitboxOffset.Y);
            renderer.DrawCollisionBox(boxPos, HitboxSize, camera, color);
        }
    }
}