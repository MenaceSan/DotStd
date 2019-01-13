using System;
using System.Collections.Generic;
using System.Text;

namespace DotStd
{
    public enum ValidLevel
    {
        // What sort of validation do i have for some object or property.
        OK = 0,
        Warning,        // We may still save this object but it looks odd.
        WarningManager, // This needs a managers approval.
        WarningAdmin,
        Fail,        // This object is not valid because this property is not valid.
    }

    public class ValidField
    {
        // Similar to System.ComponentModel.DataAnnotations.ValidationResult
        // MemberName = "" for global form error.
        public string MemberName;       // reflection name of some property/field.
        public ValidLevel Level;
        public string Description;      // Why did we validate it this way ? null or "" = ok.
    }

    public interface IValidatable<T>
    {
        // This class is used to validate some other type. AKA Validator
        // Filter for valid strings/objects?
        // like System.Web.UI.IValidator

        bool IsValid(T s);
    }

    public class ValidState // : System.ComponentModel.IDataErrorInfo
    {
        // Hold the validation state for some object.
        // helper class for general validation of some object.
        // A View model may have SUGGESTIONS for validation. We may optionally ignore them. (Zip code looks weird, etc)
        // Similar to System.ComponentModel.DataAnnotations.Validator
        // Used with Microsoft.AspNetCore.Mvc.ModelStateDictionary

        public const int kInvalidId = 0;    // This is never a valid id in the db. ValidState.IsValidId()

        public ValidLevel ValidLevel;       // aggregate state for Fields

        public bool IsValid { get { return this.ValidLevel == ValidLevel.OK; } }

        public Dictionary<string, ValidField> Fields = new Dictionary<string, ValidField>();    // details

        //**********************

        public static bool IsNull(object obj)
        {
            // is a null ref type or a nullable value type that is null.
            return obj == null || obj == DBNull.Value;
        }

        public static bool IsEmpty(object obj)
        {
            // is object equiv to null or empty string. string.IsNullOrEmpty() but NOT whitespace.
            // DateTime.MinValue might qualify ?
            // NOTE: 0 value is not the same as empty.

            if (IsNull(obj) || String.IsNullOrEmpty(obj.ToString()))
                return true;
            return false;
        }

        public static bool IsWhiteSpaceStr(object obj)  // similar to IsEmpty()
        {
            if (IsNull(obj) || String.IsNullOrWhiteSpace(obj.ToString()))
                return true;
            return false;
        }

        public static bool IsTrue(string s)
        {
            if (string.Compare(s, "true", true) == 0)
                return true;
            if (string.Compare(s, "t", true) == 0)
                return true;
            if (string.Compare(s, "yes", true) == 0)
                return true;
            if (string.Compare(s, "y", true) == 0)
                return true;
            return false;
        }

        public static bool IsFalse(string s)
        {
            if (string.Compare(s, "false", true) == 0)
                return true;
            if (string.Compare(s, "f", true) == 0)
                return true;
            if (string.Compare(s, "no", true) == 0)
                return true;
            if (string.Compare(s, "n", true) == 0)
                return true;
            return false;
        }

        public static bool IsValidId(int? id)
        {
            // is this a valid db id ? db convention says all id must be > 0
            if (!id.HasValue)
                return false;
            return id.Value > kInvalidId;
        }
        public static bool IsValidId(Enum id)
        {
            return id.ToInt() > kInvalidId;
        }

        public static bool IsValidUnique(string s)
        {
            // Is unique string valid ? might be email ?
            // This should be a unique string for InvoiceNo, EmployeeNo, ClientNo, ClaimNo, etc. 
            // AlphNumeric + "_@.-+"   NOT "," 

            if (string.IsNullOrWhiteSpace(s))
                return false;
            string sl = s.ToLower();
            if (sl == "none" || sl == "null" || sl == "-none")      // StripeCustomerId = none ?
                return false;
            // if (sl == "0") return false;
            // TODO MUST NOT CONTAIN WHITESPACE !!! 
            return s.Length > 2;
        }

        //*********************

        public void AddError(string nameField, string error)
        {
            // Record some error. nameField = nameof(x);
            this.ValidLevel = ValidLevel.Fail;
            Fields.Add(nameField, new ValidField { MemberName = nameField, Description = error, Level = ValidLevel.Fail });
        }

        public string GetErrorHTML(bool allFields = true)
        {
            // Get an HTML string describing the full error state of the object.
            var sb = new StringBuilder();
            foreach (var f in Fields)
            {
                if (allFields || string.IsNullOrWhiteSpace(f.Key))
                {
                    sb.Append("<p>");
                    sb.Append(f.Value.Description);
                    sb.Append("</p>");
                }
            }
            return sb.ToString();
        }
    }

    public interface IValidatable
    {
        // An Object supports this interface to indicate/test its valid state. Test properties.
        // Interface so any object ViewModel or DataModel may validate itself.
        // Similar to ASP ModelState.TryValidateModel
        // like System.Web.UI.IValidator

        // Name ?
        // Save // Persist object to db ?

        ValidState GetValidState();
    }
}
