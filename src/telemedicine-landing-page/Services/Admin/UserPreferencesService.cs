using Microsoft.Extensions.Options;
using TelemedicineLandingPage.Models.Admin;
using TelemedicineLandingPage.Models.Chatbot;
using TelemedicineLandingPage.Services.Chatbot;

namespace TelemedicineLandingPage.Services.Admin;

public sealed class UserPreferencesService : IUserPreferencesService
{
    private readonly object _gate = new();
    private UserPreferences _current;

    public UserPreferencesService()
        : this(Options.Create(new ChatbotOptions()))
    {
    }

    public UserPreferencesService(IOptions<ChatbotOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var chatbot = options.Value;
        _current = new UserPreferences
        {
            AiModel = ChatbotModelCatalog.Resolve(chatbot.Provider, chatbot.Model, preferredModel: null)
        };
    }

    public UserPreferences Current
    {
        get { lock (_gate) return _current; }
    }

    public void Update(UserPreferences next)
    {
        ArgumentNullException.ThrowIfNull(next);
        lock (_gate)
        {
            _current = next;
        }
        StateChanged?.Invoke();
    }

    public event Action? StateChanged;
}
