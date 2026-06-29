using UnityEngine;

namespace MolecularLab.Chemistry
{
    /// <summary>
    /// Spawns a procedural smoke particle system at a world-space position.
    /// Dark gray puffs rise, expand, and fade over ~4.5 s then self-destruct.
    /// Material loaded from Resources/M_ReactionSmoke (alpha-blend URP Particles/Unlit).
    /// </summary>
    public static class ReactionSmokeVFX
    {
        private const float StartDelay   = 0.25f;
        private const float Duration     = 4.5f;
        private const float LifetimeMin  = 2.8f;
        private const float LifetimeMax  = 4.5f;
        private const float SpeedMin     = 0.08f;
        private const float SpeedMax     = 0.38f;
        private const float SizeMin      = 0.07f;
        private const float SizeMax      = 0.22f;
        private const float SizeEndScale = 3.2f;
        private const int   BurstMin     = 22;
        private const int   BurstMax     = 36;

        private static Material _mat;

        private static Material SmokeMat
        {
            get
            {
                if (_mat == null)
                    _mat = Resources.Load<Material>("M_ReactionSmoke");
                return _mat;
            }
        }

        public static void Spawn(Vector3 position)
        {
            var go = new GameObject("VFX_ReactionSmoke");
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.duration          = Duration;
            main.loop              = false;
            main.startDelay        = new ParticleSystem.MinMaxCurve(StartDelay);
            main.startLifetime     = new ParticleSystem.MinMaxCurve(LifetimeMin, LifetimeMax);
            main.startSpeed        = new ParticleSystem.MinMaxCurve(SpeedMin, SpeedMax);
            main.startSize         = new ParticleSystem.MinMaxCurve(SizeMin, SizeMax);
            main.startColor        = new ParticleSystem.MinMaxGradient(
                new Color(0.18f, 0.18f, 0.18f, 0.55f),
                new Color(0.38f, 0.35f, 0.32f, 0.40f)
            );
            main.gravityModifier   = new ParticleSystem.MinMaxCurve(-0.04f);
            main.maxParticles      = 50;
            main.simulationSpace   = ParticleSystemSimulationSpace.World;
            main.stopAction        = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime  = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, BurstMin, BurstMax) });

            var shape = ps.shape;
            shape.enabled          = true;
            shape.shapeType        = ParticleSystemShapeType.Sphere;
            shape.radius           = 0.1f;

            var vel = ps.velocityOverLifetime;
            vel.enabled            = true;
            vel.space              = ParticleSystemSimulationSpace.World;
            vel.y                  = new ParticleSystem.MinMaxCurve(SpeedMin, SpeedMax);
            vel.x                  = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
            vel.z                  = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);

            var sizeLife = ps.sizeOverLifetime;
            sizeLife.enabled       = true;
            var sizeAnim           = new AnimationCurve(
                new Keyframe(0f,   0.4f, 0f, 4f),
                new Keyframe(0.2f, 1f,   0f, 0f),
                new Keyframe(1f,   SizeEndScale, 0f, 0f)
            );
            sizeLife.size          = new ParticleSystem.MinMaxCurve(1f, sizeAnim);

            var colorLife = ps.colorOverLifetime;
            colorLife.enabled      = true;
            var grad               = new Gradient();
            grad.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.35f, 0.32f, 0.28f), 0f),
                    new GradientColorKey(new Color(0.20f, 0.18f, 0.16f), 0.4f),
                    new GradientColorKey(new Color(0.10f, 0.10f, 0.10f), 1f),
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0f,    0f),
                    new GradientAlphaKey(0.55f, 0.12f),
                    new GradientAlphaKey(0.45f, 0.45f),
                    new GradientAlphaKey(0f,    1f),
                }
            );
            colorLife.color        = new ParticleSystem.MinMaxGradient(grad);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (SmokeMat != null)
                renderer.material = SmokeMat;

            ps.Play();
        }
    }
}
