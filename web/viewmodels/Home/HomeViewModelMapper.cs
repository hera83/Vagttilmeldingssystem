namespace web.ViewModels.Home;

public static class HomeViewModelMapper
{
    public static IndexHomeViewModel ToIndexHomeViewModel(bool profileComplete) => new()
    {
        ProfileComplete = profileComplete
    };
}
