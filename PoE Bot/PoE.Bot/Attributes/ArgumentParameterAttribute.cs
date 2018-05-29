using System;

namespace PoE.Bot.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class ArgumentParameterAttribute : Attribute
    {
        /// <summary>
        /// Gets the parameter's description.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Gets whether the parameter is required.
        /// </summary>
        public bool IsRequired { get; private set; }

        /// <summary>
        /// Defines a new command parameter.
        /// </summary>
        /// <param name="description">Parameter's description.</param>
        /// <param name="required">Whether or not the parameter is required.</param>
        public ArgumentParameterAttribute(string description, bool required)
        {
            this.Description = description;
            this.IsRequired = required;
        }
    }
}
