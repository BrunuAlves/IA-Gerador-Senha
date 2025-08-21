using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("=== Gerador de Senhas Fortes (IA simples) ===\n");

        bool passphrase = Ask("Modo (1=aleatória, 2=passphrase) [1]: ") == "2";
        int length = AskInt(passphrase ? "Qtd de PALAVRAS [4]: " : "Tamanho [14]: ", passphrase ? 4 : 14);
        bool useSymbols = !passphrase && AskBool("Incluir símbolos? (s/n) [s]: ", true);
        bool excludeAmb = AskBool("Excluir ambíguos (O,0,l,1,|)? (s/n) [s]: ", true);
        int count = AskInt("Quantas sugestões [5]: ", 5);

        var opts = new PasswordOptions
        {
            Mode = passphrase ? PasswordMode.Passphrase : PasswordMode.Random,
            Length = length,
            UseSymbols = useSymbols,
            ExcludeAmbiguous = excludeAmb
        };

        var sugestões = new List<PasswordOut>();
        for (int i = 0; i < count; i++)
            sugestões.Add(PasswordService.Generate(opts));

        foreach (var (pwd, score) in sugestões.OrderByDescending(s => s.Score.Score))
        {
            Console.WriteLine($"Senha Gerada: {pwd}");
            Console.WriteLine($"Score: {score.Score}/100 | entropia~{score.EntropyBits:F1} bits");
            Console.WriteLine($"Motivos: {string.Join("; ", score.Reasons)}\n");
        }

        Console.WriteLine("Dica: use gerenciador de senhas. Em passphrase, varie separador/ordem/dígitos.");
    }

    static string Ask(string label)
    {
        Console.Write(label);
        return (Console.ReadLine() ?? "").Trim();
    }
    static int AskInt(string label, int def)
    {
        var raw = Ask(label);
        return int.TryParse(raw, out var v) ? v : def;
    }
    static bool AskBool(string label, bool defYes)
    {
        var raw = Ask(label).ToLower();
        if (raw == "s" || raw == "y") return true;
        if (raw == "n") return false;
        return defYes;
    }
}
