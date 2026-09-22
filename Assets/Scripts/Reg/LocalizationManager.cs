using UnityEngine;
using System.Collections.Generic;
using TMPro;

public enum Language { English, Sinhala }

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance;
    public Language currentLanguage = Language.English;

    [System.Serializable]
    public struct FontProfile
    {
        public string profileName; // e.g., "Title", "Body", "Digital"
        public TMP_FontAsset englishFont;
        public TMP_FontAsset sinhalaFont;
    }

    public List<FontProfile> fontProfiles = new List<FontProfile>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSavedLanguage(); // Load the setting when the game starts
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public TMP_FontAsset GetFont(string profileName)
    {
        FontProfile profile = fontProfiles.Find(p => p.profileName == profileName);
        
        if (string.IsNullOrEmpty(profile.profileName)) 
            return currentLanguage == Language.Sinhala ? profile.sinhalaFont : profile.englishFont;

        return currentLanguage == Language.Sinhala ? profile.sinhalaFont : profile.englishFont;
    }

    private void LoadSavedLanguage()
    {
        
        if (PlayerPrefs.HasKey("HasSelectedLanguage"))
        {
            
            int savedLang = PlayerPrefs.GetInt("SelectedLanguage", 0);
            currentLanguage = (Language)savedLang;
        }
        else
        {
            
            currentLanguage = Language.English;
        }
    }

    public string GetTranslation(string key)
    {
    
        if (currentLanguage == Language.Sinhala)
        {
            //---Welcome Back---
            if (key == "Welcome Back !") return "wdmiq idorfhka ms<s.ksuq æ";
            if (key == "Login to your account using your Name and Number") return "Tfí ku iy wxlh Ndú;fhka Tfí .sKqug msúfikak'";
            if (key == "Don’t have an account") return ".sKqula fkdue;so@";
            if (key == "Register") return ",shdmÈxÑ jkak";


            //---Subscription Details---
            if (key == "Tutorial") return "f.aï tl .yk úÈy";
            if (key == "Your Details") return "Thdf.a úia;r";
            if (key == "Please fill your details to create game account") return ".sKqula yod.kak úia;r ál mqrjkak";
            if (key == "Your Name") return "Thdf.a ku";
            if (key == "Your Number") return "Thdf.a f*daka kïn¾ tl";
            if (key == "Resend") return "kej; hjkak";
            if (key == "Continue") return "È.gu hkak";
            
            //---Subscription OTP---
            if (key == "Please enter OTP number we text to your number") return "f*daka tlg tjmq ryia wxlh ^OTP& fldgkak";
            if (key == "OTP Number") return "ryia wxlh ^OTP&";
            if (key == "Want to change number") return "f*daka kïn¾ tl udre lrkak ´fko@";
            if (key == "Change number") return "kïn¾ tl udre lrkak";

            //---Welcome---
            if (key == "Start") return "mgka .kak";

            //---Home---
            if (key == "Play") return ".yuq";

            //---Out of Space--
            if (key == "Out of Space") return "bv uÈ æ";
            if (key == "Played Time") return ".ymq fj,dj";

            //---BUY Hammer---
            if (key == "BUY Hammer") return "ñáhla .kak";
            if (key == "Hammer Count") return "ñá .dK";

            //---BUY ROTATORS---
            if (key == "BUY ROTATORS") return "frdfÜg¾ia .kak";
            if (key == "Rotator Count") return "frdfÜg¾ia .dK";

            //---BUY UNDOS---
            if (key == "BUY UNDOS") return "UNDO .kak";
            if (key == "Undo Counts") return "UNDO .dK";
            if (key == "Insufficient Keys") return "Keys uÈ";

            //---Menu---
            if (key == "Menu") return "fukqj";
            if (key == "Restart") return "uq, b|ka";
            if (key == "Settings") return "ieliqï";

            //---Settings---
            if (key == "Sound") return "f.aï tfla ioafoa";
            if (key == "Music") return "ix.S;h";
            if (key == "Language") return "NdIdj";
            if (key == "Please select your language to continue. You can change later") return "bÈßhg hdu i|yd lreKdlr Tfí NdIdj f;darkak' Tng miqj th fjkia l< yel'";
            if (key == "Logout") return "whska fjkak";
            if (key == "Delete Account") return ".sKqu whska lrkak";

            //---Quit ?---
            if (key == "Quit ?") return "whska fjuqo @";
            if (key == "Do you really want to quit?") return "we;a;gu f.aï tflka whska fjkak ´fko@";
            if (key == "Quit") return "whska fjuqo";

            //---RESTART ?---
            if (key == "RESTART ?") return "uq, b|ka huqo @";
            if (key == "Do you want to restart the game ?") return "Thdg wdfh;a uq, b|ka mgka .kak ´fko@";

            //---Leaderboard---
            if (key == "Leaderboard") return "olaIhskaf.a ,ehsia;=j";

            //---Key Store---
            if (key == "Key Store") return "h;=re fIdma tl";

            //---Delete account---
            if (key == "Delete account") return ".sKqu whska lrkak";
            if (key == "Do you want to delete account ?") return "Thdg úYajdio @ Thdf.a o;a; Tlafldu uelS,d hhs'";

            //---Logout---
            if (key == "Log Out ?") return "whska fjuqo@";
            if (key == "Do you want to logout ?") return "Thdg .sKqfuka whska fjkak ´fko@";

            //---Update---
            if (key == "Update Available") return "wmafâÜ tlla weú,a,d";
            if (key == "New update available. Update Block Puzzle now") return "w¨‍;a wmafâÜ tlla weú,a,d' f.aï tl oekau wmafâÜ lr.kak'";
            if (key == "Update now") return "oekau wmafâÜ lrkak";

            //---Connection lost !---
            if (key == "Connection Lost ! Please check your connection and try again.") return "bkag¾fkÜ lfklaIka tfla .eg¨‍jla' lreKdlr mßlaId lr,d wdfh;a g%hs lrkak'";
            if (key == "try again") return "wdfh;a g%hs lrkak";
            if (key == "Connection Lost") return "iïnkaO;djh ì| jegq‚";


        }
        
        // --- DEFAULT ENGLISH KEYS ---

        //---Welcome Back---
            if (key == "Welcome Back !") return "Welcome Back !";
            if (key == "Login to your account using your Name and Number") return "Login to your account using your Name and Number";
            if (key == "Don’t have an account") return "Don’t have an account";
            if (key == "Register") return "Register";


            //---Subscription Details---
            if (key == "Tutorial") return "Tutorial";
            if (key == "Your Details") return "Your Details";
            if (key == "Please fill your details to create game account") return "Please fill your details to create game account";
            if (key == "Your Name") return "Your Name";
            if (key == "Your Number") return "Your Number";
            if (key == "Resend") return "Resend";
            if (key == "Continue") return "Continue";
            
            //---Subscription OTP---
            if (key == "Please enter OTP number we text to your number") return "Please enter OTP number we text to your number";
            if (key == "OTP Number") return "OTP Number";
            if (key == "Want to change number") return "Want to change number";
            if (key == "Change number") return "Change number";

            //---Welcome---
            if (key == "Start") return "Start";

            //---Home---
            if (key == "Play") return "Play";

            //---Out of Space--
            if (key == "Out of Space") return "Out of Space";
            if (key == "Played Time") return "Played Time";

            //---BUY Hammer---
            if (key == "BUY Hammer") return "BUY Hammer";
            if (key == "Hammer Count") return "Hammer Count";

            //---BUY ROTATORS---
            if (key == "BUY ROTATORS") return "BUY ROTATORS";
            if (key == "Rotator Count") return "Rotator Count";

            //---BUY UNDOS---
            if (key == "BUY UNDOS") return "BUY UNDOS";
            if (key == "Undo Counts") return "Undo Counts";
            if (key == "Insufficient Keys") return "Insufficient Keys";

            //---Menu---
            if (key == "Menu") return "Menu";
            if (key == "Restart") return "Restart";
            if (key == "Settings") return "Settings";

            //---Settings---
            if (key == "Sound") return "Sound";
            if (key == "Music") return "Music";
            if (key == "Language") return "Language";
            if (key == "Please select your language to continue. You can change later") return "Please select your language to continue. You can change later";
            if (key == "Logout") return "Logout";
            if (key == "Delete Account") return "Delete Account";

            //---Quit ?---
            if (key == "Quit ?") return "Quit ?";
            if (key == "Do you really want to quit?") return "Do you really want to quit?";
            if (key == "Quit") return "Quit";

            //---RESTART ?---
            if (key == "RESTART ?") return "RESTART ?";
            if (key == "Do you want to restart the game ?") return "Do you want to restart the game ?";

            //---Leaderboard---
            if (key == "Leaderboard") return "Leaderboard";

            //---Key Store---
            if (key == "Key Store") return "Key Store";

            //---Delete account---
            if (key == "Delete account") return "Delete account";
            if (key == "Do you want to delete account ?") return "Do you want to delete account ?";

            //---Logout---
            if (key == "Log Out ?") return "Log Out ?";
            if (key == "Do you want to logout ?") return "Do you want to logout ?";

            //---Update---
            if (key == "Update Available") return "Update Available";
            if (key == "New update available. Update Block Puzzle now") return "New update available. Update Block Puzzle now";
            if (key == "Update now") return "Update now";

            //---Connection lost !---
            if (key == "Connection Lost ! Please check your connection and try again.") return "Connection Lost ! Please check your connection and try again.";
            if (key == "try again") return "try again";
            if (key == "Connection Lost") return "Connection Lost";
            
        
        return key;
    }

    public void SetLanguage(int langIndex)
    {
        currentLanguage = (Language)langIndex;
        
        
        PlayerPrefs.SetInt("SelectedLanguage", langIndex);
        PlayerPrefs.SetInt("HasSelectedLanguage", 1);
        PlayerPrefs.Save(); 

        // Trigger UI updates
        LocalizedText[] allTexts = FindObjectsByType<LocalizedText>(
            FindObjectsInactive.Include, // This is the crucial addition
            FindObjectsSortMode.None
        );

        foreach (var text in allTexts) 
        {
            text.UpdateUI();
        }
    }

    public bool IsLanguageSelected()
    {
        return PlayerPrefs.GetInt("HasSelectedLanguage", 0) == 1;
    }

}