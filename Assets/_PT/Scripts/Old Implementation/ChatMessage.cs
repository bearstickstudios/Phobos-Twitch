using System;

[System.Serializable]
public struct ChatMessage
{
    public MessageType type;
    public string username;
    public string message;
    public DateTime timestamp;
    
    // Channel Points Redeem Info
    public string customRewardId;
        
    // Badge info
    public bool isSubscriber;
    public bool isModerator;
    public bool isVip;
    public bool isBroadcaster;
    public string[] badges;
        
    // Emote info
    public bool hasEmotes;
    public EmoteInfo[] emotes;
        
    // Bits info
    public bool hasBits;
    public int bitsAmount;
        
    // User Notice info
    public UserNoticeType noticeType;
    public string systemMessage;
    public int subMonths;
    public string raidFrom;
    public int raidViewers;
}