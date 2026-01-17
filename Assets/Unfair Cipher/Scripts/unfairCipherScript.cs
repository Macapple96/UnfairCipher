using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using KModkit;
using System;
using System.Text.RegularExpressions;

public class unfairCipherScript : MonoBehaviour
{

    #region unityStuff

    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule module;

    public KMSelectable[] buttons;


    public TextMesh idScreen;
    public TextMesh screen;
    public Material[] LEDState;
    public Renderer[] LEDS;
    public Light[] shinylights;
    public AudioClip[] sounds;


    List<Cell> matrix = new List<Cell>();

    #endregion

    #region Variables


    static int moduleIdCounter = 1;
    int _moduleId;

    private int portPlates;
    private int batHolders;
    int colorpresses = 0;
    //int mits = 0;
    int strikeCounter = 0;
    int lastStrikeCount;
    int buttonint;
    int offset;

    string day = DateTime.Now.DayOfWeek.ToString();
    int dayInt;
    int month = DateTime.Now.Month;

    int stage = 1;


    bool solved;
    bool ZenModeActive;
    private bool TwitchPlaysSkipTimeAllowed = true;
    bool TimeModeActive;
    bool live = false;
    bool idScreenShow = true;

    private static string version = "1.1.5.4"; // Updated Jan 16 2026
    private string[] orders = { "PCR", "PCG", "PCB", "SUB", "MIT", "CHK", "PRN", "BOB", "REP", "EAT", "STR", "IKE" };
    string[] Message = new string[4];
    private string message;
    private string encMessage;
    private string encEncMessage;

    private string keyA;
    private string keyB;
    private string keyC;

    string currentOrder;
    string previousOrder = "None";

    string correctAnswer;

    string userInput;
    string logVerboseAnswer;
    string previousVerboseAnswer = "None";

    private string[] messagerand;

    private KMSelectable previousinput;

    #endregion

    #region Awake Start Activate

    void Awake()
    {

        //_moduleId = 27;
        _moduleId = moduleIdCounter++; //beh

        float scalar = transform.lossyScale.x;
        for (var i = 0; i < shinylights.Length; i++)
            shinylights[i].range *= scalar;


    }

    void Start()
    {
        StartCoroutine(AwakeProcess());

    }

    #endregion

    private void restart(bool strike = true)
    {
        live = true;
        stage = 1;
        colorpresses = 0;
        previousOrder = "None";
        previousinput = null;
        //previousVerboseAnswer = "";

        if (strike)
        {
            StartCoroutine(Strike());
        }
        else
        {
            verboseAction();
            resetLeds();
        }
    }



    static string makeRoman(int number)
    {
        if (number < 0)
        {
            return string.Empty;
        }
        if (number < 1)
        {
            return string.Empty;
        }
        if (number >= 100)
        {
            return "C" + makeRoman(number - 100);
        }
        if (number >= 90)
        {
            return "XC" + makeRoman(number - 90);
        }
        if (number >= 50)
        {
            return "L" + makeRoman(number - 50);
        }
        if (number >= 40)
        {
            return "XL" + makeRoman(number - 40);
        }
        if (number >= 10)
        {
            return "X" + makeRoman(number - 10);
        }
        if (number >= 9)
        {
            return "IX" + makeRoman(number - 9);
        }
        if (number >= 5)
        {
            return "V" + makeRoman(number - 5);
        }
        if (number >= 4)
        {
            return "IV" + makeRoman(number - 4);
        }
        if (number >= 1)
        {
            return "I" + makeRoman(number - 1);
        }
        return number.ToString();
    }

    IEnumerator Strike()
    {
        yield return null;
        live = false;
        module.HandleStrike();
        strikeCounter++;

        int flash = 0;

        while (flash < 10)
        {

            foreach (var led in LEDS)
            {


                led.sharedMaterial = LEDState[2];


            }
            if (flash < 5)
            {
                Audio.PlaySoundAtTransform(sounds[0].name, transform);
            }
            screen.color = Color.red;

            yield return new WaitForSeconds(0.05f);

            foreach (var led in LEDS)
            {
                led.sharedMaterial = LEDState[0];
            }
            screen.color = Color.white;

            yield return new WaitForSeconds(0.05f);

            flash++;
        }
        verboseAction();
        resetLeds();
        live = true;
    }

    #region twitchPlays

#pragma warning disable 0414



    public string TwitchHelpMessage = "Select the given button with “!{0} press R(ed);G(reen);B(lue);Inner;Outer” " +
        "To time a specific press, specify as many time stamps based only on seconds digits (##), full time stamp (DD:HH:MM:SS), or MM:SS where MM exceeds 99 min. " +
        "Press the screen with, “!{0} screen” Semicolons can be used to combine presses, both untimed and timed. (Example: “!{0} r 50 40 30 20 10 00;outer;inner”)";

#pragma warning restore 0414

    public void TwitchHandleForcedSolve()
    {
        live = false;
        DebugMsg("Module force solved by command");
        StopAllCoroutines();
        StartCoroutine(Solve());
    }

