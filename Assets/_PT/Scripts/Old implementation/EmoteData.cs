[System.Serializable]
public struct EmoteData
{
    public string emoteId;
    public string emoteName;
    public string imageUrl;
    
    public EmoteData(string id, string name)
    {
        emoteId = id;
        emoteName = name;
        // Construct Twitch emote URL - using 2.0 size (56x56 pixels)
        // Format: https://static-cdn.jtvnw.net/emoticons/v2/{emote_id}/default/{theme_mode}/{scale}
        imageUrl = $"https://static-cdn.jtvnw.net/emoticons/v2/{id}/default/dark/2.0";
    }
    
    public static EmoteData[] FromEmoteInfoArray(EmoteInfo[] emoteInfos)
    {
        EmoteData[] emoteDataArray = new EmoteData[emoteInfos.Length];
        for (int i = 0; i < emoteInfos.Length; i++)
        {
            emoteDataArray[i] = new EmoteData(emoteInfos[i].emoteId, emoteInfos[i].emoteName);
        }
        return emoteDataArray;
    }
}