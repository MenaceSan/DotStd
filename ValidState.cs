using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DotStd
{
    public interface IValidatorT<T>
    {
        // This class is used to validate some other class type. AKA Validator
        // Should we just use Func<T, bool> ??
        // Filter for valid strings/objects?
        // like System.Web.UI.IValidator

        bool IsValid(T s);
    }

    public enum ValidLevel
    {
        // What sort of validation do i have for some object or property.
        OK = 0,
        Warning,        // We may still save this object but it looks odd.
        WarningManager, // This needs a managers approval.
        WarningAdmin,   // This needs a Admin approval.
        Fail,        // This object is not valid because this property is not valid. Can not proceed.
    }

    public class ValidField
    {
        // Similar to System.ComponentModel.DataAnnotations.ValidationResult
        // MemberName = "" for global form error.
        public string MemberName;       // reflection name of some property/field.
        public ValidLevel Level;
        public string Description;      // Why did we validate it this way ? null or "" = ok.
    }

    public class ValidState : System.ComponentModel.IDataErrorInfo
    {
        // Hold the validation state for some object.
        // helper class for general validation of some object.
        // A View model may have SUGGESTIONS for validation. We may optionally ignore them. (Postal code looks weird, etc)
        // Similar to System.ComponentModel.DataAnnotations.Validator
        // Used with Microsoft.AspNetCore.Mvc.ModelStateDictionary

        public const int kInvalidId = 0;    // This is never a valid id/PK in the db. ValidState.IsValidId()

        public const string kSeeBelowFor = "See below for problem description.";    // default top level error.
        public const string kSavedChanges = "Saved Changes";
        public const string kNoChanges = "No Changes";      // We pressed 'save' but there was nothing to do.

        public ValidLevel ValidLevel;       // aggregate state for all Fields

        public bool IsValid { get { return this.ValidLevel == ValidLevel.OK; } }

        public Dictionary<string, ValidField> Fields = new Dictionary<string, ValidField>();    // details

        // implement IDataErrorInfo
        public string this[string columnName]
        {
            get
            {
                return Fields[columnName].Description;
            }
            set
            {
                Fields[columnName] = new ValidField { MemberName = columnName, Description = value };
            }
        }

        // implement IDataErrorInfo. Whole object error not specific to a single field.
        public string Error
        {
            get
            {
                ValidField val;
                if (!Fields.TryGetValue(string.Empty, out val))
                    return "";
                return val.Description;
            }
            set
            {
                Fields[string.Empty] = new ValidField { MemberName = string.Empty, Description = value };
            }
        }

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

            if (IsNull(obj) || string.IsNullOrEmpty(obj.ToString()))
                return true;
            return false;
        }

        public static bool IsWhiteSpaceStr(object obj)  // similar to IsEmpty()
        {
            if (IsNull(obj) || String.IsNullOrWhiteSpace(obj.ToString()))
                return true;
            return false;
        }

        public static bool IsNullOrDefault<T>(T obj)
        {
            return obj == null || obj.Equals(default(T));
        }

        public static bool IsTrue(string s)
        {
            if (string.Compare(s, SerializeUtil.kTrue, true) == 0)
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
            if (string.Compare(s, SerializeUtil.kFalse, true) == 0)
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
            return id.Value != kInvalidId;
        }
        public static bool IsValidId(Enum id)
        {
            return id.ToInt() != kInvalidId;
        }

        public const string kUniqueAllowX = "_@.-+"; // special allowed chars.

        public static bool IsValidUnique(string s)
        {
            // Is string valid as a unique id ? might be email ?
            // This should be a proper unique string for Uid. 
            // Can be used to detect proper property/field names from untrusted sources. e.g. Grid column sort.
            // AlphNumeric + "_@.-+" ONLY NOT "," 
            // MUST NOT CONTAIN WHITESPACE 
            // Use Converter.ToUnique(s)

            if (string.IsNullOrWhiteSpace(s))
                return false;
            string sl = s.ToLower();
            if (sl == SerializeUtil.kNull || sl == "none" || sl == "-none") 
                return false;

            foreach (char ch in s)
            {
                if (ch >= 'a' && ch <= 'z')
                    continue;
                if (ch >= '0' && ch <= '9')
                    continue;
                if (kUniqueAllowX.IndexOf(ch) >= 0)      // allowed special chars.
                    continue;
                return false;   // bad char.
            }
            return s.Length > 2;
        }

        //*********************

        public void AddError(string nameField, string error)
        {
            // Record some error. nameField = nameof(x);
            // nameField = "" = global error.
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

        //*********************
        // Internal assertions for code.

        public static void AssertTrue(bool isTrue, string msg = "")
        {
            // Internal check. No throw. record for debug purposes.
            if (isTrue)
                return;

            Debug.WriteLine("AssertTrue FAIL " + msg);
        }

        public static void ThrowIf(bool isBad, string msg = null)
        {
            // Assert that this is true. throe if not.
            // like Trace.Assert()
            // like Debug.Assert() but also works for release code. 
            // The Assert methods are not available for Windows Store apps.

            if (isBad) // ok?
            {
                if (msg == null)
                    msg = "Internal Check Failed";
                throw new ArgumentException(msg);
            }
        }

        /// <summary>
        /// verify that the argument is not null. use nameof(Property).
        /// </summary>
        /// <param name="argument">The object that will be validated.</param>
        /// <param name="name">The name of the <i>argument</i> that will be used to identify it should an exception be thrown. use nameof(Property)</param>
        /// <exception cref="ArgumentNullException">Thrown when <i>argument</i> is null.</exception>
        public static void ThrowIfNull(object argument, string name)
        {
            if (null == argument)
            {
                throw new ArgumentNullException(name);
            }
        }

        /// <summary>
        /// Throw if arguments are out of range. use nameof(Property)
        /// </summary>
        public static void ThrowIfNegative(int n, string name)
        {
            // 0 or positive int.
            if (n < 0)
            {
                throw new ArgumentOutOfRangeException(name, "argument must be >= 0");
            }
        }

        /// <summary>
        /// Throw if arguments are out of range.
        /// </summary>
        public static void ThrowIfBadId(int argument, string name)
        {
            if (!IsValidId(argument))
            {
                throw new ArgumentException("The argument must be greater than zero.", name);
            }
        }
        public static void ThrowIfBadId(Enum argument, string name)
        {
            if (!IsValidId(argument))
            {
                throw new ArgumentException("The argument must be greater than zero.", name);
            }
        }

        /// <summary>
        /// verify that the string is not null or zero length.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when <i>argument</i> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <i>argument</i> is an empty string.</exception>
        public static void ThrowIfEmpty(string argument, string name)
        {
            ThrowIfNull(argument, name);
            if (string.IsNullOrEmpty(argument))
            {
                throw new ArgumentException("The argument cannot be an empty string.", name);
            }
        }

        /// <summary>
        /// verify that the string is not null and that it doesn't contain only white space.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when <i>argument</i> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <i>argument</i> is either an empty string or contains only white space.</exception>
        public static void ThrowIfWhiteSpace(string argument, string name)
        {
            ThrowIfNull(argument, name);
            if (string.IsNullOrWhiteSpace(argument))
            {
                throw new ArgumentException("The value cannot be an empty string or contain only whitespace.", name);
            }
        }
    }

    public interface IValidatable
    {
        // An Object supports this interface to indicate/test its valid state. Test properties.
        // Interface so any object ViewModel or DataModel may validate itself.
        // Similar to ASP ModelState.TryValidateModel
        // like System.Web.UI.IValidator

        // Name ? // all objects support a name property ?
        // Save // Persist object to db ?

        ValidState GetValidState();
    }
}
