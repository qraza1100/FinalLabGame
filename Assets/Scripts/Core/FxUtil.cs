using UnityEngine;

namespace DrownedSanctum
{
    /// <summary>Small particle helpers used at runtime and by the editor builder.</summary>
    public static class FxUtil
    {
        public static Material ParticleMat;

        public static ParticleSystem Burst(Vector3 pos, Color color, int count = 24, float speed = 3f, float size = 0.25f, float life = 0.8f)
        {
            var go = new GameObject("FX Burst");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.05f;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.3f;
            FadeOut(ps);
            var r = go.GetComponent<ParticleSystemRenderer>();
            if (ParticleMat) r.sharedMaterial = ParticleMat;
            ps.Play();
            return ps;
        }

        public static void FadeOut(ParticleSystem ps)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
        }

        /// <summary>Configures a looping ambient emitter (bubbles, sparkles, currents).</summary>
        public static void ConfigureLoop(ParticleSystem ps, Material mat, Color color, float rate, float life, float speed, float size,
                                         ParticleSystemShapeType shape, float radius, Vector3 boxSize, float gravity = -0.02f,
                                         ParticleSystemSimulationSpace space = ParticleSystemSimulationSpace.World, int maxParticles = 200)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 5f;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = space;
            main.maxParticles = maxParticles;
            var em = ps.emission;
            em.rateOverTime = rate;
            var sh = ps.shape;
            sh.shapeType = shape;
            sh.radius = radius;
            sh.scale = boxSize;
            FadeOut(ps);
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
        }
    }
}
