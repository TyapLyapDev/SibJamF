namespace SibJam
{
    public interface IGravity
    {
        // Ускорение подъёма (без множителя падения) — для расчёта скорости прыжка по высоте
        public float Strength { get; }

        public void SetVerticalVelocity(float velocity);

        // Резко тянет вниз (удар в прыжке): скорость падения не меньше заданной
        public void Slam(float downwardSpeed);
    }
}
