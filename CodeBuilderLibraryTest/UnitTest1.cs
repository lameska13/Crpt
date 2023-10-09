using CodeBuilderLibrary;

namespace CodeBuilderLibraryTest;

public class Tests
{
    public List<string> PossibleSequences { get; set; }

    [SetUp]
    public void Setup()
    {
        PossibleSequences = new() {
            "01 + 21 + 240 + 8005",
            "01 + 21 + 240",
            "01 + 21 + 8005",
            "01 + 21 + 93",
            "01 + 21"
        };
    }


    [Test, TestCaseSource(typeof(TestCasesClass))]
    public void Test1(List<string> list)
    {        
        var codeBuilder = new CodeBuilder();

        foreach (var possibleSequence in PossibleSequences)
        {
            var result = codeBuilder.GeneratePossibleCodes(list, possibleSequence);

            TestContext.WriteLine($"{possibleSequence}:{result}");
            Assert.That(string.IsNullOrEmpty(result), Is.False);
        }
    }
}

