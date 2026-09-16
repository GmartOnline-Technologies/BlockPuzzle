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
            //--- FirstRound---
            if (key == "fstrd") return "m<uq jgh";
            if (key == "easy") return "f,aishs";
            if (key == "normal") return "idudkHhs";
            if (key == "hard") return "wudrehs";
            if (key == "help") return "Woõ";
            if (key == "grid") return "oe,";
            if (key == "hammer") return "ñáh";

            //--- Collect Mobile Number---
            if (key == "congratu") return "iqn me;=ï";
            if (key == "enter name and mobile") return "Thdf.a kuhs f*daka kïn¾ tlhs fldgkak'";
            if (key == "enter name") return "ku fldgkak";
            if (key == "verify") return ";yjqre lrkak";

             //--- OTP--
            if (key == "enterotp") return "ryia wxlh fldgkak";
            if (key == "ples enter otp") return "f*daka tlg tjmq ryia wxlh .ykak";
            if (key == "didnt receive otp") return "fldaâ tl wdfõ keoao@";
            if (key == "resendotp") return "wdfh f.kak .kak";
            if (key == "resendavailable") return "0•59 lska wdfh;a f.kak .kak mq¿jka";
            
            //--- Congrats--
            if (key == "congrats") return "iqn me;=ï";
           if (key == "congratsdescri") return ";rÕldÍ f.aï .ykak\" ,hsõ bfjkaÜia n,kak lshdmq ;ek'"; 
           if (key == "start") return "mgka .kak";
           
            //--- Login--
           if (key == "welcomeback") return "h,s;a idorfhka ms<s.kakjd";
           if (key == "enteryournameandmobilenumtologin") return ".sKqug we;=,a fjkak Thdf.a kuhs f*daka kïn¾ tlhs fldgkak'";
           if (key == "enteryournum") return "ÿrl:k wxlh we;=,;a lrkak";
           if (key == "login") return "we;=,a fjkak";
           
            //--- Home--
           if (key == "1daystrike") return "m<fjks ojfia iag%hsla tl";
           if (key == "completedailystrike") return "ojfia iag%hsla tl iïmQ¾K lrkak";
           if (key == "dailystrikecompleted") return "ojfia iag%hsla tl wjika";

            //--- Calendar--
           if (key == "calendar") return "le,ekavrh";
           if (key == "thismonthprogress") return "fï udfia m%.;sh";

            //---Completed--
           if (key == "yousuccessfullycompleted..") return "Thd fâ,s iag%hsla tl id¾:lj bjr l<d";
           if (key == "timespent") return ".shmq fj,dj";
           if (key == "playagain") return "wdfh;a .yuq";
           if (key == "cancel") return "tmd";

            //---Game menu---
           if (key == "resume") return "wdfh;a mgka .kak";
           if (key == "restartlvl") return "uq, b|ka";
           if (key == "home") return "m%Odk msgqjg";

            //---Quit---
            if (key == "quittohome") return "f.aï tflka whska fjuqo@";
            if (key == "doyoureallywanttoquit") return "we;a;gu f.aï tflka whska fjkak ´fko@";
            if (key == "exit") return "whska fjkak";

            //---Restart---
            if (key == "restart") return "uq, b|ka huqo@";
            if (key == "doyouwanttorestart") return "Thdg wdfh;a uq, b|ka mgka .kak ´fko@";

            //---Level Completed---
            if (key == "lvlcompleted") return "f,j,a tl bjrhs";
            if (key == "nextlvl") return "B<. f,j,a tl";
            
            //---Out of lives---
            if (key == "outoflives") return ",hs*a bjrhs";
            if (key == "buy") return "ñ,§ .kak";
            if (key == "insufficientpoints") return "fmdhskaÜia uÈ";
            if (key == "outoflivesdesc") return "l%Svdj kej; wdrïN lrkak fyda uq,a msgqjg hkak¡";

            //---Coin Store---
            if (key == "coinstore") return "fldhskaia fIdma tl";

            //---Buy---
            if (key == "150coinpack") return "150 mela tl";
            if (key == "wouldyouliketopurchase") return "Thdg fï fldhska meflaÊ tl .kak ´fko @";

            //---Success---
            if (key == "purchasesuccess") return "id¾:lj ñ,§ .;a;d";
            if (key == "collect") return "tl;= lr.kak";

            //---Payment Declined---
            if (key == "paymentdeclined") return "f.ùu m%;slafIam jqKd";
            if (key == "paymentdeclineddescription") return "f.ùfï§ fudlla yß .egÆjla wdjd' wdfh;a g%hs lr,d n,kak";
            if(key == "done") return "yß";

            //---Settings---
            if (key == "settings") return "ieliqï";
            if (key == "yourname") return "Thdf.a ku";
            if (key == "mobilenum") return "ÿrl:k wxlh";
            if (key == "gamesound") return "f.aï tfla ioafoa";
            if (key == "language") return "NdIdj";
            if (key == "logout") return "whska fjkak";
            if (key == "deactivateacc") return ".sKqu wl%sh lrkak";   

            //---Logout---
            if (key == "logout?") return "whska fjuqo@";
            if (key == "logoutdesc") return "Thdg .sKqfuka whska fjkak ´fko@ f*daka kïn¾ tflka f,aisfhkau wdfh;a f,d.a fjkak mq¿jka";

            //---Deactivate Account---
            if (key == "deactivateacc") return ".sKqu wl%sh lrkak";
            if (key == "deactivateaccdesc") return "úYajdio @ Thdf.a o;a; Tlafldu uelS,d hhs'";
            if (key == "deactivate") return "wl%sh lrkak";

            //---Change Language---
            if (key == "doyouwanttochangethegamelanguageintosinhala") return "Thdg f.aï tfla NdIdj isxy, j,g udre lrkak ´fko @";
            if (key == "yeschange") return "Tõ udre lrkak";

            //---Update ---
            if (key == "updateavailable") return "wmafâÜ tlla weú,a,d";
            if (key == "newupdateavailable") return "wÆ;a wmafâÜ tlla weú,a,d' f.aï tl oekau wmafâÜ lr.kak'";
            if (key == "updatenow") return "oekau wmafâÜ lrkak";

            //---Connection Lost ---
            if (key == "connectionlostdesc") return "bkag¾fkÜ lfklaIka tfla .egÆjla' lreKdlr mßlaId lr,d wdfh;a g%hs lrkak'";
            if (key == "tryagain") return "wdfh;a g%hs lrkak";  

            //---Helps ---
            if (key == "outofhelps") return "Woõ wjika";
            if (key == "helpoutofhelpsdesc") return "Woõ ñ,§ .kak fyda l%Svdj È.gu lrf.k hkak'";

            //---Hammers ---
            if (key == "outofhammers") return "ñá wjika";
            if (key == "hammersoutofdesc") return "ñá ñ,§ .kak fyda l%Svdj È.gu lrf.k hkak'";

        }
        
        // --- DEFAULT ENGLISH KEYS ---

        //--- FirstRound---
            if (key == "fstrd") return "First Round ";
            if (key == "easy") return "Easy";
            if (key == "normal") return "Normal";
            if (key == "hard") return "Hard";
            if (key == "help") return "Help";
            if (key == "grid") return "Grid";
            if (key == "hammer") return "Hammer";

            //--- Collect Mobile Number---
            if (key == "congratu") return "CONGRATULATIONS !";
            if (key == "enter name and mobile") return "Enter your name and mobile number.";
            if (key == "enter name") return "Enter name";
            if (key == "verify") return "Verify";

             //--- OTP--
            if (key == "enterotp") return "Enter OTP !";
            if (key == "ples enter otp") return "Please enter OTP we text to your number";
            if (key == "didnt receive otp") return "Didn't receive code?";
            if (key == "resendotp") return "Resend OTP";
            if (key == "resendavailable") return "Resend available in 0:59";
            
            //--- Congrats--
           if (key == "congratsdescri") return "Your ultimate destination for competitive gaming, live events, and unforgettable experiences."; 
           if (key == "start") return "START";
           
            //--- Login--
           if (key == "welcomeback") return "Welcome back !";
           if (key == "enteryournameandmobilenumtologin") return "Enter your name and mobile number to login to your account.";
           if (key == "enteryournum") return "Enter your Number";
           if (key == "login") return "Login";
           
            //--- Home--
           if (key == "1daystrike") return "1St Day strike";
           if (key == "completedailystrike") return "Complete your daily strike";
           if (key == "dailystrikecompleted") return "Daily Strike Completed";

            //--- Calendar--
           if (key == "calendar") return "Calendar";
           if (key == "thismonthprogress") return "This month's progress";

            //---Completed--
           if (key == "yousuccessfullycompleted..") return "You successfully completed daily strike on";
           if (key == "timespent") return "Time spent";
           if (key == "playagain") return "PLay Again";
           if (key == "cancel") return "Cancel";

            //---Game menu---
           if (key == "resume") return "Resume";
           if (key == "restartlvl") return "Restart Level";
           if (key == "home") return "Home";

            //---Quit---
            if (key == "quittohome") return "QUIT to Home ?";
            if (key == "doyoureallywanttoquit") return "Do you really want to quit?";
            if (key == "exit") return "Exit";

            //---Restart---
            if (key == "restart") return "Restart";
            if (key == "doyouwanttorestart") return "Do you want to restart the game ?";

            //---Level Completed---
            if (key == "lvlcompleted") return "Level COMPLETED !";
            if (key == "nextlvl") return "NEXT LEVEL";
            
            //---Out of lives---
            if (key == "outoflives") return "Out of Lives !";
            if (key == "buy") return "Buy";
            if (key == "insufficientpoints") return "Insufficient Points";

            //---Coin Store---
            if (key == "coinstore") return "Coin Store";

            //---Buy---
            if (key == "150coinpack") return "150 Coin Pack";
            if (key == "wouldyouliketopurchase") return "Would you like to purchase this coin package ?";

            //---Success---
            if (key == "purchasesuccess") return "Purchase Success !";
            if (key == "collect") return "Collect";

            //---Payment Declined---
            if (key == "paymentdeclined") return "Payment Declined";
            if (key == "paymentdeclineddescription") return "Something went wrong with the payment. Please try again";
             if(key == "done") return "Done";

            //---Settings---
            if (key == "settings") return "Settings";
            if (key == "yourname") return "Your Name";
            if (key == "mobilenum") return "Mobile Number";
            if (key == "gamesound") return "Game Sound";
            if (key == "language") return "Language";
            if (key == "logout") return "Logout";
            if (key == "deactivateacc") return "Deactivate Account";   

            //---Logout---
            if (key == "logout?") return "Logout?";
            if (key == "logoutdesc") return "Are you sure you want to logout?";    

            //---Deactivate Account---
            if (key == "deactivateacc") return "DEACTIVATE ACCOUNT";
            if (key == "deactivateaccdesc") return "Are you sure to deactivate your account ? Your data will be removed";
            if (key == "deactivate") return "DEACTIVATE";

            //---Change Language---
            if (key == "doyouwanttochangethegamelanguageintosinhala") return "Do you want to change the game language in to Sinhala ?";
            if (key == "yeschange") return "Yes. Change";

            //---Update ---
            if (key == "updateavailable") return "Update Available";
            if (key == "newupdateavailable") return "New update available. Update Arrow Arena now";
            if (key == "updatenow") return "Update Now";

            //---Connection Lost ---
            if (key == "connectionlostdesc") return "Connection Lost ! Please check your connection and try again.";
            if (key == "tryagain") return "try again";  

             //---Helps ---
            if (key == "outofhelps") return "OUT OF HELPS !";
            if (key == "helpoutofhelpsdesc") return "Buy helps or continue the game.";

            //---Hammers ---
            if (key == "outofhammers") return "OUT OF HAMMERS !";
            if (key == "hammersoutofdesc") return "Buy hammers or continue the game.";
        
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