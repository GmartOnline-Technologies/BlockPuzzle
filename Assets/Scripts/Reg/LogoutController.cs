using UnityEngine;
using UnityEngine.SceneManagement;

// Assign OnLogoutClicked to the Settings logout button. Local logout, not unsubscribe.
public class LogoutController : MonoBehaviour
{
    public string bootstrapSceneName = "Bootstrap";
    private bool loggingOut;

    public void OnLogoutClicked()
    {
        if (loggingOut) return;
        if (!Application.CanStreamedLevelBeLoaded(bootstrapSceneName))
        { Debug.LogError("Add Bootstrap to Build Settings before logging out.", this); return; }
        loggingOut = true;
        PlayerPrefs.SetInt("IsRegisteredUser", 0);
        PlayerPrefs.SetInt("Auth_LoggedOut", 1);
        PlayerPrefs.DeleteKey("AccessToken");
        PlayerPrefs.DeleteKey("UserId");
        PlayerPrefs.DeleteKey("Username");
        PlayerPrefs.DeleteKey("Mobile");
        PlayerPrefs.DeleteKey("SubscriberId");
        PlayerPrefs.DeleteKey("ReferenceNo");
        PlayerPrefs.Save();
        Debug.Log("[Auth] Logged out. Returning to Bootstrap login.");
        Time.timeScale = 1f;
        SceneManager.LoadScene(bootstrapSceneName);
    }
}
