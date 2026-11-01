namespace SibJam.Combat
{
    public interface IDamageable
    {
        HitResult TakeHit(HitInfo hit);
    }
}
