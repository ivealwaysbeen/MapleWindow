using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapleWindow.Core.Config;
using MapleWindow.Core.Nexon;

namespace MapleWindow.App.ViewModels;

public enum SetupStep { ApiKey, PickCharacter }

/// <summary>
/// Drives both the first-run setup flow (API key -> character list -> pick one) and "캐릭터 변경"
/// (tray menu re-runs this with the API key already known, starting at PickCharacter).
/// </summary>
public partial class SetupViewModel : ObservableObject
{
    private readonly INexonApiClient _api;
    private readonly IConfigStore _configStore;

    [ObservableProperty]
    private SetupStep _currentStep = SetupStep.ApiKey;

    [ObservableProperty]
    private string _apiKey = "";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private CharacterPickerItem? _selectedCharacter;

    public ObservableCollection<CharacterPickerItem> Characters { get; } = [];

    /// <summary>Raised when the window should close — true on a successful save, false on cancel.</summary>
    public event EventHandler<bool>? RequestClose;

    public SetupViewModel(INexonApiClient api, IConfigStore configStore)
    {
        _api = api;
        _configStore = configStore;
    }

    /// <summary>Called by "캐릭터 변경" to skip straight to the picker with the existing API key.</summary>
    public void StartAtPickCharacter(string existingApiKey)
    {
        ApiKey = existingApiKey;
        CurrentStep = SetupStep.PickCharacter;
        _ = FetchCharactersAsync();
    }

    [RelayCommand]
    private async Task FetchCharactersAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            ErrorMessage = "API 키를 입력해주세요.";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var response = await _api.GetCharacterListAsync(ApiKey.Trim());
            var items = response.AccountList
                .SelectMany(account => account.CharacterList)
                .Select(c => new CharacterPickerItem(c.Ocid, c.CharacterName, c.WorldName, c.CharacterClass, c.CharacterLevel))
                .ToList();

            if (items.Count == 0)
            {
                ErrorMessage = "연결된 캐릭터가 없습니다.";
                return;
            }

            Characters.Clear();
            foreach (var item in items) Characters.Add(item);
            CurrentStep = SetupStep.PickCharacter;
        }
        catch (NexonApiException ex)
        {
            ErrorMessage = $"API 키가 올바르지 않거나 요청에 실패했습니다. ({ex.Message})";
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "네트워크 연결을 확인해주세요.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Confirm()
    {
        if (SelectedCharacter is null)
        {
            ErrorMessage = "캐릭터를 선택해주세요.";
            return;
        }

        var existing = _configStore.Current;
        _configStore.Save(new AppConfig
        {
            ApiKey = ApiKey.Trim(),
            Ocid = SelectedCharacter.Ocid,
            CharacterName = SelectedCharacter.CharacterName,
            WorldName = SelectedCharacter.WorldName,
            PollIntervalSeconds = existing?.PollIntervalSeconds ?? 300,
            SpeakIntervalSeconds = existing?.SpeakIntervalSeconds ?? 1800,
        });

        RequestClose?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(this, false);

    [RelayCommand]
    private void BackToApiKeyStep() => CurrentStep = SetupStep.ApiKey;
}
