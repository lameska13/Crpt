namespace CodeBuilderLibrary
{
    public class CodeBuilder
    {
        /// <summary>
        /// We glue a string of the required identifiers. A list of codes and the desired string of identifiers are submitted to the input.
        /// </summary>
        /// <param name="input">List of available codes</param>
        /// <param name="str">Exapme: "01 + 21 + 240" or "01 + 21 + 8005 + 240"</param>
        public string GeneratePossibleCodes(IEnumerable<string> input, string str)
        {
            if(input is null || string.IsNullOrEmpty(str))
                throw new ArgumentNullException();

            var result = string.Empty;
            var codes = str.Split('+');

            for (int i = 0; i < codes.Length; i++)
            {
                var checkedCode = FindCodeByPredefinedIdentifier(input, codes[i].Trim());
                if(string.IsNullOrEmpty(checkedCode))
                    return string.Empty;
                result += checkedCode;
            }

            return result;
        }

        /// <summary>
        /// Serche code bu identifier
        /// </summary>
        /// <param name="input">List of codes</param>
        /// <param name="str">Identifier</param>
        /// <returns></returns>
        private string FindCodeByPredefinedIdentifier(IEnumerable<string> input, string str) => input?.Find(x => x.StartsWith(str)) ?? "";
    }
}

