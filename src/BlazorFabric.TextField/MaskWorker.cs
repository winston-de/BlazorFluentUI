using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BlazorFabric
{
    public class MaskWorker
    {
        private readonly Dictionary<char, string> _defaultMaskFormatChars;
        private readonly char _defaultMaskChar;
        public MaskWorker()
        {
            _defaultMaskFormatChars = new Dictionary<char, string>()
            {
                { '9',"[0-9]" },
                { 'a',"[a-zA-Z]" },
                { '*',"[a-zA-Z0-9]" }
            };
            _defaultMaskChar = '_';
        }

        public ICollection<MaskValue> ParseMask(string mask)
        {
            if (string.IsNullOrEmpty(mask))
                return null;

            var MaskCharData = new List<MaskValue>();
            int excapeCounter = 0;
            for (int i = 0; i + excapeCounter < mask.Length; i++)
            {
                var character = mask[i + excapeCounter];
                if (character == '\\')
                {
                    excapeCounter++;
                    continue;
                }
                else
                {
                    string maskFormat;
                    if (_defaultMaskFormatChars.TryGetValue(character, out maskFormat))
                    {
                        MaskCharData.Add(new MaskValue()
                        {
                            DisplayIndex = i,
                            Format = new Regex(maskFormat)
                        });
                    }
                }

            }
            return MaskCharData;
        }

        public string GetMaskDisplay(string mask, ICollection<MaskValue> maskCharData, char? maskChar)
        {
            var maskDisplay = mask;

            if (string.IsNullOrWhiteSpace(maskDisplay))
                return "";

            maskDisplay = maskDisplay.Replace("\\", "");

            foreach (var charData in maskCharData)
            {
                char nextChar = maskChar.HasValue ? maskChar.Value : _defaultMaskChar;
                if (charData.Value.HasValue)
                {
                    nextChar = charData.Value.Value;
                }

                maskDisplay = maskDisplay.Substring(0, charData.DisplayIndex) + nextChar + maskDisplay.Substring(charData.DisplayIndex + 1);
            }

            return maskDisplay;
        }
    }
}
