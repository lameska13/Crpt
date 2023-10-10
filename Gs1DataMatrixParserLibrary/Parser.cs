using System.Text;

namespace Gs1DataMatrixParserLibrary
{
    public class Parser : IDisposable
    {
        private bool _disposed = false;
        private readonly Byte _gs1 = 29; // Predefined GS1 symbol
        public int Cursor { get; set; } = 0;
        public Byte[] Buffer { get; set; } = Array.Empty<byte>();
        public string? ErrorMessage { get; set; } = null;
        public List<string> FindedApplicationCode { get; set; } = new(); // Найденые коды применения со значениями

        #region Incoming byte array
        private Byte[] _incomingString = Array.Empty<byte>(); // Массив со входящей строкой
        public Byte[] IncomingString
        {
            get => _incomingString;
            set => _incomingString = value ?? throw new ArgumentNullException(nameof(IncomingString), $"Cannot be null");
        }
        #endregion

        #region Incoming predefined identifier
        private Dictionary<string, int> _predefinedIdentifier = new();
        public Dictionary<string, int> PredefinedIdentifier
        {
            get => _predefinedIdentifier;
            set => _predefinedIdentifier = value ?? throw new ArgumentNullException(nameof(PredefinedIdentifier), $"Cannot be null");
        }
        #endregion

        public Parser()
        {
            PredefinedIdentifier = InitPredefinedIdentifier();
        }

        public Parser(Byte[] incomintStrAsByte, Dictionary<string, int>? predefinedIdentifier = null)
        {
            PredefinedIdentifier = predefinedIdentifier ?? InitPredefinedIdentifier();
            IncomingString = incomintStrAsByte;
        }

        public Parser(string incomintStr, Dictionary<string, int>? predefinedIdentifier = null)
        {
            var rx = new System.Text.RegularExpressions.Regex(@"\\[uU]([0-9A-F]{4})");
            incomintStr = rx.Replace(incomintStr, match => ((char)int.Parse(match.Value[2..], System.Globalization.NumberStyles.HexNumber)).ToString());

            PredefinedIdentifier = predefinedIdentifier ?? InitPredefinedIdentifier();
            IncomingString = Encoding.ASCII.GetBytes(incomintStr);
        }


