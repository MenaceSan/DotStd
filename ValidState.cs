using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace DotStd
{
    /// <summary>
    /// This class is used to validate some other class type. AKA Validator
    /// Should we just use Func<T, bool> ??
    /// Filter for valid strings/objects?
    /// like System.Web.UI.IValidator
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IValidatorT<T>
    {
        bool IsValid(T s);
    }

    /// <summary>
    /// What sort of validation do i have for some object or property.
    /// </summary>
    public enum ValidLevel
    {
        OK = 0,
        Warning,        // We may still save this object but it looks odd.
        WarningManager, // This needs a managers approval.
        WarningAdmin,   // This needs a Admin approval.
        Fail,        // This object is not valid because this property is not valid. Can not proceed.
    }

    /// <summary>
    /// Similar to System.ComponentModel.DataAnnotations.ValidationResult
    /// MemberName = "" for global form error.
    /// </summary>
    public class ValidField
    {
        public string MemberName;       // reflection name of some property/field.
        public string Description;      // Why did we validate it this way ? null or "" = OK.
        public ValidLevel Level;    // fail or warn ?

        public ValidField(string memberName, string description, ValidLevel level = ValidLevel.OK)
        {
            MemberName = memberName;
            Description = description;
            Level = level;
        }
    }

    /// <summary>
    /// Hold the validation state for some object. untranslated.
    /// helper class for general validation of some object.
    /// A View model may have SUGGESTIONS for validation. We may optionally ignore them. (Postal code looks weird, etc)
    /// Similar to System.ComponentModel.DataAnnotations.Validator
    /// Used with Microsoft.AspNetCore.Mvc.ModelStateDictionary
    /// </summary>
    public class ValidState : System.ComponentModel.IDataErrorInfo
    {
        public const int kInvalidId = default;    // 0 This is NEVER a valid id/PK in the db. ValidState.IsValidId() default(int)
        public static readonly string kInvalidName = "??";   // This data is broken but display something. 

        public const string kSeeBelowFor = "See below for problem description.";    // default top level error.
        public const string kSavedChanges = "Saved Changes";    // used with errorMsg = StringUtil._NoErrorMsg = ""
        public const string kNoChanges = "No Changes";      // We pressed 'save' but there was nothing to do. used with errorMsg = null

        public ValidLevel ValidLevel;       // aggregate state for all Fields

        public bool IsValid { get { return this.ValidLevel == ValidLevel.OK; } }

        public Dictionary<string, ValidField> Fields = new();    // details

        // implement IDataErrorInfo
        public string this[string columnName]
        {
            get
            {
                return Fields[columnName].Description;
            }
            set
            {
                Fields[columnName] = new ValidField(columnName, value);
            }
        }

        // implement IDataErrorInfo. Whole object error not specific to a single field.
        public string Error
        {
            get
            {
                if (!Fields.TryGetValue(string.Empty, out ValidField? val))
                    return "";
                return val.Description;
            }
            set
            {
                Fields[string.Empty] = new ValidField(string.Empty, value);
            }
        }

        //**********************

        /// <summary>
        /// is a null ref type or a nullable value type that is null.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool IsNull([NotNullWhen(false)] object? obj)
        {
            return obj == null || obj == DBNull.Value;
        }

        /// <summary>
        /// is object equiv to null or empty string. string.IsNullOrEmpty() but NOT whitespace.
        /// DateTime.MinValue might qualify ?
        /// NOTE: If this is an array, is the array empty ?
        /// NOTE: 0 value is not the same as empty.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool IsEmpty([NotNullWhen(false)] object? obj)
        {
            if (IsNull(obj) || string.IsNullOrEmpty(obj.ToString()))
                return true;
            return false;
        }

        public static bool IsWhiteSpaceStr([NotNullWhen(false)] object? obj)  // similar to IsEmpty()
        {
            if (IsNull(obj) || String.IsNullOrWhiteSpace(obj.ToString()))
                return true;
            return false;
        }

        public static bool IsNullOrDefault<T>([NotNullWhen(false)] T? obj)
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

        /// <summary>
        /// is this a valid db id ? db convention says all id must be > 0
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool IsValidId([NotNullWhen(true)] int? id)
        {
            if (!id.HasValue)
                return false;
            return id.Value != kInvalidId;      // default(int)
        }
        public static bool IsValidId([NotNullWhen(true)] Enum? id)
        {
            if (id == null)
                return false;
            return id.ToInt() != kInvalidId;
        }

        public const string kUniqueAllowX = "_@.-+"; // special allowed chars for unique ids.

        /// <summary>
        /// Is string valid as a unique id ? might be email ?
        /// This should be a proper unique string for Uid. 
        /// Can be used to detect proper property/field names from untrusted sources. e.g. Grid column sort.
        /// AlphNumeric + "_@.-+" ONLY NOT "," 
        /// MUST NOT CONTAIN WHITESPACE 
        /// Use Converter.ToUnique(s)
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsValidUnique([NotNullWhen(true)] string? s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return false;
            string sl = s.ToLower();
            if (sl == SerializeUtil.kNull || sl == "none" || sl == "-none") // disallow this.
                return false;

            foreach (char ch in s)
            {
                if (ch >= 'a' && ch <= 'z')
                    continue;
                if (StringUtil.IsDigit1(ch))
                    continue;
                if (kUniqueAllowX.Contains(ch))      // allowed special chars.
                    continue;
                return false;   // bad char.
            }
            return s.Length > 2;
        }

        //*********************

        /// <summary>
        /// Record some error for a field.
        /// </summary>
        /// <param name="nameField">nameof(x). "" = global error</param>
        /// <param name="errorMsg">untranslated error description.</param>
        public void AddError(string nameField, string? errorMsg)
        {
            if (string.IsNullOrWhiteSpace(errorMsg))    // not a real error.
                return;
            this.ValidLevel = ValidLevel.Fail;
            Fields.Add(nameField, new ValidField(nameField, errorMsg, ValidLevel.Fail));
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

        /// <summary>
        /// Assert that this is true. throw if not.
        /// like Trace.Assert()
        /// like Debug.Assert() but also works for release code. 
        /// The Assert methods are not available for Windows Store apps.
        /// </summary>
        /// <param name="isBad"></param>
        /// <param name="msg"></param>
        /// <exception cref="ArgumentException"></exception>
        public static void ThrowIf(bool isBad, string? msg = null)
        {
            if (isBad) // ok?
            {
                throw new ArgumentException(msg ?? "Internal Check Failed");
            }
        }

        /// <summary>
        /// verify that the argument is not null. use nameof(Property).
        /// </summary>
        /// <param name="obj">The object that will be validated.</param>
        /// <param name="name">The name of the <i>argument</i> that will be used to identify it should an exception be thrown. use nameof(Property)</param>
        /// <exception cref="ArgumentNullException">Thrown when <i>argument</i> is null.</exception>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowIfNull([NotNull] object? obj, string? name = null)
        {
            // ala AssertTrue().IsNotNull()
            if (null == obj)
            {
                throw new ArgumentNullException(name ?? (new StackFrame(1, true).GetMethod()?.Name));
            }
        }

        /// <summary>
        /// The result will not be null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T GetNotNull<T>([NotNull] T? obj, string? name = null)
        {
            if (null == obj)
            {
                throw new ArgumentNullException(name ?? (new StackFrame(1, true).GetMethod()?.Name));
            }
            return obj;
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
        public static void ThrowIfEmpty([NotNull] string? argument, string name)
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
        public static void ThrowIfWhiteSpace([NotNull] string? argument, string name)
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
