using Gs1DataMatrixParserLibrary;

namespace Gs1DataMatrixParserLibraryTest;

public class Tests
{
    public Dictionary<string, int> PredefinedIdentifier { get; set; } = new();
    public static string[] DivideCases => File.ReadAllLines("test.txt");
    //public TestContext TestContext { get; set; }


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
    public void TestIncomingStrings(string value)
    {
        using (var parser = new Parser(value, PredefinedIdentifier ?? new()))
        {
            CheckResult(value, parser);
        };
    }    


    [TestCaseSource(nameof(DivideCases))]
    public void TestIncomingByteArray(string value)
    {
        using (var parser = new Parser(System.Text.Encoding.ASCII.GetBytes(value), PredefinedIdentifier ?? new()))
        {
            CheckResult(value, parser);
        };
    }


    /// <summary>
    /// Check is code parser
    /// </summary>
    /// <param name="input">incoming string</param>
    /// <param name="parser">inited parsef, after use ReadString() method</param>
    /// <returns></returns>
    private void CheckResult(string value, Parser parser)
    {
        var result = false;
        
        parser.ReadString();        

        if(value.StartsWith("01"))
        {
            if (parser.FindedApplicationCode.Count > 1 
                && parser.FindedApplicationCode.Exists(x => x.StartsWith("01")) 
                && parser.FindedApplicationCode.Exists(x => x.StartsWith("21")))
            {
                result = true;
            }
        }
        else
        {
            // Ќе начинаетс€ с 01 кода применени€, значит это UNIT
            result = parser.FindedApplicationCode.Count > 0;
        }

        Assert.That(result, Is.True);
    }
}