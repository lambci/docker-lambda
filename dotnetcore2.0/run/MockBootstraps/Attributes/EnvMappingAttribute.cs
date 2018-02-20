using System;

namespace MockLambdaRuntime.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class EnvMappingAttribute : Attribute
    {
        public EnvMappingAttribute(string key)
        {
            Key = key;
        }

        public EnvMappingAttribute(string key, string defaultValue)
        {
            Key = key;
            DefaultValue = defaultValue;
        }

        public string Key { get; }
        public string DefaultValue { get; }
    }
}
