﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace DotStd
{
    /// <summary>
    /// Try to translate the validation error messages based on some unknown context.
    /// e.g. make them appropriate to the HttpContext that is current for the thread.
    /// </summary>
    public static class TransValidationAttribute
    {
        public static Func<ITranslatorProvider1?>? _GetTranslator;     // My application should provide this. singleton.

        public static ITranslatorProvider1? GetTranslatorProvider()
        {
            // Get/Make the ITranslatorProvider for the ASP HttpContext that is appropriate for my thread/session.
            if (_GetTranslator == null)
                return null;
            return _GetTranslator();
        }
    }

    public class TransRequiredAttribute : RequiredAttribute
    {
        // use [TransRequired] not [Required]

        public override string FormatErrorMessage(string name)
        {
            // override ValidationAttribute
            ITranslatorProvider1? trans = TransValidationAttribute.GetTranslatorProvider();
            if (trans != null)
            {
                Task<string> task = trans.TranslateAsync(base.ErrorMessageString ?? "The {0} field is required.");
                task.Wait();        // Wait for async.
                string errorMsg = task.Result;
                return string.Format(errorMsg, name);
            }

            return base.FormatErrorMessage(name);
        }
    }


    // TODO OTHER VALIDATION ATTRIBUTES ??

}
