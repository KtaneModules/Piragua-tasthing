using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using rnd = UnityEngine.Random;

public class piragua : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMBombModule module;

    public KMSelectable piraguaButton;
    public KMSelectable syrupButton;
    public Renderer ice;
    public Renderer syrup;
    public Color[] colors;

    private int[] syrupOrder = new int[10];
    private int currentFlavor;
    private int numberA;
    private int numberB;
    private int solution;

    private static readonly string[] flavorNames = new string[10] { "china", "melao", "fresa", "limón", "guanábana", "uva", "anis", "frambuesa", "ajonjolí", "tamarindo" };
    private Color startingColor;
    private int startingTime;
    private int startingDay;
    private int solvedPiraguas;

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    private void Awake()
    {
        moduleId = moduleIdCounter++;
        syrupButton.OnInteract += delegate () { PressSyrup(); return false; };
        piraguaButton.OnInteract += delegate () { PressPiragua(); return false; };
    }

    private void Start()
    {
        startingTime = (int)bomb.GetTime();
        startingDay = (int)DateTime.Now.DayOfWeek;
        startingColor = ice.material.color;
        syrupOrder = Enumerable.Range(0, 10).ToList().Shuffle().ToArray();
        syrup.material.color = colors[syrupOrder[0]];

        Debug.LogFormat("[Piragua #{0}] NUMBER A:", moduleId);
        var aString = bomb.GetBatteryCount().ToString() + bomb.GetPortCount() + bomb.GetIndicators().Count() + bomb.GetModuleNames().Count() + (startingTime / 60);
        var aStringNoDupes = "";
        for (int i = 0; i < aString.Length; i++)
            if (!aStringNoDupes.Contains(aString[i]))
                aStringNoDupes += aString[i];
        Debug.LogFormat("[Piragua #{0}] Concatenated number: {1} (Duplicates removed: {2})", moduleId, aString, aStringNoDupes);
        numberA = int.Parse(aStringNoDupes) * (bomb.GetPortPlateCount() * bomb.GetBatteryHolderCount() + 1);
        numberA %= 10000;
        Debug.LogFormat("[Piragua #{0}] Final value of number A: {1}", moduleId, numberA);

        Debug.LogFormat("[Piragua #{0}] NUMBER B:", moduleId);
        var digit1 = bomb.GetModuleNames().Count(x => x.ToUpperInvariant().Contains("IDENTIFICATION") || x.ToUpperInvariant().Contains("ARROW") || x.ToUpperInvariant().Contains("SIMON"));
        var digit2 = startingDay + 1;
        var group1 = bomb.GetPortCount(Port.Parallel) > 0 || bomb.GetPortCount(Port.Serial) > 0;
        var group2 = bomb.GetPortCount(Port.PS2) > 0 || bomb.GetPortCount(Port.DVI) > 0 || bomb.GetPortCount(Port.StereoRCA) > 0 || bomb.GetPortCount(Port.RJ45) > 0;
        var digit3 = 0;
        if (group1 && group2)
            digit3 = 1;
        else if (group1)
            digit3 = 2;
        else if (group2)
            digit3 = 3;
        else
            digit3 = 4;
        if (bomb.GetSerialNumberNumbers().First() % 2 == 0)
            digit3 += 5;
        var digit4 = bomb.GetSerialNumberNumbers().Last();
        var digits = digit1.ToString() + digit2 + digit3 + digit4;
        Debug.LogFormat("[Piragua #{0}] Four digits: {1}", moduleId, digits);
        var unhingedNumber = long.Parse(digits) * 58829412353L;
        numberB = (int)(unhingedNumber % 10000L);
        Debug.LogFormat("[Piragua #{0}] Final value of number B: {1}", moduleId, numberB);

        if (GCD(numberA, numberB) == 1)
        {
            Debug.LogFormat("[Piragua #{0}] Numbers A and B are coprime.", moduleId);
            solution = ((numberA + numberB - 1) % 9) + 1;
        }
        else
        {
            Debug.LogFormat("[Piragua #{0}] Numbers A and B are not coprime.", moduleId);
            solution = GCD(numberA, numberB) % 10;
        }
        Debug.LogFormat("[Piragua #{0}] The final digit is {1}. The correct flavor is {2}.", moduleId, solution, flavorNames[solution]);
    }

    private void PressSyrup()
    {
        syrupButton.AddInteractionPunch(.25f);
        if (moduleSolved)
            return;
        currentFlavor = (currentFlavor + 1) % 10;
        syrup.material.color = colors[syrupOrder[currentFlavor]];
    }

    private void PressPiragua()
    {
        piraguaButton.AddInteractionPunch(.25f);
        if (moduleSolved)
            return;
        Debug.LogFormat("[Piragua #{0}] You served {1}.", moduleId, flavorNames[syrupOrder[currentFlavor]]);
        if (syrupOrder[currentFlavor] != solution)
        {
            Debug.LogFormat("[Piragua #{0}] That was incorrect. Strike!", moduleId);
            module.HandleStrike();
        }
        else
        {
            Debug.LogFormat("[Piragua #{0}] That was correct. Module solved!", moduleId);
            module.HandlePass();
            moduleSolved = true;
            StartCoroutine(ColorIce(colors[syrupOrder[currentFlavor]]));
            audio.PlaySoundAtTransform("solve", transform);
        }
    }

    private IEnumerator ColorIce(Color endingColor)
    {
        var elapsed = 0f;
        var duration = .75f;
        while (elapsed < duration)
        {
            ice.material.color = Color.Lerp(startingColor, endingColor, elapsed / duration);
            yield return null;
            elapsed += Time.deltaTime;
        }
        ice.material.color = endingColor;
    }

    private static int GCD(int value1, int value2)
    {
        while (value1 != 0 && value2 != 0)
        {
            if (value1 > value2)
                value1 %= value2;
            else
                value2 %= value1;
        }
        return value1 | value2;
    }

    private void Update()
    {
        if (moduleSolved)
            return;
        if (bomb.GetSolvedModuleNames().Count(x => x == "Piragua") != solvedPiraguas)
        {
            Debug.LogFormat("[Piragua #{0}] Solved Piragua detected! Incrementing solution.", moduleId);
            solvedPiraguas++;
            solution = (solution + 1) % 10;
            Debug.LogFormat("[Piragua #{0}] The correct flavor is now {1}.", moduleId, flavorNames[solution]);
        }
    }

    // Twitch Plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} bottle [Presses the syrup bottle. Note that you may need to tilt to see the color of the syrup.] !{0} submit [Presses the piragua cup.]";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string input)
    {
        input = input.ToUpperInvariant().Trim();
        if (input == "BOTTLE")
        {
            yield return null;
            syrupButton.OnInteract();
        }
        else if (input == "SUBMIT")
        {
            yield return null;
            piraguaButton.OnInteract();
        }
        else
            yield break;
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        while (syrupOrder[currentFlavor] != solution)
        {
            syrupButton.OnInteract();
            yield return new WaitForSeconds(.1f);
        }
        piraguaButton.OnInteract();
    }
}
