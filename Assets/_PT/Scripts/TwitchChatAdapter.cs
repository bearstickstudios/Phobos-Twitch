using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace PhobosTwitch
{
    /// <summary>
    /// Reads Twitch chat over anonymous IRC and raises ChatMessage events.
    ///
    /// IMPORTANT: the official Twitch Unity SDK (Twitch.API) does not expose a chat
    /// message EventSub subscription in this SDK version - only Follows, Subscriptions,
    /// Cheers, Raids, Hype Train and Channel Point redemptions are available (see
    /// dev.twitch.tv/docs/game-engine-plugins/unity-reference, EventStreamKind /
    /// TwitchApi method list). There is no SubscribeToChatMessageEvents(), and no
    /// "TwitchClient" type anywhere in the plugin - that class in the previous version
    /// of this file didn't exist. Reading chat therefore still requires IRC (or a
    /// 3rd-party client such as TwitchLib, or a hand-rolled EventSub-over-WebSocket
    /// listener for "channel.chat.message" - both bigger jobs than this fix).
    ///
    /// This keeps the (working) IRC approach from the old prototype but:
    ///  - uses real async I/O (ReadLineAsync/WriteLineAsync) with a proper
    ///    connect/read/reconnect loop instead of a coroutine polling Update(),
    ///  - is NOT gated on TwitchAuthManager.IsAuthenticated, since anonymous IRC read
    ///    access needs no Twitch OAuth token at all - the old code's dependency on
    ///    auth completing first was a race that meant chat often never connected.
    /// </summary>
    public class TwitchChatAdapter : MonoBehaviour
    {
        [Header("Configuration")] [SerializeField]
        private string channel = "your_channel";

        [SerializeField] private float reconnectDelaySeconds = 5f;

        public event Action<ChatMessage> OnMessageReceived;
        public bool IsConnected { get; private set; }

        private TcpClient _tcpClient;
        private StreamReader _reader;
        private StreamWriter _writer;
        private CancellationTokenSource _cts;

        private async void Start()
        {
            _cts = new CancellationTokenSource();
            await RunConnectionLoopAsync(_cts.Token);
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            Disconnect();
        }

        private async Task RunConnectionLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await ConnectAndListenAsync(token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Twitch chat connection error: {ex.Message}");
                }

                IsConnected = false;
                Disconnect();

                if (token.IsCancellationRequested) break;

                Debug.Log($"Reconnecting to Twitch chat in {reconnectDelaySeconds}s...");
                await Task.Delay(TimeSpan.FromSeconds(reconnectDelaySeconds), token);
            }
        }

        private async Task ConnectAndListenAsync(CancellationToken token)
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync("irc.chat.twitch.tv", 6667);

            var stream = _tcpClient.GetStream();
            _reader = new StreamReader(stream);
            _writer = new StreamWriter(stream) { AutoFlush = true };

            string anonymousNick = "justinfan" + UnityEngine.Random.Range(10000, 99999);
            await _writer.WriteLineAsync("CAP REQ :twitch.tv/tags twitch.tv/commands");
            await _writer.WriteLineAsync($"NICK {anonymousNick}");
            await _writer.WriteLineAsync($"JOIN #{channel.ToLower()}");

            IsConnected = true;
            Debug.Log($"Connected anonymously to Twitch chat: #{channel}");

            while (!token.IsCancellationRequested && _tcpClient.Connected)
            {
                string line = await _reader.ReadLineAsync();
                if (line == null) break; // remote closed the connection
                ProcessIrcLine(line);
            }
        }

        private void Disconnect()
        {
            try
            {
                _writer?.Dispose();
                _reader?.Dispose();
                _tcpClient?.Close();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error during Twitch chat disconnect: {ex.Message}");
            }
            finally
            {
                _writer = null;
                _reader = null;
                _tcpClient = null;
            }
        }

        private void ProcessIrcLine(string rawMessage)
        {
            if (rawMessage.StartsWith("PING"))
            {
                _ = _writer?.WriteLineAsync(rawMessage.Replace("PING", "PONG"));
                return;
            }

            try
            {
                if (rawMessage.Contains("PRIVMSG"))
                {
                    var chatMessage = ParsePrivMsg(rawMessage);
                    if (!string.IsNullOrEmpty(chatMessage.username))
                    {
                        OnMessageReceived?.Invoke(chatMessage);
                    }
                }
                else if (rawMessage.Contains("USERNOTICE"))
                {
                    var notice = ParseUserNotice(rawMessage);
                    if (!string.IsNullOrEmpty(notice.username))
                    {
                        OnMessageReceived?.Invoke(notice);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to process IRC line: {rawMessage} - {ex.Message}");
            }
        }

        // --- Parsing (ported from the working IRC prototype, with one correctness fix noted below) ---

        private ChatMessage ParsePrivMsg(string rawMessage)
        {
            var message = new ChatMessage { timestamp = DateTime.Now, type = MessageType.RegularChat };

            Dictionary<string, string> tags = new Dictionary<string, string>();
            if (rawMessage.StartsWith("@"))
            {
                int tagEnd = rawMessage.IndexOf(' ');
                tags = ParseTags(rawMessage.Substring(1, tagEnd - 1));
                rawMessage = rawMessage.Substring(tagEnd + 1);
            }

            int userStart = rawMessage.IndexOf(':') + 1;
            int userEnd = rawMessage.IndexOf('!');
            message.username = rawMessage.Substring(userStart, userEnd - userStart);

            // NOTE: the old prototype used rawMessage.LastIndexOf(':') here, which breaks
            // as soon as a chat message itself contains a colon (e.g. a URL, or "note: hi").
            // The trailing IRC parameter always starts at the first " :" after the prefix,
            // so we look for that instead.
            int messageStart = rawMessage.IndexOf(" :", userEnd);
            message.message = messageStart >= 0 ? rawMessage.Substring(messageStart + 2) : string.Empty;

            if (tags.Count > 0)
            {
                ParseMessageTags(ref message, tags);
            }

            return message;
        }

        private ChatMessage ParseUserNotice(string rawMessage)
        {
            var message = new ChatMessage
            {
                timestamp = DateTime.Now,
                type = MessageType.UserNotice,
                noticeType = UserNoticeType.Other
            };

            Dictionary<string, string> tags = new Dictionary<string, string>();
            if (rawMessage.StartsWith("@"))
            {
                int tagEnd = rawMessage.IndexOf(' ');
                tags = ParseTags(rawMessage.Substring(1, tagEnd - 1));
                rawMessage = rawMessage.Substring(tagEnd + 1);
            }

            int messageStart = rawMessage.LastIndexOf(':');
            if (messageStart > 0 && messageStart < rawMessage.Length - 1)
            {
                message.message = rawMessage.Substring(messageStart + 1);
            }

            if (tags.Count > 0)
            {
                ParseUserNoticeTags(ref message, tags);
            }

            return message;
        }

        private Dictionary<string, string> ParseTags(string tagString)
        {
            var tags = new Dictionary<string, string>();
            foreach (string tagPair in tagString.Split(';'))
            {
                int eq = tagPair.IndexOf('=');
                string key = eq < 0 ? tagPair : tagPair.Substring(0, eq);
                string value = eq < 0 ? string.Empty : tagPair.Substring(eq + 1);
                tags[key] = value;
            }

            return tags;
        }

        private void ParseMessageTags(ref ChatMessage message, Dictionary<string, string> tags)
        {
            // Informational only - the authoritative source for redemptions is now
            // TwitchRedemptionManager via Twitch.API.SubscribeToCustomRewardEvents().
            if (tags.TryGetValue("custom-reward-id", out string rewardId))
            {
                message.customRewardId = rewardId;
                message.type = MessageType.ChannelPointRedemption;
            }

            if (tags.TryGetValue("badges", out string badgeStr) && !string.IsNullOrEmpty(badgeStr))
            {
                message.badges = badgeStr.Split(',');
                ApplyBadgeFlags(ref message, message.badges);
            }

            if (tags.TryGetValue("bits", out string bitsStr) && int.TryParse(bitsStr, out int bits))
            {
                message.hasBits = true;
                message.bitsAmount = bits;
                message.type = MessageType.BitsCheer;
            }

            if (tags.TryGetValue("emotes", out string emoteStr) && !string.IsNullOrEmpty(emoteStr))
            {
                message.hasEmotes = true;
                message.emotes = ParseEmotes(emoteStr, message.message);
                if (IsEmoteOnlyMessage(message.message, message.emotes))
                {
                    message.type = MessageType.EmoteOnly;
                }
            }
        }

        private void ParseUserNoticeTags(ref ChatMessage message, Dictionary<string, string> tags)
        {
            if (tags.TryGetValue("login", out string login)) message.username = login;
            if (tags.TryGetValue("system-msg", out string sysMsg)) message.systemMessage = sysMsg.Replace("\\s", " ");

            if (tags.TryGetValue("msg-id", out string msgId))
            {
                switch (msgId)
                {
                    case "sub":
                        message.noticeType = UserNoticeType.Sub;
                        break;
                    case "resub":
                        message.noticeType = UserNoticeType.Resub;
                        if (tags.TryGetValue("msg-param-cumulative-months", out var months))
                            int.TryParse(months, out message.subMonths);
                        break;
                    case "subgift":
                        message.noticeType = UserNoticeType.SubGift;
                        break;
                    case "raid":
                        message.noticeType = UserNoticeType.Raid;
                        if (tags.TryGetValue("msg-param-displayName", out var from)) message.raidFrom = from;
                        if (tags.TryGetValue("msg-param-viewerCount", out var viewers))
                            int.TryParse(viewers, out message.raidViewers);
                        break;
                    case "bitsbadgetier":
                        message.noticeType = UserNoticeType.BitsBadgeTier;
                        break;
                }
            }

            if (tags.TryGetValue("badges", out string badgeStr) && !string.IsNullOrEmpty(badgeStr))
            {
                message.badges = badgeStr.Split(',');
                ApplyBadgeFlags(ref message, message.badges);
            }
        }

        private void ApplyBadgeFlags(ref ChatMessage message, string[] badges)
        {
            foreach (string badge in badges)
            {
                if (badge.StartsWith("subscriber/")) message.isSubscriber = true;
                else if (badge.StartsWith("moderator/")) message.isModerator = true;
                else if (badge.StartsWith("vip/")) message.isVip = true;
                else if (badge.StartsWith("broadcaster/")) message.isBroadcaster = true;
            }
        }

        private EmoteInfo[] ParseEmotes(string emoteString, string messageText)
        {
            var emoteList = new List<EmoteInfo>();
            try
            {
                foreach (string emoteGroup in emoteString.Split('/'))
                {
                    string[] parts = emoteGroup.Split(':');
                    if (parts.Length != 2) continue;
                    string emoteId = parts[0];

                    foreach (string position in parts[1].Split(','))
                    {
                        string[] range = position.Split('-');
                        if (range.Length == 2
                            && int.TryParse(range[0], out int start)
                            && int.TryParse(range[1], out int end)
                            && start < messageText.Length && end < messageText.Length)
                        {
                            emoteList.Add(new EmoteInfo
                            {
                                emoteId = emoteId,
                                emoteName = messageText.Substring(start, end - start + 1),
                                startIndex = start,
                                endIndex = end
                            });
                        }
                    }
                }
            }
            catch
            {
                // Malformed emotes tag - keep whatever we managed to parse.
            }

            return emoteList.ToArray();
        }

        private bool IsEmoteOnlyMessage(string messageText, EmoteInfo[] emotes)
        {
            if (emotes == null || emotes.Length == 0) return false;

            int emoteCharCount = 0;
            foreach (var emote in emotes) emoteCharCount += emote.endIndex - emote.startIndex + 1;

            int nonWhitespaceCount = 0;
            foreach (char c in messageText)
                if (!char.IsWhiteSpace(c))
                    nonWhitespaceCount++;

            return nonWhitespaceCount > 0 && emoteCharCount >= nonWhitespaceCount * 0.8f;
        }
    }
}
