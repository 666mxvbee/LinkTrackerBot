namespace LinkTracker.Bot.Services.Notifications;

public sealed record KafkaDlqMessage(
    string Reason,
    string Error,
    string? Payload,
    string SourceTopic,
    int SourcePartition,
    long SourceOffset,
    DateTimeOffset FailedAt);
