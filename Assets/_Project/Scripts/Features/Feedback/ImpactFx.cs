using UnityEngine;

namespace SibJam.Feedback
{
    // Одноразовые вспышки частиц из кода: искры от ударов и взрыв сломанной камеры. Материалы — в Resources/ImpactFx
    public sealed class ImpactFx
    {
        private Material _sparkMaterial;
        private Material _smokeMaterial;

        private Material SparkMaterial => _sparkMaterial ??= Resources.Load<Material>("ImpactFx/Spark");
        private Material SmokeMaterial => _smokeMaterial ??= Resources.Load<Material>("ImpactFx/Smoke");

        // Попадание мечом: искры веером от точки удара по направлению замаха
        public void Hit(Vector3 point, Vector3 direction, bool isStrong, float delay = 0f)
        {
            float power = isStrong ? 1.8f : 1f;

            Burst(point, direction, SparkMaterial, count: (int)(18 * power), speed: new Vector2(6f, 14f) * power,
                lifetime: new Vector2(0.15f, 0.35f), size: new Vector2(0.04f, 0.09f),
                color: isStrong ? new Color(1f, 0.35f, 0.15f) : new Color(1f, 0.8f, 0.4f), angle: 50f, gravity: 2f, stretch: true, delay: delay);

            Burst(point, direction, SparkMaterial, count: 1, speed: Vector2.zero,
                lifetime: new Vector2(0.08f, 0.08f), size: new Vector2(0.8f, 0.8f) * power,
                color: new Color(1f, 0.9f, 0.7f, 0.8f), angle: 0f, gravity: 0f, stretch: false, delay: delay);
        }

        // Удар с прыжка о землю: кольцо пыли вокруг цели
        public void Slam(Vector3 point)
        {
            Burst(point, Vector3.up, SmokeMaterial, count: 16, speed: new Vector2(3f, 6f),
                lifetime: new Vector2(0.4f, 0.7f), size: new Vector2(0.5f, 1f),
                color: new Color(0.7f, 0.7f, 0.72f, 0.6f), angle: 85f, gravity: -0.2f, stretch: false);
        }

        // Разрушение камеры: сноп искр, вспышка и дым
        public void Explosion(Vector3 point)
        {
            Burst(point, Vector3.up, SparkMaterial, count: 45, speed: new Vector2(4f, 12f),
                lifetime: new Vector2(0.3f, 0.8f), size: new Vector2(0.05f, 0.12f),
                color: new Color(1f, 0.6f, 0.2f), angle: 180f, gravity: 3f, stretch: true);

            Burst(point, Vector3.up, SparkMaterial, count: 1, speed: Vector2.zero,
                lifetime: new Vector2(0.12f, 0.12f), size: new Vector2(2f, 2f),
                color: new Color(1f, 0.85f, 0.6f, 0.9f), angle: 0f, gravity: 0f, stretch: false);

            Burst(point, Vector3.up, SmokeMaterial, count: 12, speed: new Vector2(0.5f, 2f),
                lifetime: new Vector2(0.8f, 1.6f), size: new Vector2(0.6f, 1.4f),
                color: new Color(0.2f, 0.2f, 0.22f, 0.7f), angle: 60f, gravity: -0.3f, stretch: false);
        }

        private void Burst(Vector3 point, Vector3 direction, Material material, int count, Vector2 speed,
            Vector2 lifetime, Vector2 size, Color color, float angle, float gravity, bool stretch, float delay = 0f)
        {
            if (material == null || count <= 0) return;

            var go = new GameObject("ImpactFx");
            go.transform.SetPositionAndRotation(point, Quaternion.LookRotation(direction.sqrMagnitude > 0.0001f ? direction : Vector3.up));

            var particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.duration = 0.1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startDelay = delay;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.useUnscaledTime = true; // чтобы стоп-кадр удара не замораживал искры

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = Mathf.Min(angle, 90f);
            shape.radius = 0.05f;

            if (angle > 90f)
                shape.shapeType = ParticleSystemShapeType.Sphere;

            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = stretch ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            renderer.velocityScale = stretch ? 0.04f : 0f;
            renderer.lengthScale = stretch ? 1f : 0f;

            particles.Play();
            Object.Destroy(go, delay + 0.1f + lifetime.y + 0.1f);
        }
    }
}
