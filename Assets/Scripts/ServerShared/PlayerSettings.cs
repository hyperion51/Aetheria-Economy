using System;
using System.Collections.Generic;
using System.Globalization;
using MessagePack;

[MessagePackObject]
public class PlayerSettings
{
    [Key(0)] public string Name = "Anonymous";
    [Key(1)] public SavedGame SavedRun;
    [Key(2)] public bool TutorialPassed;
    [Key(3)] public Dictionary<string, string> HashedStoryFiles = new Dictionary<string, string>();
    [Key(4)] public PlayerGameplaySettings GameplaySettings;
    [Key(5)] public PlayerInputSettings InputSettings;

    public string FormatTemperature(float t)
    {
        return GameplaySettings.TemperatureUnit switch
        {
            TemperatureUnit.Kelvin => $"{Format(t)}°K",
            TemperatureUnit.Celsius => $"{Format(t - 273.15f)}°C",
            TemperatureUnit.Fahrenheit => $"{Format((t - 273.15f) * (9f / 5) + 32)}°F",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public float ParseTemperature(string s)
    {
        var t = float.Parse(s);
        return GameplaySettings.TemperatureUnit switch
        {
            TemperatureUnit.Kelvin => t,
            TemperatureUnit.Celsius => t + 273.15f,
            TemperatureUnit.Fahrenheit => (t - 32) * (5f / 9) + 273.15f,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public string Format(float d)
    {
        var magnitude = d == 0.0f ? 0 : (int)Math.Floor(Math.Log10(Math.Abs(d))) + 1;
        var digits = GameplaySettings.SignificantDigits;
        digits -= magnitude;
        if (digits < 0)
            digits = 0;
        var strdec = d.ToString($"N{digits}");
        var dec = Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
        return strdec.Contains(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator) ? strdec.TrimEnd('0').TrimEnd(dec) : strdec;
    }
}

public class PlayerGameplaySettings
{
    [Key(0)] public TemperatureUnit TemperatureUnit = TemperatureUnit.Celsius;
    [Key(1)] public int SignificantDigits = 3;
}

public class PlayerInputSettings
{
    [Key(0)] public Dictionary<string, string> InputActionMap = new Dictionary<string, string>();
    [Key(1)] public bool FiveButtonMouse;
}