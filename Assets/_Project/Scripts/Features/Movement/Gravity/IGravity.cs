namespace SibJam
{
    public interface IGravity
    {
        public void SetVerticalVelocity(float velocity);

        // Резко тянет вниз (удар в прыжке): скорость падения не меньше заданной
        public void Slam(float downwardSpeed);
    }
}