        /// <summary>
        /// Если не передали список предопределенных кодов применений
        /// </summary>
        /// <returns>Dictionary<string, int></returns>
        private Dictionary<string, int> InitPredefinedIdentifier()
        {
            return new Dictionary<string, int>{
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


        /// <summary>
        /// Работ строки, алгоритм описан в документации GS1 раздел 7.8(от 18.05.2023)
        /// </summary>
        public void ReadString()
        {
            if (!IsAnyDataPresent()) // (Спецификация 7.8, на схеме пункт: 6.)
            {
                HandleErrors("The Data is not present");
                return;
            }

            if (!DoesStringContainsGsOne()) // Нет символа FNC(не подходит под алгоритм GS1, по этому считаем кодом пачки и используем целиком
            {
                MoveRemainingDataStringToBuffer();

                TransmitDataInBufferForFutherProcessing();

                return;
            }

        LOOP:
            if (AreFirstTwoDigitsInFigure()) // (Спецификация 7.8, на схеме пункт: 7.)
            {
                if (!DoesStringContainAtLeastTheCorrectNumberOfNumberCharacters(out int len, out bool haveBraces)) // (Спецификация 7.8, на схеме пункт: 9.)
                {
                    HandleErrors("The length of the string is less than the requested");
                    return;
                }

                MovePredefinedNumberOfCharactersToBuffer(len, haveBraces);

                if (DoesBufferContainGsOne()) // (Спецификация 7.8, на схеме пункт: 10.)
                {
                    HandleErrors("The buffer cannot contain FNC symbol");
                    return;
                }

                TransmitDataInBufferForFutherProcessing();

                IfThisCharacterIsGsOneMovePastIt();

                if (!IsAnyDataPresent()) // (Спецификация 7.8, на схеме пункт: 11.)
                    return;

                // move to NEXT
                goto LOOP;
            }
            else
            {
                if (DoesStringContainsGsOne()) // (Спецификация 7.8, на схеме пункт: 8.)
                {
                    MoveCharactersUpToGsOneToBuffer();

                    TransmitDataInBufferForFutherProcessing();

                    MovePastGsOne();

                    if (!IsAnyDataPresent()) // (Спецификация 7.8, на схеме пункт: 12.)
                        return;

                    // move to NEXT
                    goto LOOP;
                }
                else
                {
                    MoveRemainingDataStringToBuffer();

                    TransmitDataInBufferForFutherProcessing();
                }
            }
        }


        /// <summary>
        /// Обработка ошибок.
        /// </summary>
        private void HandleErrors(string? message = null)
        {
            if (!string.IsNullOrEmpty(message))
                ErrorMessage = message;
        }


        /// <summary>
        /// Скопировать опредененное количество символов в буфер с текущей позиции.
        /// </summary>
        /// <param name="length">Заданная длина</param>
        private void MovePredefinedNumberOfCharactersToBuffer(int length, bool haveBraces)
        {
            if (length > 0)
            {
                int startBraces = 0, endBraces = 0, len = Cursor + length;

                if (haveBraces)
                {
                    startBraces = Cursor;
                    endBraces = Cursor + 3;
                }

                for (; Cursor < len; Cursor++)
                {
                    if (haveBraces && (Cursor == startBraces || Cursor == endBraces))
                        continue;

                    Buffer = AddByteToArray(Buffer, IncomingString[Cursor]);
                }
            }
        }


        /// <summary>
        /// Переносим данные из буфер в лист, для дальнейшей обработки.
        /// </summary>
        private void TransmitDataInBufferForFutherProcessing()
        {
            try
            {
                FindedApplicationCode?.Add(Encoding.ASCII.GetString(Buffer));
            }
            catch (ArgumentException ex)
            {
                HandleErrors(ex.Message);
            }
            finally
            {
                Buffer = Array.Empty<byte>();
            }
        }


        /// <summary>
        /// Скопировать оставшуюся часть строки в буфер
        /// </summary>
        private void MoveRemainingDataStringToBuffer()
        {
            if ((IncomingString.Length - Cursor) > 0)
            {
                for (; Cursor < IncomingString.Length; Cursor++)
                {
                    Buffer = AddByteToArray(Buffer, IncomingString[Cursor]);
                }
            }
        }


        /// <summary>
        /// Копируем символы в буфер пока не упремся в символ GS1.
        /// </summary>
        private void MoveCharactersUpToGsOneToBuffer()
        {
            if ((IncomingString.Length - Cursor) > 0)
            {
                for (; Cursor < IncomingString.Length; Cursor++)
                {
                    // Если следующий символ GS1, то заканчиваем обработку. В стандарте GS1 описано, что все не описанные коды применения заканчиваются символом GS1
                    if (IncomingString[Cursor] == _gs1)
                    {
                        Cursor++;
                        break;
                    }
                    Buffer = AddByteToArray(Buffer, IncomingString[Cursor]);
                }
            }
        }


        /// <summary>
        /// Проверим код применения(первые 2 символа) с позиции курсора, содержится ли они в таблице предопределенных символов.
        /// Таблица содержит коды применения фиксированной длинны, без спец фимвола FNC
        /// Коды применения не содержащиеся в данной таблице должны всегда оканчиваться символом FNC
        /// </summary>
        /// <returns>Код применения определен: Да/Нет</returns>
        private bool AreFirstTwoDigitsInFigure()
        {
            var twoDigit = string.Empty;

            try
            {
                twoDigit = (Encoding.Default.GetString(IncomingString[Cursor..(Cursor + 1)]) == "(") 
                    && Encoding.Default.GetString(IncomingString[(Cursor + 3)..(Cursor + 4)]) == ")"
                    ? Encoding.Default.GetString(IncomingString[(Cursor + 1)..(Cursor + 3)])
                    : Encoding.Default.GetString(IncomingString[Cursor..(Cursor + 2)]);
            }
            catch (Exception ex)
            {
                HandleErrors(ex.Message);
            }

            return PredefinedIdentifier.ContainsKey(twoDigit);
        }


        /// <summary>
        /// По первым трем символам проверим, что это именно GS1 код
        /// НЕ ИСПОЛЬЗУЕТСЯ(МЫ ЗАРАНЕЕ ПЕРЕДАЕМ НУЖНЫЙ КОДЫ)
        /// </summary>
        /// <returns>Это GS1 код: Да/Нет</returns>
        private bool AreThreeSymbolsInStart()
        {
            var threeSymbols = string.Empty;

            try
            {
                threeSymbols = Encoding.Default.GetString(IncomingString[Cursor..(Cursor + 3)]);
            }
            catch (ArgumentException ex)
            {
                HandleErrors(ex.Message);
            }

            return threeSymbols switch
            {
                "]C1" => true,// (Спецификация 7.8, на схеме пункт: 1.GS1-128)
                "]e0" => true,// (Спецификация 7.8, на схеме пункт: 2.GS1 DataBar and GS1 Composite symbols)
                "]d2" => true,// (Спецификация 7.8, на схеме пункт: 3.GS1 DataMatrix)
                "]Q3" => true,// (Спецификация 7.8, на схеме пункт: 4.GS1 QR Code)
                "]J1" => true,// (Спецификация 7.8, на схеме пункт: 5.GS1 DotCode)
                _ => false,
            };
        }


        /// <summary>
        /// Проверить содержитли исходнаяя строка заданное количество символов от курсора.
        /// В слушчае, если строка содержит нужное количество символов, то вернется в выходных параметрах количество символов которое нужно скопировать в буфер.
        /// </summary>
        /// <param name="len">Выходной параметр длинны копируемой строки</param>
        /// <param name="haveBraces">Содержится ли тег кода применения в скобках</param>
        /// <returns>Соответствует длинна: Да/Нет</returns>
        private bool DoesStringContainAtLeastTheCorrectNumberOfNumberCharacters(out int len, out bool haveBraces)
        {
            var twoDigit = string.Empty;
            var result = false;

            len = 0;
            haveBraces = false;

            try
            {
                // Возмем первая два символа от текущей позиции для проверки в таблице предопреденных кодов применения
                if ((Encoding.Default.GetString(IncomingString[Cursor..(Cursor + 1)]) == "(") && Encoding.Default.GetString(IncomingString[(Cursor + 3)..(Cursor + 4)]) == ")")
                {
                    twoDigit = Encoding.Default.GetString(IncomingString[(Cursor + 1)..(Cursor + 3)]);
                    haveBraces = true;
                    len += 2;
                }
                else
                {
                    twoDigit = Encoding.Default.GetString(IncomingString[Cursor..(Cursor + 2)]);
                }
            }
            catch (ArgumentException ex)
            {
                HandleErrors(ex.Message);
            }

            // Объект предопределенного кода применения
            if (PredefinedIdentifier.TryGetValue(twoDigit, out int findedPredefinedIdentifier))
            {
                // Если не нашли код применения или количество оставшихся символов в строке меньше заявленой длинне, то выходим из обработки.
                if (findedPredefinedIdentifier <= (IncomingString[Cursor..].Length + (haveBraces ? 2 : 0)))
                {
                    len += findedPredefinedIdentifier;
                    result = true;
                }
            }

            return result;
        }


        /// <summary>
        /// Добавляем символ в массив
        /// </summary>
        /// <param name="bArray">Существующий массив</param>
        /// <param name="newByte">Новый байт для добавления в конец</param>
        /// <returns>Новый массив</returns>
        private byte[] AddByteToArray(byte[] bArray, byte newByte)
        {
            var newArray = new byte[bArray.Length + 1];

            bArray.CopyTo(newArray, 0);
            newArray[^1] = newByte;

            return newArray;
        }


        /// <summary>
        /// Если это символ GS1, то ставим курсоз за него
        /// </summary>
        private void IfThisCharacterIsGsOneMovePastIt()
        {
            if (IncomingString[Cursor] == _gs1)
                Cursor++;
        }


        /// <summary>
        /// Если следующий символ GS1, то просто инкриментируем курсор.
        /// </summary>
        private void MovePastGsOne()
        {
            if (IncomingString[Cursor] == _gs1)
                Cursor++;
        }


        /// <summary>
        /// Проверка содержит ли буфер символ GS1
        /// </summary>
        /// <returns>В буфере есть символ GS1: Да/Нет</returns>
        private bool DoesBufferContainGsOne() => Buffer.Contains(_gs1);


        /// <summary>
        /// Содержит ли исходная строка символ GS1 начиная от курсора и до конца строки.
        /// </summary>
        /// <returns>Символ присутствует в строке: Да/Нет</returns>
        private bool DoesStringContainsGsOne() => IncomingString[Cursor..].Contains(_gs1);


        /// <summary>
        /// Проверим, что строка с позиции курсора содержит символы.
        /// </summary>
        /// <returns>Символы содержатся: Да/Нет</returns>
        private bool IsAnyDataPresent() => IncomingString[Cursor..] != null && IncomingString[Cursor..].Length > 0;


        /// <summary>
        /// dispose
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (IncomingString != null)
                        Array.Clear(IncomingString);
                }
            }
            _disposed = true;
        }


        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        ~Parser()
        {
            Dispose(false);
        }
    }
}
