using System.Collections.Generic;

public enum PasswordMode { Random, Passphrase }

public class PasswordOptions
{
    public PasswordMode Mode { get; set; } = PasswordMode.Random;
    public int Length { get; set; } = 14;          // tamanho (ou qtd de palavras no modo passphrase)
    public bool UseSymbols { get; set; } = true;   // só vale para modo Random
    public bool ExcludeAmbiguous { get; set; } = true;
}

public record ScoreResult(double EntropyBits, int Score, List<string> Reasons);
public record PasswordOut(string Password, ScoreResult Score);