    public IEnumerator ProcessTwitchCommand(string cmd)
    {
        if (solved || !live)
        {
            yield return "sendtochaterror The module is not accepting inputs right now.";
            yield break;
        }
        var intCmd = cmd.ToLowerInvariant().Trim();
        var timeStampsCmdPart = new List<List<long>>();
        var intCmdParts = intCmd.Split(';');
        var btnsToPress = new List<KMSelectable>();

        var multiplierTimes = new[] { 1, 60, 3600, 86400 }; // To denote seconds, minutes, hours, days in seconds.

        foreach (string cmdPart in intCmdParts)
        {
            var partTrimmed = cmdPart.Trim();
            if (partTrimmed.RegexMatch(@"^press "))
                partTrimmed = partTrimmed.Substring(5).Trim();
            var partOfPartTrimmed = partTrimmed.Split();
            if (Regex.IsMatch(partTrimmed, @"^(R(ed)?|G(reen)?|B(lue)?|Inner|Outer|Screen)(\s+(at|on))?(\s+(([0-9]+)(:[0-5][0-9]){1,3}))+$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
            {
                var possibleTimes = new List<long>();
                var timeStamps = partOfPartTrimmed.Where(a => a.RegexMatch(@"^[0-9]+(:[0-5][0-9]){1,3}$")).ToList();
                foreach (string aTime in timeStamps)
                {
                    var curTimePart = aTime.Split(':').Reverse().ToArray();
                    long curTime = 0;
                    for (var idx = 0; idx < curTimePart.Length; idx++)
                    {
                        long possibleValue;
                        if (!long.TryParse(curTimePart[idx], out possibleValue))
                        {
                            yield return string.Format("sendtochaterror The command portion: \"{0}\" contains an uncalculatable time. The full command has been voided.", cmdPart);
                            yield break;
                        }
                        curTime += multiplierTimes[idx] * possibleValue;
                    }
                    possibleTimes.Add(curTime);
                }
                possibleTimes = possibleTimes.Where(a => ZenModeActive ? a > Bomb.GetTime() : a < Bomb.GetTime()).ToList();

                if (possibleTimes.Any())
                    timeStampsCmdPart.Add(possibleTimes);
                else
                {
                    yield return string.Format("sendtochaterror The command portion: \"{0}\" gave no possible times for this module. The full command has been voided.", cmdPart);
                    yield break;
                }
            }
            else if (Regex.IsMatch(partTrimmed, @"^(R(ed)?|G(reen)?|B(lue)?|Inner|Outer|Screen)(\s+(at|on))?(\s+[0-5][0-9])+$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
            {
                var possibleTimes = new List<long>();
                var curMinRemaining = (long)Bomb.GetTime() / 60;
                var timeStamps = partOfPartTrimmed.Where(a => a.RegexMatch(@"^[0-5][0-9]$")).ToList(); // Filter out all that are 00-59 inclusive.
                foreach (var secondsPart in timeStamps)
                {
                    int secondsTime = int.Parse(secondsPart); // Always within 0 - 59 inclusive due to restricting the via the regex above.
                    for (long t = curMinRemaining - (ZenModeActive ? 0 : 2); t <= curMinRemaining + (ZenModeActive ? 2 : 0); t++)
                    {
                        var newTime = t * 60 + secondsTime;
                        if ((ZenModeActive && newTime > Bomb.GetTime()) ||
                            (!ZenModeActive && newTime < Bomb.GetTime()))
                            // If Zen mode is active, check if the new time is larger than the current time.
                            // If Zen mode is inactive, check if the new time is shorter than the current time.
                            possibleTimes.Add(newTime);
                    }
                }

                if (possibleTimes.Any())
                    timeStampsCmdPart.Add(possibleTimes);
                else
                {
                    yield return string.Format("sendtochaterror The command portion: \"{0}\" gave no possible times for this module. The full command has been voided.", cmdPart);
                    yield break;
                }
            }
            else if (Regex.IsMatch(partTrimmed, @"^(R(ed)?|G(reen)?|B(lue)?|Inner|Outer|Screen)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
                timeStampsCmdPart.Add(new List<long>());
            else
            {
                yield return string.Format("sendtochaterror The command portion: \"{0}\" is not valid. Check your command for typos.", cmdPart);
                yield break;
            }
            // This entire part below is for adding what button to press for this module to perform.
            switch (partOfPartTrimmed[0])
            {
                case "r":
                case "red":
                    btnsToPress.Add(buttons[0]); // Red
                    break;
                case "g":
                case "green":
                    btnsToPress.Add(buttons[1]); // Green
                    break;
                case "b":
                case "blue":
                    btnsToPress.Add(buttons[2]); // Blue
                    break;
                case "inner":
                    btnsToPress.Add(buttons[3]); // Inner "Center"
                    break;
                case "outer":
                    btnsToPress.Add(buttons[4]); // "Outer" Center
                    break;
                case "screen":
                    btnsToPress.Add(buttons[5]); // Screen
                    break;
                default:
                    yield break;
            }
        }

        if (btnsToPress.Any())
        {
            var willStrike = false;
            yield return "multiple strikes";
            for (var x = 0; x < btnsToPress.Count; x++)
            {
                yield return null;
                if (willStrike) yield break;
                if (timeStampsCmdPart[x].Any() && btnsToPress[x] == buttons[5])
                    yield return string.Format("sendtochat /me Kappa was it really necessary to time a screen press for press #{1} in your command, {0}?", "{0}", x + 1);
                if (timeStampsCmdPart[x].Any())
                {
                    #region TPTimeDetection
                    var curTimeThresholds = timeStampsCmdPart[x].Where(a => ZenModeActive ? a > Bomb.GetTime() : a < Bomb.GetTime()).ToList();
                    if (!curTimeThresholds.Any())
                    {
                        yield return string.Format("sendtochaterror Your timed interaction has been canceled. There are no remaining times left for press #{0} in the command that was sent.", x + 1);
                        yield break;
                    }
                    long targetTime = ZenModeActive ? curTimeThresholds.Min() : curTimeThresholds.Max();
                    yield return string.Format("sendtochat Target time for press #{0} in command: {1}", x + 1,
                        string.Format("{0}:{1}", targetTime / 60, (targetTime % 60).ToString("00")));
                    var canSkipTime = ZenModeActive ? targetTime > 15 + Bomb.GetTime() : targetTime < Bomb.GetTime() - 15;
                    var canPlayMusic = ZenModeActive ? targetTime > 10 + Bomb.GetTime() : targetTime < Bomb.GetTime() - 10;
                    if (canSkipTime) // The previous TP Support had a time skip feature that was used to make the module more convenient. Figured I add that back here.
                        yield return string.Format("skiptime {0}", targetTime + (ZenModeActive ? -5 : 5));
                    else if (canPlayMusic)
                    {
                        yield return "waiting music";
                        yield return "sendtochat This press may take a long time, if you wish to cancel this command, do \"!cancel\" now.";
                    }
                    do
                    {
                        yield return string.Format("trycancel Your timed interaction has been canceled after a total of {0}/{1} presses in the command.", x + 1, btnsToPress.Count);
                        if ((long)Bomb.GetTime() > targetTime && ZenModeActive)
                        {
                            curTimeThresholds = curTimeThresholds.Where(a => a > Bomb.GetTime()).ToList();
                            if (!curTimeThresholds.Any())
                            {
                                yield return string.Format("sendtochaterror Your timed interation has been canceled. There are no remaining times left for press #{0} in the command that was sent.", x + 1);
                                yield break;
                            }
                            targetTime = curTimeThresholds.Min();
                            yield return string.Format("sendtochat Your timed interaction has been altered. The new time is now {1} for press #{0} in the command that was sent.",
                                x + 1, string.Format("{0}:{1}", targetTime / 60, (targetTime % 60).ToString("00")));
                        }
                        else if ((long)Bomb.GetTime() < targetTime && !ZenModeActive)
                        {
                            curTimeThresholds = curTimeThresholds.Where(a => a < Bomb.GetTime()).ToList();
                            if (!curTimeThresholds.Any())
                            {
                                yield return string.Format("sendtochaterror Your timed interaction has been canceled. There are no remaining times left for press #{0} in the command that was sent.", x + 1);
                                yield break;
                            }
                            targetTime = curTimeThresholds.Max();
                            yield return string.Format("sendtochat Your timed interaction has been altered. The new time is now {1} for press #{0} in the command that was sent.",
                                x + 1, string.Format("{0}:{1}", targetTime / 60, (targetTime % 60).ToString("00")));
                        }
                    }
                    while ((long)Bomb.GetTime() != targetTime);
                    #endregion
                }
                var curBtn = btnsToPress[x];
                #region TPStrikeDetection
                var possibleButtons = new[] { "Red", "Green", "Blue", "Inner", "Outer", "Screen" };
                var buttonName = buttons.Contains(curBtn) ? possibleButtons[Array.IndexOf(buttons, curBtn)] : "Screen";
                if (buttonName != "Screen" && !IsBtnCorrect(curBtn) && btnsToPress.Count > 1)
                {
                    willStrike = true;
                    yield return string.Format("strikemessage incorrectly pressing {0} on {1} after {2} press{3} in the TP command specified!", buttonName == "Center" ? "Inner Center" : buttonName == "Outer" ? "Outer Center" : buttonName, Bomb.GetFormattedTime(), x + 1, x == 0 ? "" : "es");
                }
                #endregion
                curBtn.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            yield return "end multiple strikes";
        }

    }

    private bool IsBtnCorrect(KMSelectable userinput) // Method used for strike detection.
    {
        int bombSeconds = (int)Bomb.GetTime() % 60;
        var button = userinput.name;

        switch (currentOrder)
        {
            case "PCR":
                return button == "R";
            case "PCG":
                return button == "G";
            case "PCB":
                return button == "B";
            case "SUB":
                return button == "Outer" && bombSeconds % 11 == 0;
            case "MIT":
                return button == "Center" && Modulo(bombSeconds, 10) == Modulo(_moduleId + colorpresses + stage, 10);
            case "CHK":
                {
                    var primesLt20 = new[] { 2, 3, 5, 7, 11, 13, 17, 19 };
                    return (primesLt20.Contains(_moduleId % 20) && button == "Outer") || (!primesLt20.Contains(_moduleId % 20) && button == "Center");
                }
            case "PRN":
                {
                    var primesLt20 = new[] { 2, 3, 5, 7, 11, 13, 17, 19 };
                    return (primesLt20.Contains(_moduleId % 20) && button == "Center") || (!primesLt20.Contains(_moduleId % 20) && button == "Outer");
                }
            case "BOB":
                return button == "Center";
            case "STR":
            case "IKE":
                {
                    string[] rgb = { "R", "G", "B" };
                    return rgb[strikeCounter % 3] == button;
                }
            case "REP":
            case "EAT":
                {
                    if (previousOrder == "None")
                        return button == "Center";
                    else
                        return previousinput.name == button;
                }

            default:
                DebugMsg("Dafuq? Default? How?");
                return true;
        }
    }

    #endregion

    void resetLeds()
    {


        foreach (var led in LEDS)
        {
            led.sharedMaterial = LEDState[0];
        }


    }

    IEnumerator AwakeProcess()
    {
        yield return null;

        //Audio should play here, some leds will slowly light up

        DebugMsg(":::Unfair Cipher Version: " + version+":::");

        screen.text = "";
        idScreen.text = "";



        string[] welcometextarray = { "THIS WILL BE FUN\n:)))))))", "HEXADECIMALIZING", "I AM NOT SIMON", "42\n42\n42", "GET IN THE ROBOT\nSHINJI", "BEING MEGUCA IS\nSUFFERING\n/人◕ ‿‿ ◕人\\", "IT'S AN ANGERU\n :o", "IT'S AN ANJANATH\n :O", "YOU ACTIVATED MY\nTRAP CARD", "DIFFICULTY:\nUNFAIR", "CHECKMATE.", "SORRY... :)", "YOU'LL HAVE A\nBAD TIME", "GET\nTHRASHED", "CASTING METEOR:\n▒▒▒▒▒▒▒▒▒▒▒▒▒", "RIICHI, IPPATSU\nJUNCHAN, DORA", "SO ZETTA SLOW!", "FACTORIAL!", "SOHCAHTOA", "INVERSE MATRIX!", "DROWN IN THE\nDIRAC SEA", "3.14159265358979\n3238462643383279", "QED. CLASS IS\nEXPLODED", "PREPARE TO BE\nITERATED!","THE GREAT BERATE\nYOU MADLADS <3" };
        string welcometext = welcometextarray[UnityEngine.Random.Range(0, welcometextarray.Length)];
        StringBuilder screentext = new StringBuilder();
        screen.color = Color.red;

        for (int i = 0; i < welcometext.Length; i++)
        {
            screentext.Append(welcometext.ToCharArray()[i]);
            screen.text = screentext.ToString();
            yield return new WaitForSeconds(0.1f);
        }

        /*/
        
        yield return new WaitForSeconds(1f);

        Audio.PlaySoundAtTransform(sounds[0].name, transform);
        
        /*/

        yield return new WaitForSeconds(3f);


        screen.text = "";
        screen.color = Color.white;

        //The module is technically live from the moment text appears on it

        portPlates = Bomb.GetPortPlateCount();
        batHolders = Bomb.GetBatteryHolderCount();
        idScreen.text = makeRoman(_moduleId);
        generateKeys();
        rollActions();

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].OnInteract += HandlePress(i);
        }

        restart(false);

        module.OnActivate += Activate;

    }

    void Activate()
    {
        //DebugMsg("Eskere");
    }

    IEnumerator solveFlash()
    {
        yield return null;
        int flash = 0;
        
        while (flash < 10)
        {
            foreach (var led in LEDS) led.sharedMaterial = LEDState[1];

            screen.color = Color.green;

            yield return new WaitForSeconds(0.05f);
            foreach (var led in LEDS) led.sharedMaterial = LEDState[0];
            screen.color = Color.white;
            yield return new WaitForSeconds(0.05f);
            flash++;
        }
        foreach (var led in LEDS) led.sharedMaterial = LEDState[1];
    }

    IEnumerator Solve()
    {
        yield return null;

        solved = true;
        module.HandlePass();

        ////////////////////

        int iterate = 0;

        Audio.PlaySoundAtTransform(sounds[10].name, transform);
        yield return new WaitForSeconds(0.95f);
        while (iterate < UnityEngine.Random.Range(15, 25))
        {

            iterate++;
            int rand = UnityEngine.Random.Range(0, 6);
            foreach (var led in LEDS) led.sharedMaterial = LEDState[1];
            Audio.PlaySoundAtTransform(rand == 5 ? sounds[12].name : sounds[11].name, transform);
            yield return new WaitForSeconds(rand == 5 ? sounds[12].length : sounds[11].length);
            foreach (var led in LEDS) led.sharedMaterial = LEDState[0];
            yield return new WaitForSeconds(0.005f);

        }

        Audio.PlaySoundAtTransform(sounds[13].name, transform);
        yield return new WaitForSeconds(sounds[13].length + 0.02f);


        //StartCoroutine(solveFlash());

        screen.text = "";
        idScreen.text = "";
        for (int l = 0; l < shinylights.Length; l++)
        {
            shinylights[l].enabled = false;
        }
    }

    #region explainsolve

    // Explain how to solve current stage

    void verboseAction()
    {
        currentOrder = (Message[stage - 1]).ToString();



        DebugMsg("Current Order: " + currentOrder);

        if (currentOrder == "PCR")
        {
            logVerboseAnswer = ("Answer for \"" + currentOrder + "\" : Press Red");
        }

        else if (currentOrder == "PCG")
        {
            logVerboseAnswer = ("Answer for \"" + currentOrder + "\" : Press Green");
        }

        else if (currentOrder == "PCB")
        {
            logVerboseAnswer = ("Answer for \"" + currentOrder + "\" : Press Blue");
        }

        else if (currentOrder == "SUB")
        {
            logVerboseAnswer = ("Answer for \"" + currentOrder + "\" : Press Outer Center when seconds digits on the timer are equal");
        }

        else if (currentOrder == "MIT")
        {
            int merc = _moduleId + colorpresses + stage;
            int mercmod = Modulo(merc, 10);
            logVerboseAnswer = ("Answer for \"" + currentOrder + "\" : Press Inner Center when the last digit on the timer is " + mercmod);
            //logVerboseAnswer = ("Answer for \"" + currentOrder + "\" : Press Inner Center when the last digit on the timer is " + mercmod + ", " + Modulo(mercmod + 1, 10) + ", " + Modulo(mercmod + 2, 10) + ", " + Modulo(mercmod - 1, 10) + ", or " + Modulo(mercmod - 2, 60) + " [( " + _moduleId + " + " + colorpresses + " ) % 60 = " + mercmod + " ± 2 )");

        }

        else if (currentOrder == "PRN")
        {
            if (new[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31 }.Contains(_moduleId % 20))
            {
                logVerboseAnswer = ("Module ID % 20 is a prime number! \n Answer for \"" + currentOrder + "\" : Press Inner Center");
            }
            else
            {
                logVerboseAnswer = ("Module ID % 20 is not a prime number \n Answer for \"" + currentOrder + "\" : Press Outer Center");
            }
        }

        else if (currentOrder == "CHK")
        {
            if (new[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31 }.Contains(_moduleId % 20))
            {
                logVerboseAnswer = ("Module ID % 20 is a prime number! \n Answer for \"" + currentOrder + "\" : Press Outer Center");
            }
            else
            {
                logVerboseAnswer = ("Module ID % 20 is not a prime number \n Answer for \"" + currentOrder + "\" : Press Inner Center");
            }
        }

        else if (currentOrder == "BOB")
        {
            logVerboseAnswer = ("Answer for \"" + currentOrder + "\" : Press Inner Center.");
        }

        else if (currentOrder == "REP" || currentOrder == "EAT")
        {
            if (previousVerboseAnswer == "None")
            {
                logVerboseAnswer = ("No previous action. Press Inner Center.");
            }
            else
            {
                logVerboseAnswer = ("Answer for \"" + currentOrder + "\" : Repeat previous order ( " + previousVerboseAnswer + " )");
            }
        }

        else if (currentOrder == "STR" || currentOrder == "IKE")
        {

            logVerboseAnswer = ("Answer for \"" + currentOrder + "\" : Press " + (strikeCounter % 3 == 0 ? "Red" : strikeCounter % 3 == 1 ? "Green" : "Blue"));

        }
        previousVerboseAnswer = logVerboseAnswer;
        DebugMsg(logVerboseAnswer);
    }


    #endregion


    #region Button Stuff

    private KMSelectable.OnInteractHandler HandlePress(int i)
    {
        return delegate
        {
            if (live)
            {
                if (solved)
                {
                    DebugMsg("Module already solved.");
                    return false;
                }
                if (buttons[i].name != "screen")
                {
                    buttons[i].AddInteractionPunch();

                    if (buttons[i].name == "R" || buttons[i].name == "G" || buttons[i].name == "B")
                    {
                        colorpresses++;
                    }
                    DebugMsg("User pressed " + buttons[i]);
                    checkAnswer(buttons[i]);
                    previousinput = buttons[i];

                    return false;
                }
                else
                {
                    if (!idScreenShow)
                    {
                        idScreenShow = true;
                        idScreen.color = Color.white;
                        idScreen.text = makeRoman(_moduleId);

                    }
                    else if (idScreenShow)
                    {
                        idScreenShow = false;
                        idScreen.color = Color.red;
                    }
                    return false;
                }
            }
            else
            {
                DebugMsg("Module not interactable! (is it in a strike animation or solved?)");
                return false;
            }

        };

    }

    // Everytime a button is pressed, check if it's the correct one

    private void checkAnswer(KMSelectable userinput)
    {
        DebugMsg("Checked answer for input " + userinput.name + " and " + currentOrder + " ( " + logVerboseAnswer + " )");

        int bombSeconds = (int)Bomb.GetTime() % 60;
        int bombMinutes = (int)Bomb.GetTime() / 60;
        var button = userinput.name;

        switch (currentOrder)
        {



            case "PCR":
                {
                    if (button == "R")
                    {

                        ansCorrect();
                    }
                    else
                    {
                        DebugMsg("Pressed " + button + ", R expected");
                        restart();
                    }
                    break;
                }
            case "PCG":
                {
                    if (button == "G")
                    {

                        ansCorrect();
                    }
                    else
                    {
                        DebugMsg("Pressed " + button + ", G expected");
                        restart();
                    }
                    break;
                }
            case "PCB":
                {
                    if (button == "B")
                    {

                        ansCorrect();
                    }
                    else
                    {
                        DebugMsg("Pressed " + button + ", B expected");
                        restart();
                    }
                    break;
                }

            case "SUB":
                {
                    if (button == "Outer")
                    {
                        if (bombSeconds % 11 == 0)
                        {
                            ansCorrect();
                        }
                        else
                        {
                            DebugMsg("Button pressed at " + Bomb.GetFormattedTime() + ", seconds digits don't match eachother.");
                            restart();
                        }
                        break;
                    }

                    else
                    {
                        DebugMsg("Pressed " + button + ", Outer Center expected.");
                        restart();
                    }
                    break;

                }
            case "MIT":
                {

                    if (button == "Center")
                    {
                        int merc = _moduleId + colorpresses + stage;
                        int mercmod = Modulo(merc, 10);
                        if (Modulo(bombSeconds, 10) == mercmod)
                        {
                            ansCorrect();
                        }

                        /*/if (bombSeconds == Modulo(merc, 60) || bombSeconds == Modulo((mercmod + 1), 60) || bombSeconds == Modulo((mercmod + 2), 60) || bombSeconds == Modulo((mercmod - 1), 60) || bombSeconds == Modulo((mercmod - 2), 60))
                        {
                            ansCorrect();
                        }
                        /*/

                        else
                        {
                            DebugMsg("Button pressed at " + Bomb.GetFormattedTime() + ", seconds digits don't match any acceptable value.");
                            restart();
                        }
                        break;
                    }
                    else
                    {
                        DebugMsg("Pressed " + button + ", Inner Center expected.");
                        restart();
                    }
                    break;
                }
            case "CHK":
                {
                    if (button == "Outer")
                    {
                        if (new[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31 }.Contains(_moduleId % 20))
                        {
                            ansCorrect();
                        }
                        else
                        {
                            DebugMsg("Pressed " + button + ", but Prime Number conditions match, Inner Center expected.");
                            restart();
                        }
                        break;
                    }
                    else if (button == "Center")
                    {
                        if (!new[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31 }.Contains(_moduleId % 20))
                        {
                            ansCorrect();
                        }
                        else
                        {
                            DebugMsg("Pressed " + button + ", but Prime Number conditions don't match, Outer Center expected.");
                            restart();
                        }
                        break;
                    }
                    else
                    {
                        if (new[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31 }.Contains(_moduleId % 20))
                        {
                            DebugMsg("Pressed " + button + ", Outer Center expected.");
                        }
                        else
                        {
                            DebugMsg("Pressed " + button + ", Inner Center expected.");

                        }
                        restart();
                    }
                    break;
                }
            case "PRN":
                {
                    if (button == "Center")
                    {
                        if (new[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31 }.Contains(_moduleId % 20))
                        {
                            ansCorrect();
                        }
                        else
                        {
                            DebugMsg("Pressed " + button + ", but Prime Number conditions match, Outer Center expected.");
                            restart();
                        }
                        break;
                    }
                    else if (button == "Outer")
                    {
                        if (!new[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31 }.Contains(_moduleId % 20))
                        {
                            ansCorrect();
                        }
                        else
                        {
                            DebugMsg("Pressed " + button + ", but Prime Number conditions don't match, Inner Center expected.");
                            restart();
                        }
                        break;
                    }
                    else
                    {
                        if (new[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31 }.Contains(_moduleId % 20))
                        {
                            DebugMsg("Pressed " + button + ", Inner Center expected.");
                        }
                        else
                        {
                            DebugMsg("Pressed " + button + ", Outer Center expected.");

                        }
                        restart();
                    }
                    break;
                }
            case "BOB": // Best unicorn in the game 10/10 useful monkaOMEGA
                {
                    if (button == "Center")
                    {
                        if (Bomb.IsIndicatorOn(Indicator.BOB) && Bomb.GetIndicators().ToArray().Length == 1 && Bomb.GetBatteryCount() == 2)
                        {
                            DebugMsg("BOB Unicorn conditions met. Module Solved.");
                            StartCoroutine(Solve());
                            break;
                        }

                        ansCorrect();
                    }
                    else
                    {
                        DebugMsg("Pressed " + button + ", Inner Center expected.");
                        restart();
                    }
                    break;
                }
            case "STR":
                {
                    if (strikeCounter % 3 == 0 && button == "R")
                    {
                        ansCorrect();
                    }
                    else if (strikeCounter % 3 == 1 && button == "G")
                    {
                        ansCorrect();
                    }
                    else if (strikeCounter % 3 == 2 && button == "B")
                    {
                        ansCorrect();
                    }
                    else
                    {
                        string[] rgb = { "R", "G", "B" };
                        DebugMsg("Pressed " + button + ", " + rgb[strikeCounter % 3] + " expected");
                        restart();
                    }
                    break;
                }
            case "IKE":
                {
                    if (strikeCounter % 3 == 0 && button == "R")
                    {
                        ansCorrect();
                    }
                    else if (strikeCounter % 3 == 1 && button == "G")
                    {
                        ansCorrect();
                    }
                    else if (strikeCounter % 3 == 2 && button == "B")
                    {
                        ansCorrect();
                    }
                    else
                    {
                        string[] rgb = { "R", "G", "B" };
                        DebugMsg("Pressed " + button + ", " + rgb[strikeCounter % 3] + " expected");
                        restart();
                    }
                    break;
                }
            case "REP":
                {
                    if (previousOrder == "None")
                    {
                        if (button == "Center")
                        {
                            ansCorrect();
                        }
                        else
                        {
                            DebugMsg("Pressed " + button + ", Inner Center expected.");
                            restart();
                        }
                    }
                    else
                    {
                        if (previousinput.name == button)
                        {
                            ansCorrect();
                        }
                        else
                        {
                            DebugMsg("Pressed " + button + ", " + previousinput.name + " expected.");
                            restart();
                        }
                    }
                    break;
                }
            case "EAT":
                {
                    if (previousOrder == "None")
                    {
                        if (button == "Center")
                        {
                            ansCorrect();
                        }
                        else
                        {
                            DebugMsg("Pressed " + button + ", Inner Center expected.");
                            restart();
                        }
                    }
                    else
                    {
                        if (previousinput.name == button)
                        {
                            ansCorrect();
                        }
                        else
                        {
                            DebugMsg("Pressed " + button + ", " + previousinput.name + " expected.");
                            restart();
                        }
                    }
                    break;
                }

            default:
                DebugMsg("Dafuq? Default?");
                restart();
                break;

        }
    }


    // If the answer is correct

    private void ansCorrect()
    {

        switch (stage)
        {

            case 1:
                {
                    LEDS[0].sharedMaterial = LEDState[1];
                    break;
                }
            case 2:
                {
                    LEDS[1].sharedMaterial = LEDState[1];
                    break;
                }
            case 3:
                {
                    LEDS[2].sharedMaterial = LEDState[1];
                    break;
                }
            case 4:
                {
                    LEDS[3].sharedMaterial = LEDState[1];
                    break;
                }
            case 5:
                {
                    LEDS[4].sharedMaterial = LEDState[1];
                    break;
                }
            case 6:
                {
                    LEDS[5].sharedMaterial = LEDState[1];
                    break;
                }
            case 8:
                {
                    LEDS[6].sharedMaterial = LEDState[1];
                    break;
                }
            case 9:
                {
                    LEDS[7].sharedMaterial = LEDState[1];
                    break;
                }
            case 10:
                {
                    LEDS[8].sharedMaterial = LEDState[1];
                    break;
                }
            case 11:
                {
                    LEDS[9].sharedMaterial = LEDState[1];
                    break;
                }

            default:
                break;
        }

        if (stage < 4)
        {
            Audio.PlaySoundAtTransform(sounds[UnityEngine.Random.Range(6, 10)].name, transform);
            DebugMsg("Correct. Next Stage.");
            stage++;
            previousOrder = currentOrder;
            verboseAction();

        }
        else
        {
            StartCoroutine(Solve());
        }

    }

    #endregion


    int Modulo(int number, int mod)
    {
        int result = number % mod;
        while (result < 0)
        {
            result = result + mod;
        }
        return result;
    }

    #region Keys

    private void generateKeys()
    {
        // Key A
        // Starting with the bomb’s Serial Number. Transform each letter into its numerical equivalent (A=1, B=2…)

        DebugMsg("== KEY A ==");

        string serial = Bomb.GetSerialNumber();
        string newserial;

        if (Convert.ToInt32(alphaToInt(Bomb.GetSerialNumber().Substring(0, 1))) >= 20)
        {
            string serialnofirst = serial.Remove(0, 1);
            DebugMsg("The first letter of the serial has been neglected (" + Bomb.GetSerialNumber().Substring(0, 1) + " = " + Convert.ToInt32(alphaToInt(Bomb.GetSerialNumber().Substring(0, 1))) + ")");
            newserial = alphaToInt(serialnofirst);
        }
        else
        {
            newserial = alphaToInt(serial);
        }

        DebugMsg("Serial after number conversion: " + newserial);



        // Divide this number by a factor of 10 if vowel in 4th or 5th characters

        int newserialcalc = Convert.ToInt32(newserial);

        if (Bomb.GetSerialNumber().ToCharArray().ElementAt<char>(3).ToString().Any("AEIOU".Contains) || Bomb.GetSerialNumber().ToCharArray().ElementAt<char>(4).ToString().Any("AEIOU".Contains))
        {
            DebugMsg("Either the 4th or the 5th letter of the serial is a vowel!");
            DebugMsg("Dividing by 10");
            newserialcalc = (newserialcalc / 10);
        }



        // Convert to Hexadecimal

        string newserial16 = Convert.ToString(newserialcalc, 16).ToUpper();

        //// Better logging HEX

        int quotient = newserialcalc;
        int remainder = newserialcalc % 16;
        int i = 0;
        StringBuilder hexer = new StringBuilder();
        DebugMsg("CONVERTING TO HEX – iteration #1: " + quotient + " / 16 = " + quotient / 16 + " (remainder " + remainder + ")");
        string convertedremainder;

        if (quotient != 0)
        {
            hexer.Append(replaceRemainder(remainder) + " ");
        }

        while (quotient != 0)
        {
            i++;
            quotient = quotient / 16;
            remainder = quotient % 16;
            DebugMsg("CONVERTING TO HEX – iteration #" + (i + 1) + ": " + quotient + " / 16 = " + quotient / 16 + " (remainder " + remainder + ")");

            if (quotient != 0)
            {
                hexer.Append(replaceRemainder(remainder) + " ");
            }

            //DebugMsg("CONVERTING TO HEX – HEXER CURRENT = "+ hexer);


        }

        DebugMsg("CONVERTING TO HEX – Quotient Zero! – END");


        convertedremainder = Reverse(hexer.ToString());


        DebugMsg("Final Hex number: " + convertedremainder.Replace(" ", "").Trim());

        if (newserial16 != convertedremainder.Replace(" ", ""))
        {
            DebugMsg("WARNING! THE STEP-BY-STEP CONVERSION DIFFERS FROM THE AUTOMATIC CONVERSION! NOTIFY THE AUTHOR @Maca6774 ON Discord, please attach a link to the bomb logfile!");
            DebugMsg(newserialcalc + " in HEX is " + newserial16);
        }

        //Convert HEX numeric to Alphabet



        /*/string keyA16 = newserial16.Replace("26", "Z").Replace("25", "Y").Replace("24", "X").Replace("23", "W").Replace("22", "V").Replace("21", "U").Replace("20", "T").Replace("19", "S")
        .Replace("18", "R").Replace("17", "Q").Replace("16", "P").Replace("15", "O").Replace("14", "N").Replace("13", "M").Replace("12", "L").Replace("11", "K")
        .Replace("10", "I").Replace("9", "I").Replace("8", "H").Replace("7", "G").Replace("6", "F").Replace("5", "E").Replace("4", "D")
        .Replace("3", "C").Replace("2", "B").Replace("1", "A").Replace("0", "");
        /*/

        string keyA16 = Regex.Replace(newserial16, @"(2[0-6]|1[0-9]|[1-9])", m => ((char)(int.Parse(m.Groups[1].Value) + 'A' - 1)).ToString()).Replace("0", "");


        string keyAMID = Regex.Replace((Modulo((_moduleId - 1), 26) + 1).ToString(), @"(2[0-6]|1[0-9]|[1-9])", m => ((char)(int.Parse(m.Groups[1].Value) + 'A' - 1)).ToString()).Replace("0", "");


        string keyAPP = Regex.Replace((Modulo((portPlates - 1), 26) + 1).ToString(), @"(2[0-6]|1[0-9]|[1-9])", m => ((char)(int.Parse(m.Groups[1].Value) + 'A' - 1)).ToString()).Replace("0", "");


        string keyABH = Regex.Replace((Modulo((batHolders - 1), 26) + 1).ToString(), @"(2[0-6]|1[0-9]|[1-9])", m => ((char)(int.Parse(m.Groups[1].Value) + 'A' - 1)).ToString()).Replace("0", "");

        if (_moduleId != 0)
        {
            DebugMsg("Module ID is " + _moduleId + ", which is equal to " + keyAMID + ":\n ((" + _moduleId + "- 1 % 26) + 1 = " + Modulo(_moduleId, 26) + ")");
        }
        else
        {
            keyAMID = "";
            DebugMsg("Module ID is 0, which is a blank space.");
        }

        if (portPlates != 0)
        {
            DebugMsg("There are " + portPlates + " port plates, which equal to " + keyAPP + ":\n ((" + portPlates + " - 1 % 26) + 1 = " + Modulo(portPlates, 26) + ")");
        }
        else
        {
            keyAPP = "";
            DebugMsg("There are no port plates, which equals a blank space.");
        }

        if (batHolders != 0)
        {
            DebugMsg("There are " + batHolders + " battery holders, which equal to " + keyABH + ":\n ((" + batHolders + " - 1 % 26) + 1 = " + Modulo(batHolders, 26) + ")");
        }
        else
        {
            keyABH = "";
            DebugMsg("There are no battery holders, which equals a blank space.");
        }

        keyA = keyA16 + keyAMID + keyAPP + keyABH;

        DebugMsg("Calculated Key A: " + keyA16 + keyAMID + keyAPP + keyABH);

        // Key B
        // Key B is based on the day and month the bomb was started at.

        DebugMsg("== KEY B  ==");
        //Previous DateTime fetch location
        //string monthRefIndex = keyBPrefixes[month - 1];
        string preKeyB;

        string[,] keyBTable = new string[7, 12]
        {

            {"ABDA","FEV","DBHC","BLD","DBIE","AFEF","AFCG","CQH","DEAI","FEAA","EFAB","DECC" },
            {"ABDB","FEW","DBHD","BLE","DBIF","AFEG","AFCH","CQI","DEAA","FEAB","EFAC","DECD" },
            {"ABDC","FEX","DBHE","BLF","DBIG","AFEH","AFCI","CQA","DEAB","FEAC","EFAD","DECE" },
            {"ABDD","FEY","DBHF","BLG","DBIH","AFEI","AFCA","CQB","DEAC","FEAD","EFAE","DECF" },
            {"ABDE","FEZ","DBHG","BLH","DBII","AFEA","AFCB","CQC","DEAD","FEAE","EFAF","DED" },
            {"ABDF","FEBG","DBHH","BLI","DBIA","AFEB","AFCC","CQD","DEAE","FEAF","EFB","DEDA" },
            {"ABDG","FEBH","DBHI","BLA","DBIB","AFEC","AFCD","CQE","DEAF","FET","EFBA","DEDB" },
            

        };

        switch (day)
        {
            case "Monday":
                dayInt = 1;
                break;
            case "Tuesday":
                dayInt = 2;
                break;
            case "Wednesday":
                dayInt = 3;
                break;
            case "Thursday":
                dayInt = 4;
                break;
            case "Friday":
                dayInt = 5;
                break;
            case "Saturday":
                dayInt = 6;
                break;
            case "Sunday":
                dayInt = 7;
                break;

            default:
                dayInt = 1;
                break;
        }



        //DebugMsg("Month Reference Index: " + monthRefIndex + " " + "(" + keyBPrefixMonths[month - 1] + ")");
        DebugMsg("Month: " + month + ", Day of Week: " + day + " (" + dayInt + ")");

        keyB = keyBTable[dayInt - 1, month - 1];

        //keyB = Regex.Replace(preKeyB, @"(2[0-6]|1[0-9]|[1-9])", m => ((char)(int.Parse(m.Groups[1].Value) + 'A' - 1)).ToString()).Replace("0", "");

        DebugMsg("Key B: " + keyB + " ( D "+month+", M "+dayInt+")");


        // Key C
        // Key C is obtained from Playfair Key A using Key B as a Key

        DebugMsg("== KEY C ==");

        try
        {
            DebugMsg("Playfair Enc: " + keyA + ", " + keyB);
            keyC = PlayfairCipher(keyB, keyA);
        }
        catch
        {
            DebugMsg("FAILED ENCRYPTION WTF");
        }
        DebugMsg("Key C: " + keyC);


    }

    string caesarCipher(string input, int offset)
    {
        string final = (new string(input.Select(ch => (char)((ch - 'A' + offset + 26) % 26 + 'A')).ToArray()));
        //DebugMsg("Caesar Cipher: " + input + " shifted " + offset + " letters is " + final);
        return final;
    }

    /*/   private string divideByFactor(int newserialcalc, string serial)
       {
           int factor = 1;
           char serial4 = Bomb.GetSerialNumber().ToCharArray().ElementAt<char>(3);
           char serial5 = Bomb.GetSerialNumber().ToCharArray().ElementAt<char>(4);


           if (serial4.ToString().Any("AEIOU".Contains))
           {
               DebugMsg("4th Letter of the Serial Number is a vowel! (" + serial4 + ")");
               DebugMsg("Dividing by 10");
               factor = 10;
           }
           if (serial5.ToString().Any("AEIOU".Contains))
           {
               DebugMsg("5th Letter of the Serial Number is a vowel (" + serial5 + ")");
               factor = 10;
               if (!serial4.ToString().Any("AEIOU".Contains))
               {
                   DebugMsg("Dividing by 10");
               }

       }

           //newserialcalc = int.Parse(newserialcalc.ToString().Remove(newserialcalc.ToString().Length-1));
           newserialcalc = newserialcalc / factor;

           return newserialcalc.ToString();
       }
   /*/
    private string alphaToInt(string s)
    {
        string s2 = s.Replace("A", "1").Replace("B", "2").Replace("C", "3").Replace("D", "4").Replace("E", "5").Replace("F", "6").Replace("G", "7").Replace("H", "8")
        .Replace("I", "9").Replace("J", "10").Replace("K", "11").Replace("L", "12").Replace("M", "13").Replace("N", "14").Replace("O", "15").Replace("P", "16")
        .Replace("Q", "17").Replace("R", "18").Replace("S", "19").Replace("T", "20").Replace("U", "21").Replace("V", "22").Replace("W", "23")
        .Replace("X", "24").Replace("Y", "25").Replace("Z", "26");

        return s2;
    }

    private static string Reverse(string s)
    {
        char[] chArray = s.ToCharArray();
        Array.Reverse(chArray);
        return new string(chArray);
    }

    private string replaceRemainder(int c)
    {
        string prereplaced = c.ToString();

        string replaced = prereplaced.Replace("20", "14").Replace("19", "13").Replace("18", "12").Replace("17", "11").Replace("16", "10")
        .Replace("15", "F").Replace("14", "E").Replace("13", "D").Replace("12", "C").Replace("11", "B")
        .Replace("10", "A");

        return replaced;
    }

    #endregion

    IEnumerator OrderSlow(string orders)
    {
        yield return null;

        //offset = 0;

        StringBuilder splicedScreenOrders = new StringBuilder();

        yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 2f));

        for (int i = 0; i < orders.Length; i++)
        {
            yield return new WaitForSeconds(0.01f);

            if (orders.ToCharArray()[(i)].Equals('\n'))
            {
                Audio.PlaySoundAtTransform(sounds[4].name, transform);
                splicedScreenOrders.Append(orders.ToCharArray()[i]);
                screen.text = splicedScreenOrders.ToString();
                yield return new WaitForSeconds(sounds[4].length);
            }
            else
            {
                Audio.PlaySoundAtTransform(sounds[UnityEngine.Random.Range(1, 4)].name, transform);
                splicedScreenOrders.Append(orders.ToCharArray()[i]);
                screen.text = splicedScreenOrders.ToString();
                //yield return new WaitForSeconds(0.125f);
            }

            yield return new WaitForSeconds(0.2f);
        }

        Audio.PlaySoundAtTransform(sounds[5].name, transform);

        yield return false;
    }

    private void rollActions()
    {
        /*/StringBuilder Message = new StringBuilder();
        for (int i = 0; i < 10; i++)
        {
            Message.Append(orders[(UnityEngine.Random.Range(0, orders.Length))]);
        }
        /*/

        for (int i = 0; i < 4; i++) // Get 4 random orders and make an array, so orders are split per stage
        {
            Message[i] = orders[(UnityEngine.Random.Range(0, orders.Length))];
            //DebugMsg("MESSAGE #" + (i + 1) + ": " + Message[i]);
        }

        StringBuilder MessageString = new StringBuilder();
        for (int i = 0; i < Message.Length; i++) // Parse the array and make a string so the Playfair Cipher can work
        {
            MessageString.Append(Message[i]);
        }

        message = MessageString.ToString();
        //DebugMsg("MESSAGE: " + message); (Used as debug)






        string ordersEncryptA = PlayfairCipher(keyA, message);
        string ordersEncryptC = PlayfairCipher(keyC, ordersEncryptA);

        offset = 0;

        DebugMsg(" == OFFSET CALCULATION (CAESAR CIPHER) == ");

        foreach (string port in Bomb.GetPorts().Distinct()) //Offset 2 letter left for every non duplicate port, not once per port.
        {

            offset -= 2;
            DebugMsg("Port " + port + " is unique. Offset -2 (" + offset + ")");

        }

        //offset -= 2 * (Bomb.GetPorts().Distinct().Count());

        // Port Plates

        for (int i = 0; i < Bomb.GetPortPlateCount(); i++)
        {
            offset += 1;
        }

        DebugMsg("Offset increased by " + Bomb.GetPortPlateCount() + " (Port Plates) " + " (" + offset + ")");

        // Serial

        foreach (char c in Bomb.GetSerialNumberLetters())
        {
            if (!c.ToString().Any("AEIOU".Contains))
            {
                offset += 1;
                DebugMsg(c + " is not a vowel. Offset +1!" + " (" + offset + ")");
            }
            else
            {
                offset -= 2;
                DebugMsg(c + " is a vowel! Offset -2!" + " (" + offset + ")");
            }
        }

        /*/foreach (int c in Bomb.GetSerialNumberNumbers())
        {
            offset += c;
            DebugMsg("Offset increased by " + c + "(Serial)");
        }
        /*/

        // Indicators

        foreach (string ind in Bomb.GetOnIndicators())
        {
            offset += 2;
            DebugMsg("Indicator " + ind + " is on, Offset +2!" + " (" + offset + ")");
        }

        foreach (string ind in Bomb.GetOffIndicators())
        {
            offset -= 2;
            DebugMsg("Indicator " + ind + " is off, Offset -2!" + " (" + offset + ")");
        }

        // Batteries

        for (int i = 0; i < Bomb.GetBatteryCount(); i++)
        {
            offset -= 1;
        }
        DebugMsg("Offset decreased by " + Bomb.GetBatteryCount() + "! (Batteries)" + " (" + offset + ")");

        ////

        if (Bomb.GetBatteryCount() == 0)
        {
            offset += 10;
            DebugMsg("Bomb has no batteries! Offset +10!" + " (" + offset + ")");
        }

        /*/if (Bomb.GetIndicators().Count == 0)
        {
            offset -= 10;
            DebugMsg("Bomb has no indicators! Offset +10!" + " (" + offset + ")");
        }
        /*/

        if (Bomb.GetPortCount() == 0)
        {
            offset *= 2;
            DebugMsg("Bomb has no ports! Offset doubled!" + " (" + offset + ")");
        }

        if (Bomb.GetModuleNames().Count > 30)
        {
            offset /= 2;
            DebugMsg("Bomb has more than 30 modules! Offset halved!" + " (" + offset + ")"); // Fixed this stupid mistake kek
        }

        DebugMsg("Final Offset for Caesar Ciphering is: " + offset);



        string caesarEncryptC = caesarCipher(ordersEncryptC, offset);



        DebugMsg("== ORDERS AND ENCRYPTIONS ==");

        DebugMsg("After Caesar Cipher (Offset " + offset + "): " + caesarEncryptC);

        DebugMsg("Orders string last encryption (Key C): " + ordersEncryptC);

        DebugMsg("Orders string first encryption (Key A): " + ordersEncryptA);

        DebugMsg("Original orders string: " + message);

        //DebugMsg("Orders string second encryption (Key B): " + ordersEncryptB);


        //string splicedOrders = caesarEncryptC.Insert(3 * 5, "\n");

        StartCoroutine(OrderSlow(caesarEncryptC));

    }

    #region Playfair Cipher

    void logMatrix(string key)
    {
        List<char> matrixout = new List<char>();
        var alphabet = "ABCDEFGHIIKLMNOPQRSTUVWXYZ"; //missing 'J' on purpose

        for (int i = 0; i < key.Length; i++)
        {
            if (!matrixout.Contains(key[i]))
            {
                matrixout.Add(key[i]);
            }
        }
        for (int i = 0; i < alphabet.Length; i++)
        {
            if (!matrixout.Contains(alphabet[i]))
            {
                matrixout.Add(alphabet[i]);
            }
        }
        var output = "";
        for (int i = 0; i < matrixout.Count; i++)
        {
            output += matrixout[i];
            if (i % 5 == 4)
            {
                output += "\n";
            }
        }
        DebugMsg("Output Matrix:\n" + output);
    }

    //Playfair

    //Declare struct CELL

    public struct Cell
    {
        public char character;
        public int X;
        public int Y;

        public Cell(char _character, int _X, int _Y)
        {
            this.character = _character;
            this.X = _X;
            this.Y = _Y;
        }
    }
    public string PlayfairCipher(string keyWord, string plainText)
    {
        //Debug.LogFormat("[Playfair #{2}] PlayfairCipher started – Encrypting text {1} with key {0}", keyWord, plainText, _moduleId);
        //Define alphabet
        //There is no J in the alphabet, I is used instead!
        char[] alphabet = "ABCDEFGHIKLMNOPQRSTUVWXYZ".ToCharArray();

        //region Adjust Key
        keyWord = keyWord.Trim();
        keyWord = keyWord.ToUpper();
        keyWord = keyWord.Replace(" ", "");
        keyWord = keyWord.Replace("J", "I");
        plainText = plainText.Trim();
        plainText = plainText.ToUpper();
        plainText = plainText.Replace(" ", "");
        plainText = plainText.Replace("J", "I");

        StringBuilder keyString = new StringBuilder();

        foreach (char c in keyWord)
        {
            if (!keyString.ToString().Contains(c))
            {
                keyString.Append(c);
                alphabet = alphabet.Where(val => val != c).ToArray();
            }
        }
        //endregion

        adjustText(plainText);

        //If the Length of the plain text is odd add X
        if ((plainText.Length % 2 > 0))
        {
            plainText += "X";
        }

        List<string> plainTextEdited = new List<string>();

        //Split plain text into pairs
        for (int i = 0; i < plainText.Length; i += 2)
        {
            //If a pair of chars contains the same letters replace one of them with X
            if (plainText[i].ToString() == plainText[i + 1].ToString())
            {
                plainTextEdited.Add(plainText[i].ToString() + 'X');
            }
            else
            {
                plainTextEdited.Add(plainText[i].ToString() + plainText[i + 1].ToString());
            }
        }
        //endregion



        //region Create 5 x 5 matrix
        List<Cell> matrix = new List<Cell>();

        int keyIDCounter = 0;
        int alphabetIDCounter = 0;

        //Fill the matrix. First with the key characters then with the alphabet
        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                if (keyIDCounter < keyString.Length)
                {

                    Cell cell = new Cell(keyString[keyIDCounter], x, y);
                    matrix.Add(cell);
                    keyIDCounter++;
                }
                else
                {
                    Cell cell = new Cell(alphabet[alphabetIDCounter], x, y);
                    matrix.Add(cell);
                    alphabetIDCounter++;

                }


            }
        }
        //endregion



        //region Write cipher

        StringBuilder cipher = new StringBuilder();

        foreach (string pair in plainTextEdited)
        {

            int indexA = matrix.FindIndex(c => c.character == pair[0]);
            Cell a = matrix[indexA];

            int indexB = matrix.FindIndex(c => c.character == pair[1]);
            Cell b = matrix[indexB];

            //Write cipher
            if (a.X == b.X)
            {
                cipher.Append(matrix[matrix.FindIndex(c => c.Y == (a.Y + 1) % 5 && c.X == a.X)].character);
                cipher.Append(matrix[matrix.FindIndex(c => c.Y == (b.Y + 1) % 5 && c.X == b.X)].character);
            }
            else if (a.Y == b.Y)
            {
                cipher.Append(matrix[matrix.FindIndex(c => c.Y == a.Y && c.X == (a.X + 1) % 5)].character);
                cipher.Append(matrix[matrix.FindIndex(c => c.Y == b.Y % 5 && c.X == (b.X + 1) % 5)].character);
            }
            else
            {
                cipher.Append(matrix[matrix.FindIndex(c => c.X == a.X && c.Y == b.Y)].character);
                cipher.Append(matrix[matrix.FindIndex(c => c.X == b.X % 5 && c.Y == a.Y)].character);
            }


        }
        //endregion
        //Debug.LogFormat ("[Playfair #{1}] – {0}", cipher.ToString(), _moduleId);
        return cipher.ToString();
    }


    //Make key Array
    private void baseKeyArray(string baseKey)
    {
        string baseKeyArray = baseKey;

        char[] baseCharArray = baseKeyArray.ToCharArray();

        foreach (var baseKeyArrayChar in baseCharArray)
        {
            //Debug.LogFormat ("[Playfair #{0}] Base Key Array: {1}", _moduleId, baseKeyArrayChar);
        }


    }

    //Remove Spaces, replace "J" with "I" and make UPPERCASE
    private static string adjustText(string text)
    {
        text = text.Trim();
        text = text.Replace(" ", "");
        text = text.Replace("J", "I");
        text = text.ToUpper();

        return text;
    }

    //If Text to Encrypt length is odd add "X"
    protected void checkOdd(string text)
    {
        //bool wasOdd = false;
        if ((text.Length % 2 > 0))
        {
            text += "X";
            //wasOdd = true;
        }

        //Debug.LogFormat("[Playfair #{0}] Was the Text Odd?: {1}", _moduleId, wasOdd);

        getPairs(text);
    }

    //Split text into PAIRS
    private void getPairs(string textToPairs)
    {
        List<string> textEdit = new List<string>();
        for (int i = 0; i < textToPairs.Length; i += 2)
        {
            if (textToPairs[i].ToString() == textToPairs[i + 1].ToString())
            {
                textEdit.Add(textToPairs[i].ToString() + 'X');
            }
            else
            {
                textEdit.Add(textToPairs[i].ToString() + textToPairs[i + 1].ToString());
            }
        }

    }

    #endregion

    void DebugMsg(string message)
    {
        //Debug.LogFormat("[Unfair Cipher #{0}]: Stage {2}, {3} Strikes - {1}", _moduleId, message, stage, strikeCounter);
        Debug.LogFormat("[Unfair Cipher #{0}] {1}", _moduleId, message);
    }

    private void Update()
    {
        if (!TimeModeActive)
        {
            strikeCounter = Bomb.GetStrikes();
        }
        else
        {
            if (strikeCounter != lastStrikeCount && !solved)
            {
                lastStrikeCount = strikeCounter;

                DebugMsg("Strike detected!");
            }
        }
        if (!idScreenShow)
        {
            idScreen.text = makeRoman(strikeCounter).ToString();
        }
    }

}
