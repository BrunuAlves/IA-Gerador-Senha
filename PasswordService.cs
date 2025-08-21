using System;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;

public static class PasswordService
{
    private static readonly char[] Lower = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
    private static readonly char[] Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
    private static readonly char[] Digits = "0123456789".ToCharArray();
    private static readonly char[] Symbols = "!@#$%^&*()-_=+[]{};:,.?/".ToCharArray();
    private static readonly char[] Ambiguous = "O0oIl1|".ToCharArray();
    // Lista de palavras para passphrase (pode ser expandida)
    private static readonly string[] WordList =
    {
        "lobo","neve","sol","manga","cobre","nuvem","vento","tigre",
        "rio","brasa","cacto","jade","caju","foco","vapor","areia",
        "ninja","caverna","fusao","trama","onda","dado","polo","drone"
    };

    public static PasswordOut Generate(PasswordOptions o)
    {
        string pwd = o.Mode == PasswordMode.Passphrase
            ? GeneratePassphrase(o)
            : GenerateRandom(o);

        var score = ScorePassword(pwd, o.Mode == PasswordMode.Passphrase);
        return new PasswordOut(pwd, score);
    }

    private static string GenerateRandom(PasswordOptions o)
    {
        int length = Math.Clamp(o.Length, 8, 128);

        var pool = new List<char>(Lower.Length + Upper.Length + Digits.Length + (o.UseSymbols ? Symbols.Length : 0));
        pool.AddRange(Lower);
        pool.AddRange(Upper);
        pool.AddRange(Digits);
        if (o.UseSymbols) pool.AddRange(Symbols);

        if (o.ExcludeAmbiguous) pool = pool.Where(c => !Ambiguous.Contains(c)).ToList();

        // Garante pelo menos um de cada classe essencial
        var must = new List<char>
        {
            Pick(Lower, o), Pick(Upper, o), Pick(Digits, o)
        };
        if (o.UseSymbols) must.Add(Pick(Symbols, o));

        var sb = new StringBuilder();
        foreach (var c in must) sb.Append(c);
        while (sb.Length < length) sb.Append(Pick(pool));

        return Shuffle(sb.ToString());
    }

    private static string GeneratePassphrase(PasswordOptions o)
    {
        int words = Math.Clamp(o.Length, 3, 12);
        var picked = new List<string>(words);
        for (int i = 0; i < words; i++)
        {
            var w = WordList[Next(0, WordList.Length)];
            if (o.ExcludeAmbiguous)
                w = new string(w.Where(c => !"O0oIl1|".Contains(c)).ToArray());
            if (string.IsNullOrWhiteSpace(w)) { i--; continue; }
            picked.Add(w);
        }

        // mutações leves: capitalizar uma palavra e adicionar 2 dígitos
        if (Next(0, 2) == 1)
        {
            int idx = Next(0, picked.Count);
            picked[idx] = char.ToUpper(picked[idx][0]) + picked[idx][1..];
        }
        string digits = Next(10, 99).ToString();

        return string.Join("-", picked) + digits;
    }

    private static ScoreResult ScorePassword(string pwd, bool isPassphrase)
    {
        var reasons = new List<string>();
        int score = 0;

        bool lower = pwd.Any(char.IsLower);
        bool upper = pwd.Any(char.IsUpper);
        bool digit = pwd.Any(char.IsDigit);
        bool symbol = pwd.Any(ch => Symbols.Contains(ch));

        int classes = new[] { lower, upper, digit, symbol }.Count(b => b);
        score += classes * 10;
        reasons.Add($"+{classes * 10} diversidade ({classes} classes)");

        int len = pwd.Length;
        if (len >= 12) { score += 25; reasons.Add("+25 comprimento ≥12"); }
        else if (len >= 10) { score += 15; reasons.Add("+15 comprimento ≥10"); }
        else if (len >= 8) { score += 8; reasons.Add("+8 comprimento ≥8"); }

        if (symbol) { score += 10; reasons.Add("+10 símbolos"); }

        // padrões comuns
        string lowerPwd = pwd.ToLowerInvariant();
        string[] bad = { "1234", "senha", "password", "qwerty" };
        if (bad.Any(lowerPwd.Contains)) { score -= 25; reasons.Add("-25 padrão comum"); }

        // repetições (AAA)
        if (HasRepeats(pwd)) { score -= 10; reasons.Add("-10 repetições"); }

        // entropia aproximada
        double charset = (lower ? 26 : 0) + (upper ? 26 : 0) + (digit ? 10 : 0) + (symbol ? Symbols.Length : 0);
        if (charset == 0) charset = 26;
        double entropy = len * Math.Log(charset, 2);

        if (isPassphrase && len >= 16) { score += 10; reasons.Add("+10 passphrase longa"); }

        score = Math.Clamp(score, 0, 100);
        return new ScoreResult(entropy, score, reasons);
    }
    private static int Next(int minInclusive, int maxExclusive)
        => RandomNumberGenerator.GetInt32(minInclusive, maxExclusive);

    private static T Pick<T>(IList<T> list) => list[Next(0, list.Count)];

    private static char Pick(char[] arr, PasswordOptions o)
    {
        var list = arr.AsEnumerable();
        if (o.ExcludeAmbiguous) list = list.Where(c => !"O0oIl1|".Contains(c));
        var final = list.ToArray();
        return final.Length == 0 ? arr[0] : Pick(final);
    }

    private static string Shuffle(string s)
    {
        var chars = s.ToCharArray();
        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = Next(0, i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars);
    }

    private static bool HasRepeats(string s)
    {
        int run = 1;
        for (int i = 1; i < s.Length; i++)
        {
            run = (s[i] == s[i - 1]) ? run + 1 : 1;
            if (run >= 3) return true;
        }
        return false;
    }
}
