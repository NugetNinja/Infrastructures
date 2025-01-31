﻿#nullable enable
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Aiursoft.SDKTools.Attributes
{
    public class ValidFolderName : TestableValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is string val)
            {
                return
                    val.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 &&
                    !string.IsNullOrWhiteSpace(val);
            }
            return false;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (IsValid(value))
            {
                return ValidationResult.Success;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(value as string))
                {
                    return new ValidationResult($"Empty string is not a valid file name!");
                }

                var invalidCharacters = string.Empty;
                foreach (var invalidChar in Path.GetInvalidFileNameChars())
                {
                    if (value is string val && val.Contains(invalidChar))
                    {
                        invalidCharacters += $" '{invalidChar}',";
                    }
                }
                return new ValidationResult($"The {validationContext.DisplayName} can not contain invalid characters{invalidCharacters.TrimEnd(',')}!");
            }
        }
    }
}
