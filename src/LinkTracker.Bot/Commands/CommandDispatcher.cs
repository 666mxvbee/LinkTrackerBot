using Telegram.Bot;
using Telegram.Bot.Types;
using LinkTracker.Bot.Services;
using LinkTracker.Bot.Clients;
using LinkTracker.Shared.Models;

namespace LinkTracker.Bot.Commands;

public class CommandDispatcher(
    IEnumerable<IBotCommand> commands,
    UserStateService stateService,
    IScrapperClient scrapperClient,
    ILogger<CommandDispatcher> logger)
{
    public async Task HandleMessageAsync(ITelegramBotClient bot, Message msg, CancellationToken ct)
    {
        if (msg.Text is not { } messageText) return;
        
        var chatId = msg.Chat.Id;
        var session = stateService.GetSession(chatId);

        if (messageText.StartsWith("/"))
        {
            var commandName = messageText.Split(' ')[0];
            
            if (session.State != UserState.Idle && commandName != "/track")
            {
                stateService.ResetSession(chatId);
            }

            var command = commands.FirstOrDefault(c => c.Name == commandName);
            if (command != null)
            {
                await command.ExecuteAsync(bot, msg, ct);
                return;
            }
        }

        if (session.State != UserState.Idle)
        {
            await HandleDialogueStep(bot, msg, session, ct);
            return;
        }

        await bot.SendMessage(chatId, "Unknown command. Use /help", cancellationToken: ct);
    }

    private async Task HandleDialogueStep(ITelegramBotClient bot, Message msg, UserSession session, CancellationToken ct)
    {
        var text = msg.Text;
        var chatId = msg.Chat.Id;

        if (session.State == UserState.TrackAwaitingUrl)
        {
            if (Uri.TryCreate(text, UriKind.Absolute, out _) && (text.Contains("github.com") || text.Contains("stackoverflow.com")))
            {
                session.TempUrl = text;
                session.State = UserState.TrackAwaitingTags;
                await bot.SendMessage(chatId, "URL accepted! Enter tags via comma or /skip:", cancellationToken: ct);
            }
            else
            {
                await bot.SendMessage(chatId, "Invalid URL. Use /cancel", cancellationToken: ct);
            }
        }
        else if (session.State == UserState.TrackAwaitingTags)
        {
            var tags = text?.ToLower() == "/skip" ? Array.Empty<string>() : text?.Split(',').Select(t => t.Trim()).ToArray();
            try 
            {
                await scrapperClient.AddLink(chatId, new AddLinkRequest(session.TempUrl!, tags));
                await bot.SendMessage(chatId, "Success! Link added.", cancellationToken: ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding link");
                await bot.SendMessage(chatId, "Error: service unavailable or link exists.", cancellationToken: ct);
            }
            finally { stateService.ResetSession(chatId); }
        }
        else if (session.State == UserState.UntrackAwaitingUrl)
        {
            try
            {
                await scrapperClient.RemoveLink(chatId, new RemoveLinkRequest(text!));

                await bot.SendMessage(chatId, $"The link is successfully deleted: {text}", cancellationToken: ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error removing link for chat {ChatId}", chatId);
                await bot.SendMessage(chatId, "Error: Couldn't delete the link. It may not be followed.", cancellationToken: ct);
            }
            finally
            {
                stateService.ResetSession(chatId);
            }
        }
    }
}