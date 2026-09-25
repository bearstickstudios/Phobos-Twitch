using System;
using System.Collections.Generic;
using UnityEngine;

public class TwitchChatAdapter : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string channel = "your_channel";
    [SerializeField] private TwitchAuthManager authManager;
    
    public TwitchClient Client { get; private set; }
    public event Action<ChatMessage> OnMessageReceived;
    
    private void Start()
    {
        if (authManager.IsAuthenticated)
        {
            ConnectToTwitch();
        }
    }
    
    private void ConnectToTwitch()
    {
        Client = new TwitchClient();
        Client.Initialize(channel); 
        
        Client.OnMessageReceived += HandleSDKMessage;
        Client.OnUserNoticeReceived += HandleSDKNotice;
        
        Client.Connect();
        Debug.Log($"Connected via Twitch.Unity SDK to channel: #{channel}");
    }
    
    private void OnDestroy()
    {
        if (Client != null)
        {
            Client.OnMessageReceived -= HandleSDKMessage;
            Client.OnUserNoticeReceived -= HandleSDKNotice;
            Client.Disconnect();
        }
    }
    
    private void HandleSDKMessage(object sender, MessageEventArgs e)
    {
        var message = new ChatMessage
        {
            timestamp = DateTime.Now,
            username = e.Message.Username,
            message = e.Message.MessageText,
            type = e.Message.Bits > 0 ? MessageType.BitsCheer : MessageType.RegularChat,
            bitsAmount = e.Message.Bits,
            isSubscriber = e.Message.IsSubscriber,
            isModerator = e.Message.IsModerator,
            isVip = e.Message.IsVip
        };
        
        if (e.Message.Emotes != null && e.Message.Emotes.Count > 0)
        {
            message.hasEmotes = true;
            message.emotes = MapSDKEmotes(e.Message.Emotes);
        }
        
        OnMessageReceived?.Invoke(message);
    }
    
    private void HandleSDKNotice(object sender, UserNoticeEventArgs e)
    {
        var notice = new ChatMessage
        {
            timestamp = DateTime.Now,
            type = MessageType.UserNotice,
            username = e.Notice.Username
        };
        OnMessageReceived?.Invoke(notice);
    }
    
    private EmoteInfo[] MapSDKEmotes(List<TwitchEmote> sdkEmotes)
    {
        var emoteList = new List<EmoteInfo>();
        foreach (var sdkEmote in sdkEmotes)
        {
            emoteList.Add(new EmoteInfo
            {
                emoteId = sdkEmote.Id,
                emoteName = sdkEmote.Name,
                startIndex = sdkEmote.StartIndex,
                endIndex = sdkEmote.EndIndex
            });
        }
        return emoteList.ToArray();
    }
}