using System;
using Silk.NET.Maths;

namespace ef2dg
{
    public class TeleportTriggerComponent : Component
    {
        public Vector2D<float> TargetLocation { get; set; } = new Vector2D<float>(5f, 5f);
        public string TargetSceneName { get; set; } = "";
        public TransitionSettings Transition { get; set; } = new TransitionSettings();

        public override void OnTriggerEnter(GameObject other)
        {
            // Check who exactly stepped in.
            // There is no tag system yet, so look for the player controller to keep mobs from teleporting
            var player = other.GetComponent<PlayerController>();
            
            if (player != null)
            {
                if (!string.IsNullOrWhiteSpace(TargetSceneName))
                {
                    Console.WriteLine($"[EVENT] Player entered teleport! Loading scene: {TargetSceneName}");
                    Engine.Instance.RequestSceneTransition(TargetSceneName, false, Transition);
                }
                else
                {
                    Console.WriteLine($"[EVENT] Player entered teleport! Moving to coordinates: {TargetLocation}");
                    Engine.Instance.RunTransition(() => other.Position = TargetLocation, Transition);
                }
            }
        }

        public override void OnTriggerExit(GameObject other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                Console.WriteLine("[EVENT] Player left teleport zone.");
            }
        }
    }
}
