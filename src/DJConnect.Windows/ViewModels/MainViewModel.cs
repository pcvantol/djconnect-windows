using System.Net.Http;
using System.Collections.ObjectModel;
using DJConnect.Windows.Contracts;
using DJConnect.Windows.Models;
using DJConnect.Windows.Services;

namespace DJConnect.Windows.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly SettingsStore _settingsStore = new();
    private readonly CredentialStore _credentialStore = new();
    private readonly DJConnectApiClient _apiClient = new(new HttpClient());
    private AppSettings _settings = new();
    private ClientIdentity _identity = ClientIdentity.CreateOrLoad(null);
    private string _homeAssistantUrl = DJConnectContract.DefaultHomeAssistantUrl;
    private string _token = "";
    private string _pairingCode = "";
    private string _askDJText = "";
    private string _status = "Niet gekoppeld";
    private string _nowPlaying = "Geen playbackstatus";
    private bool _isPaired;

    public MainViewModel()
    {
        SaveSettingsCommand = new AsyncCommand(SaveSettingsAsync);
        PairCommand = new AsyncCommand(PairAsync, () => !string.IsNullOrWhiteSpace(PairingCode));
        RefreshCommand = new AsyncCommand(RefreshAsync, () => IsPaired);
        SendAskDJCommand = new AsyncCommand(SendAskDJAsync, () => IsPaired && !string.IsNullOrWhiteSpace(AskDJText));
        ClearHistoryCommand = new AsyncCommand(ClearHistoryAsync, () => IsPaired);
        PlayCommand = new AsyncCommand(() => RunCommandAsync("play"), () => IsPaired);
        PauseCommand = new AsyncCommand(() => RunCommandAsync("pause"), () => IsPaired);
        NextCommand = new AsyncCommand(() => RunCommandAsync("next"), () => IsPaired);
        PreviousCommand = new AsyncCommand(() => RunCommandAsync("previous"), () => IsPaired);
    }

    public ObservableCollection<AskDJMessage> Messages { get; } = [];
    public ObservableCollection<PlaybackAction> Actions { get; } = [];
    public ObservableCollection<RecentItem> RecentItems { get; } = [];

    public string HomeAssistantUrl
    {
        get => _homeAssistantUrl;
        set => SetProperty(ref _homeAssistantUrl, value);
    }

    public string Token
    {
        get => _token;
        set => SetProperty(ref _token, value);
    }

    public string PairingCode
    {
        get => _pairingCode;
        set
        {
            if (SetProperty(ref _pairingCode, value))
            {
                PairCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string AskDJText
    {
        get => _askDJText;
        set
        {
            if (SetProperty(ref _askDJText, value))
            {
                SendAskDJCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string NowPlaying
    {
        get => _nowPlaying;
        set => SetProperty(ref _nowPlaying, value);
    }

    public string DeviceId => _identity.DeviceId;
    public string ClientType => _identity.ClientType;
    public string LegalNotice => DJConnectContract.SpotifyNotice;

    public bool IsPaired
    {
        get => _isPaired;
        set
        {
            if (SetProperty(ref _isPaired, value))
            {
                PairCommand.RaiseCanExecuteChanged();
                RefreshCommand.RaiseCanExecuteChanged();
                SendAskDJCommand.RaiseCanExecuteChanged();
                ClearHistoryCommand.RaiseCanExecuteChanged();
                PlayCommand.RaiseCanExecuteChanged();
                PauseCommand.RaiseCanExecuteChanged();
                NextCommand.RaiseCanExecuteChanged();
                PreviousCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public AsyncCommand SaveSettingsCommand { get; }
    public AsyncCommand PairCommand { get; }
    public AsyncCommand RefreshCommand { get; }
    public AsyncCommand SendAskDJCommand { get; }
    public AsyncCommand ClearHistoryCommand { get; }
    public AsyncCommand PlayCommand { get; }
    public AsyncCommand PauseCommand { get; }
    public AsyncCommand NextCommand { get; }
    public AsyncCommand PreviousCommand { get; }

    public async Task InitializeAsync()
    {
        _settings = await _settingsStore.LoadAsync();
        _identity = ClientIdentity.CreateOrLoad(_settings.InstallId, _settings.DeviceName);
        HomeAssistantUrl = _settings.HomeAssistantUrl;
        Token = _credentialStore.ReadToken() ?? "";
        IsPaired = !string.IsNullOrWhiteSpace(Token);
        ConfigureClient();
        OnPropertyChanged(nameof(DeviceId));
        OnPropertyChanged(nameof(ClientType));
        if (IsPaired)
        {
            await SyncHistoryAsync(showStatus: false);
        }
    }

    private async Task SaveSettingsAsync()
    {
        _settings.HomeAssistantUrl = HomeAssistantUrl;
        _settings.InstallId = _identity.InstallId;
        _settings.DeviceName = _identity.DeviceName;
        await _settingsStore.SaveAsync(_settings);
        if (!string.IsNullOrWhiteSpace(Token))
        {
            _credentialStore.SaveToken(Token.Trim());
            IsPaired = true;
        }

        ConfigureClient();
        Status = "Instellingen opgeslagen";
    }

    private async Task PairAsync()
    {
        ConfigureClient();
        var payload = new PairingPayload(
            _identity.DeviceId,
            _identity.DeviceName,
            _identity.ClientType,
            PairingCode.Trim(),
            PairingCode.Trim(),
            PairingCode.Trim());
        var response = await _apiClient.PairAsync(payload, CancellationToken.None);
        if (!response.Success || string.IsNullOrWhiteSpace(response.DeviceToken))
        {
            Status = response.Error ?? response.Message ?? "Pairing niet gelukt";
            return;
        }

        Token = response.DeviceToken;
        _credentialStore.SaveToken(Token);
        IsPaired = true;
        await SaveSettingsAsync();
        Status = $"Gekoppeld: {response.PairingStatus ?? "paired"}";
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        ConfigureClient();
        var response = await _apiClient.GetStatusAsync(_identity, CancellationToken.None);
        if (!response.Success)
        {
            Status = response.Error ?? "Status ophalen mislukt";
            return;
        }

        Status = $"Ask DJ: {(response.AskDJSupported == true ? "beschikbaar" : "onbekend")} | Spotify: {(response.SpotifyConfigured == true ? "geconfigureerd" : "niet bevestigd")}";
        NowPlaying = response.Playback is null
            ? "Geen playbackstatus"
            : $"{response.Playback.Title ?? "Onbekend"} - {response.Playback.Artist ?? "Onbekende artiest"}";
        await SyncHistoryAsync(showStatus: false);
    }

    private async Task SendAskDJAsync()
    {
        ConfigureClient();
        var request = new AskDJRequest(
            Guid.NewGuid().ToString("N"),
            _identity.DeviceId,
            _identity.DeviceId,
            _identity.DeviceName,
            _identity.ClientType,
            AskDJText.Trim());
        AskDJText = "";
        var response = await _apiClient.SendAskDJMessageAsync(request, CancellationToken.None);
        if (!response.Success)
        {
            Status = response.Error ?? response.Message ?? "Ask DJ verzoek mislukt";
            return;
        }

        if (response.HistoryRevision.HasValue)
        {
            _settings.HistoryRevision = response.HistoryRevision.Value;
        }
        if (response.ClearRevision.HasValue)
        {
            _settings.ClearRevision = response.ClearRevision.Value;
        }

        if (response.UserMessage is not null)
        {
            Messages.Add(response.UserMessage);
        }
        if (response.AssistantMessage is not null)
        {
            Messages.Add(response.AssistantMessage);
        }
        else if (!string.IsNullOrWhiteSpace(response.Text ?? response.Message))
        {
            Messages.Add(new AskDJMessage(Guid.NewGuid().ToString("N"), "assistant", response.Text, response.Message, DateTimeOffset.Now, "assistant", response.PlaybackActions, response.ConfirmationActions, response.Items));
        }

        ReplaceActions(response.AssistantMessage?.PlaybackActions ?? response.PlaybackActions, response.AssistantMessage?.ConfirmationActions ?? response.ConfirmationActions);
        ReplaceRecentItems(response.AssistantMessage?.Items ?? response.Items);
        await _settingsStore.SaveAsync(_settings);
        Status = "Ask DJ bijgewerkt";
    }

    private async Task ClearHistoryAsync()
    {
        ConfigureClient();
        var response = await _apiClient.ClearAskDJHistoryAsync(_identity, CancellationToken.None);
        if (!response.Success)
        {
            Status = response.Error ?? "History wissen mislukt";
            return;
        }

        Messages.Clear();
        Actions.Clear();
        RecentItems.Clear();
        _settings.HistoryRevision = response.HistoryRevision;
        _settings.ClearRevision = response.ClearRevision;
        await _settingsStore.SaveAsync(_settings);
        Status = "Ask DJ history gewist";
    }

    private async Task SyncHistoryAsync(bool showStatus)
    {
        ConfigureClient();
        var response = await _apiClient.GetAskDJHistoryAsync(_settings.HistoryRevision, CancellationToken.None);
        if (!response.Success)
        {
            if (showStatus)
            {
                Status = response.Error ?? "History sync mislukt";
            }
            return;
        }

        if (response.ClearRevision > _settings.ClearRevision)
        {
            Messages.Clear();
            Actions.Clear();
            RecentItems.Clear();
        }

        foreach (var message in response.Messages)
        {
            Messages.Add(message);
        }

        _settings.HistoryRevision = response.HistoryRevision;
        _settings.ClearRevision = response.ClearRevision;
        await _settingsStore.SaveAsync(_settings);
    }

    private async Task RunCommandAsync(string command)
    {
        ConfigureClient();
        var response = await _apiClient.RunCommandAsync(_identity, command, CancellationToken.None);
        Status = response.Success
            ? response.DjText ?? response.Message ?? $"Command uitgevoerd: {command}"
            : response.Error ?? $"Command mislukt: {command}";
        await RefreshAsync();
    }

    private void ConfigureClient()
    {
        _apiClient.Configure(HomeAssistantUrl, Token);
    }

    private void ReplaceActions(IReadOnlyList<PlaybackAction>? playbackActions, IReadOnlyList<PlaybackAction>? confirmationActions)
    {
        Actions.Clear();
        foreach (var action in (playbackActions ?? []).Concat(confirmationActions ?? []))
        {
            Actions.Add(action);
        }
    }

    private void ReplaceRecentItems(IReadOnlyList<RecentItem>? items)
    {
        RecentItems.Clear();
        foreach (var item in items ?? [])
        {
            RecentItems.Add(item);
        }
    }
}
