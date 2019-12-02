using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BlazorFabric
{
    public class MaskWorker
    {
        private IDictionary<char, Regex> maskFormatChars;
        private readonly char _defaultMaskChar;
        public MaskWorker(IDictionary<char, Regex> maskFormat)
        {
            if (maskFormat != null)
            {
                maskFormatChars = maskFormat;
            }
            else
            {
                maskFormatChars = new Dictionary<char, Regex>()
                {
                    { '9',new Regex("[0-9]") },
                    { 'a',new Regex("[a-zA-Z]") },
                    { '*',new Regex("[a-zA-Z0-9]") }
                };
            }

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
                    Regex maskFormat;
                    if (maskFormatChars.TryGetValue(character, out maskFormat))
                    {
                        MaskCharData.Add(new MaskValue()
                        {
                            DisplayIndex = i,
                            Format = maskFormat
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

        public int InsertString(ICollection<MaskValue> maskCharData, int selectionStart, string newValue)
        {
            int stringIndex = 0;
            int nextIndex = 0;
            bool isStringInserted = false;

            // Iterate through _maskCharData finding values with a displayIndex after the specified range start
            for (int i = 0; i < maskCharData.Count && stringIndex < newValue.Length; i++)
            {
                if (maskCharData.ToArray()[i].DisplayIndex >= selectionStart)
                {
                    isStringInserted = true;
                    nextIndex = maskCharData.ToArray()[i].DisplayIndex;
                    // Find the next character in the newString that matches the format
                    while (stringIndex < newValue.Length)
                    {
                        // If the character matches the format regexp, set the maskCharData to the new character
                        if (maskCharData.ToArray()[i].Format.IsMatch(newValue[stringIndex].ToString()))
                        {
                            Console.WriteLine($"Add Value {newValue[stringIndex]} to maskCharData");
                            maskCharData.ToArray()[i].Value = newValue[stringIndex];
                            stringIndex++;
                            // Set the nextIndex to the display index of the next mask format character.
                            if (i + 1 < maskCharData.Count)
                            {
                                nextIndex = maskCharData.ToArray()[i + 1].DisplayIndex;
                            }
                            else
                            {
                                nextIndex++;
                            }
                            break;
                        }
                        stringIndex++;
                    }
                }
            }

            return isStringInserted ? nextIndex : selectionStart;
        }
    }
}
