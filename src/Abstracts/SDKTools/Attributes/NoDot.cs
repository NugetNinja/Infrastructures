﻿#nullable enable
using System.ComponentModel.DataAnnotations;

namespace Aiursoft.SDKTools.Attributes
{
    public class NoDot : TestableValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is string val)
            {
                return !val.Contains(".");
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
                return new ValidationResult($"The {validationContext.DisplayName} can not contain dot!");
            }
        }
    }
}
