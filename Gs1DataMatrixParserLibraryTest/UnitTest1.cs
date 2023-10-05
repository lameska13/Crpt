using System.Diagnostics;
using Gs1DataMatrixParserLibrary;

namespace Gs1DataMatrixParserLibraryTest;

public class Tests
{
    public Dictionary<string, int> PredefinedIdentifier { get; set; } = new();

    [SetUp]
    public void Setup()
    {
        PredefinedIdentifier = new Dictionary<string, int>{
            {"00", 20},
            {"01", 16},
            {"02", 16},
            {"03", 16},
            {"11", 8},
            {"12", 8},
            {"13", 8},
            {"14", 8},
            {"15", 8},
            {"16", 8},
            {"17", 8},
            {"18", 8},
            {"19", 8},
            {"20", 4},
            {"31", 10},
            {"32", 10},
            {"33", 10},
            {"34", 10},
            {"35",  10},
            {"36", 10},
            {"41", 16}
        };
    }

    [TestCase("010460026601479521V:!CE\\\"K\\u001D8005149000\\u001D93lSKY\\u001D24010181884")]
    [TestCase("0104600266014757219.r\\/q6d\\u001D8005165000\\u001D93p3dI\\u001D24010177027")]
    [TestCase("010460026601474021fwAoGji\\u001D8005165000\\u001D93izks\\u001D24010177026")]
    [TestCase("010460026601488721Q?Y-O9.\\u001D8005250000\\u001D93CX2+\\u001D24010177024")]
    [TestCase("010460026601454221n?An-wp\\u001D8005175000\\u001D9388UC\\u001D24010177007")]
    [TestCase("010460026601459721\\/YO?ZS.\\u001D8005205000\\u001D939\\/g6\\u001D24010177004")]
    [TestCase("01146002660147471068272023\\u001D2168243GD103465955500\\u001D24010181973\\u001D914802227")]
    [TestCase("")]
    public void TestStrings(string value)
    {
        if(PredefinedIdentifier == null || PredefinedIdentifier.Count == 0){
            Assert.IsEmpty("PredefinedIdentifier is null or empty");
        }

        using (var parser = new Parser(value, PredefinedIdentifier ?? new()))
        {
            parser.ReadString();

            Debug.WriteLine($"{value} Parsed:");

            foreach(var item in parser.FindedApplicationCode){
                Debug.WriteLine($"[{item}]");
            }

            Assert.IsNotEmpty(parser.ErrorMessage, parser.ErrorMessage);
        };

        Assert.Pass();
    }


    [TestCase()]
    public void TestFromFile()
    {
        if(!File.Exists("test.txt"))
            Assert.IsEmpty("file not exists");

        var rows = File.ReadAllLines("test.txt");
        
        if(rows.Length == 0)
            Assert.IsEmpty("file is empty");

        foreach (var line in rows)
        {
            using (var parser = new Parser(System.Text.Encoding.ASCII.GetBytes(line), PredefinedIdentifier))
            {
                parser.ReadString();

                Debug.WriteLine($"{line.ToString()} Parsed:");

                foreach (var item in parser.FindedApplicationCode)
                {
                    Debug.WriteLine($"[{item}]");
                }

                Assert.IsNotEmpty(parser.ErrorMessage, parser.ErrorMessage);
            };
        }

        Assert.Pass();
    }
}