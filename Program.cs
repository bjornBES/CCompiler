using CCompiler.tokenizer;
using System.Text.Json;

public class Program
{
    static string InputPath = "";
    Dictionary<string, Func<string[], int>> arguments = new Dictionary<string, Func<string[], int>>()
    {
        { "-i", GetInput }
    };
    static int i;
    private static int Main(string[] args)
    {
        _ = new Program(args);
        return 0;
    }
    public Program(string[] args) 
    {
        for (i = 0; i < args.Length; i++)
        {
            if (arguments.ContainsKey(args[i]))
            {
                arguments[args[i]](args);
            }
            else
            {
                GetInput(args);
            }
        }

        if (string.IsNullOrEmpty(InputPath))
        {
            Console.WriteLine("No input file found");
            Environment.Exit(1);
        }

        string FileContents = File.ReadAllText(InputPath).Replace(Environment.NewLine, "\n");
        Tokenizer tokenizer = new Tokenizer();
        tokenizer.Build(FileContents);
        Token[] tokens = tokenizer.m_tokens.ToArray();
        
        string foramt = "";
        for (int i = 0; i < tokens.Length; i++)
        {
            foramt += tokens[i].ToString() + "\n";
        }
        File.WriteAllText("./tokens.txt", foramt);
        
        /*
        Parser parser = new Parser();
        NodeProg nodeProg = parser.Parse_Prog(tokens);

        Generator generator = new Generator();
        string[] output = generator.Gen_prog(nodeProg);

        File.WriteAllLines("./a.txt", output);
         */
    }

    static int GetInput(string[] argument)
    {
        if (i + 1 < argument.Length)
        {
            i++;
            InputPath = argument[i];
        }
        return 0;
    }
}
