using PersonalFinanceCli.Application.Repositories;

namespace PersonalFinanceCli.Infrastructure.Persistence;

public sealed class JsonOnboardingStateRepository : IOnboardingStateRepository
{
    private readonly JsonDataStore _store;

    public JsonOnboardingStateRepository(JsonDataStore store)
    {
        _store = store;
    }

    public DateOnly? GetLastCushionDeclinedDate()
    {
        return _store.Load().LastCushionDeclinedDate;
    }

    public void SetLastCushionDeclinedDate(DateOnly? date)
    {
        var data = _store.Load();
        data.LastCushionDeclinedDate = date;
        _store.Save(data);
    }

    public bool GetHasSeenOnboarding()
    {
        return _store.Load().HasSeenOnboarding;
    }

    public void SetHasSeenOnboarding(bool value)
    {
        var data = _store.Load();
        data.HasSeenOnboarding = value;
        _store.Save(data);
    }
}
