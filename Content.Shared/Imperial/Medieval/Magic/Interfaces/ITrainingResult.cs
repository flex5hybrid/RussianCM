namespace Content.Shared.Imperial.Medieval.Magic;


/// <summary>
/// Interface for magic trainings
/// </summary>
public interface ITrainingResult
{
    /// <summary>
    /// Perform training result logic
    /// </summary>
    void PerformTraining(EntityUid performer, EntityManager entityManager);


    /// <summary>
    /// Checks can we start training
    /// <para>
    /// You dosent need to check can we start training. This method run automatically in <see cref="PerformTraining">
    /// </para>
    /// </summary>
    /// <returns>Can we start training</returns>
    bool CanPerformTraining(EntityUid performer, EntityManager entityManager);
}
