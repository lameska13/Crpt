using System.Collections;

namespace CodeBuilderLibraryTest
{
    public class TestCasesClass : IEnumerable
    {
        public static string[] DivideCases => File.ReadAllLines("test.txt");

        public IEnumerator GetEnumerator()
        {
            foreach (var item in DivideCases)
            {
                yield return XmlToList(item);
            }
        }

        private IEnumerable? XmlToList(string xml)
        {
            return System.Xml.Linq.XDocument.Parse(xml)?.Root?.Elements("Codes").Select(x => x.Value).ToList();
        }
    }
}
