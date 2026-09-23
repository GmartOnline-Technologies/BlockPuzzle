using UnityEngine;
using OneSignalSDK;

public class OneSignalInit : MonoBehaviour
{
    // Replace with your actual OneSignal App ID
    private string _oneSignalAppId = "148027ff-fe79-4da0-ab2d-5b495c96fa64";

    void Start()
    {
        
        // Initialize OneSignal
        OneSignal.Initialize(_oneSignalAppId);

        // Prompt for notification permission (required for iOS & Android 13+)
        OneSignal.Notifications.RequestPermissionAsync(true);

        Debug.Log("OneSignal initialized.");
    }
}