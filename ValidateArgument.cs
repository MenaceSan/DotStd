using System;

namespace DotStd
{
    public static class ValidateArgument
    {
        // Internal assertions for code.

        /// <summary>
        /// verify that the argument is not null. use nameof(Property).
        /// </summary>
        /// <param name="argument">The object that will be validated.</param>
        /// <param name="name">The name of the <i>argument</i> that will be used to identify it should an exception be thrown. use nameof(Property)</param>
        /// <exception cref="ArgumentNullException">Thrown when <i>argument</i> is null.</exception>
        public static void EnsureNotNull(object argument, string name)
        {
            if (null == argument)
            {
                throw new ArgumentNullException(name);
            }
        }

        /// <summary>
        /// Throw if arguments are out of range. use nameof(Property)
        /// </summary>
        public static void EnsureZeroOrGreater(int n, string name)
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
        public static void EnsureIsValidId(int argument, string name)
        {
            // IsValidId()
            if (argument <= 0)
            {
                throw new ArgumentException("The argument must be greater than zero.", name);
            }
        }

        /// <summary>
        /// verify that the string is not null or zero length.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when <i>argument</i> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <i>argument</i> is an empty string.</exception>
        public static void EnsureNotNullOrEmpty(string argument, string name)
        {
            ValidateArgument.EnsureNotNull(argument, name);
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
        public static void EnsureNotNullOrWhiteSpace(string argument, string name)
        {
            ValidateArgument.EnsureNotNull(argument, name);
            if (string.IsNullOrWhiteSpace(argument))
            {
                throw new ArgumentException("The value cannot be an empty string or contain only whitespace.", name);
            }
        }
    }
}
