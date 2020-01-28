using System;
using System.Collections.Generic;
using System.Reflection;
using LanguageImplementation;
using LanguageImplementation.DataTypes;
using LanguageImplementation.DataTypes.Exceptions;

namespace CloacaInterpreter
{
    public class ObjectResolver
    {
        public static object GetValue(string attrName, object rawObject)
        {
            var asPyObj = rawObject as PyObject;
            if (asPyObj != null)
            {
                var val = asPyObj.__getattribute__(attrName);
                return val;
            }
            else
            {
                try
                {
                    // Try it as a field and then as a property.
                    var objType = rawObject.GetType();
                    var member = objType.GetMember(attrName);
                    if (member[0].MemberType == System.Reflection.MemberTypes.Property)
                    {
                        return rawObject.GetType().GetProperty(attrName).GetValue(rawObject);
                    }
                    else if (member[0].MemberType == System.Reflection.MemberTypes.Field)
                    {
                        return rawObject.GetType().GetField(attrName).GetValue(rawObject);
                    }
                    else if(member[0].MemberType == System.Reflection.MemberTypes.Method)
                    {
                        // If there's only one method for the given name (most common), then
                        // let's skip all of this filtering.
                        if(member.Length == 1)
                        {
                            return new WrappedCodeObject(member[0].Name, rawObject.GetType().GetMethod(attrName), rawObject);
                        }
                        else
                        {
                            var allMethods = objType.GetMethods();
                            var overloads = new MethodInfo[member.Length];
                            int overload_i = 0;
                            foreach (var methodInfo in allMethods)
                            {
                                if (methodInfo.Name == member[0].Name)
                                {
                                    overloads[overload_i] = methodInfo;
                                    overload_i += 1;
                                }
                            }
                            return new WrappedCodeObject(member[0].Name, overloads, rawObject);
                        }
                    }
                    else if(member[0].MemberType == System.Reflection.MemberTypes.Event)
                    {
                        throw new EscapedPyException(new NotImplemented("'" + rawObject.GetType().Name + "' object attribute named '" + attrName + "' is an event, which we cannot yet extract."));
                    }
                    else
                    {
                        throw new EscapedPyException(new NotImplemented("'" + rawObject.GetType().Name + "' object attribute named '" + attrName + "' is neither a field, method, event, nor property."));
                    }
                }
                catch (ArgumentException e)
                {
                    throw new EscapedPyException(new AttributeError("'" + rawObject.GetType().Name + "' object has no attribute named '" + attrName + "'"));
                }
            }
        }
    }
}
