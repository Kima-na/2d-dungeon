public interface IDamageable
{
    bool IsDead { get; }
    void TakeDamage(int damage);
}

public interface IExperienceSource
{
    int ExperienceReward { get; }
}
