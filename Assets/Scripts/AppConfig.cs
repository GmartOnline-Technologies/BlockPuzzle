using UnityEngine;

[CreateAssetMenu(fileName = "AppConfig", menuName = "Configs/AppConfig")]
public class AppConfig : ScriptableObject
{
    [Header("API Settings")]
    public string baseApiUrl = "https://lk-subscription.apphubhost.xyz/api/";
    public string gameUserApiUrl = "https://game-api.apphubhost.xyz/api/users/new";
    public string gameBaseAPIUrl = "https://game-api.apphubhost.xyz"; 
    
    [TextArea(3, 10)]
    public string apiAuthToken = "abcdefg1234";

    [Header("Game API Identity")]
    public string gameAppName = "busArena"; // Instructor value; change only to a backend-approved ID.

    [Header("App IDs")]
    public string androidAppId = "com.GmartOnline.BusArena";
}