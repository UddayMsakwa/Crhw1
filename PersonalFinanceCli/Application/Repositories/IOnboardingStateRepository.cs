namespace PersonalFinanceCli.Application.Repositories;

public interface IOnboardingStateRepository
{
    DateOnly? GetLastCushionDeclinedDate();

    void SetLastCushionDeclinedDate(DateOnly? date);

    bool GetHasSeenOnboarding();

    void SetHasSeenOnboarding(bool value);
}
