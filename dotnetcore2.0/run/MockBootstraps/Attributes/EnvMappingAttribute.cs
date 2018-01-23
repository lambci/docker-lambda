using System;

namespace MockLambdaRuntime.Attributes
{
    public class EnvMappingAttribute:Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvMappingAttribute"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        public EnvMappingAttribute(string key)
        {
            Key = key;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvMappingAttribute"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        public EnvMappingAttribute(string key, string defaultValue)
        {
            Key = key;
            DefaultValue = defaultValue;
        }

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        public string DefaultValue { get; set; }
    }
}