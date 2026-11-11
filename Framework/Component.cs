using System;
using Silk.NET.Maths;

namespace ef2dg
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SceneTransientAttribute : Attribute { }

    public abstract class Component
    {
        // Reference to the object this component is attached to
        public GameObject Owner { get; internal set; } = null!;

        // ==========================================
        // BASIC SHORTCUTS (FOR CLEANER CODE)
        // ==========================================
        protected Vector2D<float> Position
        {
            get => Owner != null ? Owner.Position : new Vector2D<float>(0, 0);
            set { if (Owner != null) Owner.Position = value; }
        }
        
        protected SpriteComponent? Sprite => Owner?.GetComponent<SpriteComponent>();
        protected PhysicsBody? Physics => Owner?.GetComponent<PhysicsBody>();

        // Virtual methods
        public virtual void Start() { }
        public virtual void Update(float deltaTime) { }
        public virtual void FixedUpdate(float fixedDeltaTime) { }
        public virtual void Draw(SpriteRenderer renderer, Camera camera) { }

        public virtual void OnTriggerEnter(GameObject other) { }
        public virtual void OnTriggerExit(GameObject other) { }
        public virtual void OnTriggerStay(GameObject other) { }
    }
}
