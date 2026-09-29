using System.Collections.ObjectModel;
using System.Windows.Media;
using SkyPilot.Core.Session;

namespace SkyPilot.App.ViewModels;

public sealed record ChatLine(string Time, string From, string Text, Brush Color);

/// <summary>The radio tab (Peer == null) or a private conversation.</summary>
public sealed class ChatTab(string title, string? peer) : Observable
{
    private const int MaxLines = 1000;
    private bool _unread;

    public string Title { get; } = title;
    public string? Peer { get; } = peer;
    public ObservableCollection<ChatLine> Lines { get; } = [];

    public bool Unread
    {
        get => _unread;
        set => Set(ref _unread, value);
    }

    public void Add(ChatMessage m)
    {
        string from = m.Kind switch
        {
            MessageKind.Radio when m.FrequencyKhz is { } f => $"{m.From} [{Core.Model.Frequency.Format(f)}]",
            MessageKind.Broadcast => $"{m.From} [ALL]",
            MessageKind.Atis => $"{m.From} [ATIS]",
            _ => m.From,
        };
        Lines.Add(new ChatLine(m.Time.ToLocalTime().ToString("HH:mm:ss"), from, m.Text, ColorFor(m)));
        if (Lines.Count > MaxLines) Lines.RemoveAt(0);
    }

    private static readonly Brush Normal = Freeze(new SolidColorBrush(Color.FromRgb(0xE4, 0xE5, 0xE7)));
    private static readonly Brush Mine = Freeze(new SolidColorBrush(Color.FromRgb(0x8E, 0x91, 0x97)));
    private static readonly Brush Server = Freeze(new SolidColorBrush(Color.FromRgb(0xA9, 0xAD, 0xB3)));
    private static readonly Brush Private = Freeze(new SolidColorBrush(Color.FromRgb(0x8F, 0xB8, 0xE3)));
    private static readonly Brush Warning = Freeze(new SolidColorBrush(Color.FromRgb(0xD9, 0xA2, 0x3A)));
    private static readonly Brush Atis = Freeze(new SolidColorBrush(Color.FromRgb(0xB7, 0xB1, 0xD6)));
    private static readonly Brush Error = Freeze(new SolidColorBrush(Color.FromRgb(0xE0, 0x60, 0x5A)));

    private static Brush Freeze(SolidColorBrush b)
    {
        b.Freeze();
        return b;
    }

    private static Brush ColorFor(ChatMessage m) => m.Kind switch
    {
        _ when m.Outgoing => Mine,
        MessageKind.Server or MessageKind.Info => Server,
        MessageKind.Private => Private,
        MessageKind.Broadcast => Warning,
        MessageKind.Atis => Atis,
        MessageKind.Error => Error,
        _ => Normal,
    };
}
