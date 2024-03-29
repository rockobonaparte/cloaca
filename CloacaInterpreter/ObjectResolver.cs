﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using LanguageImplementation;
using LanguageImplementation.DataTypes;
using LanguageImplementation.DataTypes.Exceptions;

namespace CloacaInterpreter
{
    /// <summary>
    /// Generated by ObjectResolver to represent .NET event bindings. All of this information is needed between subscribing and
    /// unsubscribing .NET events. This is what is intended for the Cloaca interpreter stack for INPLACE_ADD and INPLACE_SUB for
    /// subscribe/unsubscribe, respectively.
    /// </summary>
    public class EventInstance
    {
        public object OwnerObject { get; private set; }
        public EventInfo EventInfo { get; private set; }

        /// <summary>
        /// The MulticastDelegate from the event is needed in particular in order to unsubscribe from the event properly later.
        /// It has to be manually searched for an invocation to the given IPyCallable that is being wrapped by the
        /// CallableDelegateProxy.
        /// </summary>
        public MulticastDelegate EventDelegate { get; private set; }

        public EventInstance(object ownerObject, string attrName)
        {
            this.OwnerObject = ownerObject;
            this.EventInfo = ownerObject.GetType().GetEvent(attrName);
            this.EventDelegate = (MulticastDelegate)ownerObject.GetType()
                .GetField(attrName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                .GetValue(ownerObject);
        }
    }

    public class ObjectResolver
    {
        public static void SetValue(string attrName, object rawObject, object value)
        {
            var asPyObj = rawObject as PyObject;
            if (asPyObj != null)
            {
                asPyObj.__setattr__(attrName, value);
            }
            else
            {
                try
                {
                    // If it's not a type, then get its type. If it's a type, we have the reflection information already.
                    // This became an issue when we had to resolve stuff being imported from assemblies.
                    var objType = rawObject as Type;
                    if (objType == null)
                    {
                        objType = rawObject.GetType();
                    }

                    // Try it as a field and then as a property.
                    var member = objType.GetMember(attrName);
                    if (member.Length == 0)
                    {
                        // We have a catch for ArgumentException but it also looks like GetMember will just return an empty list if the attribute is not found.
                        throw new EscapedPyException(new AttributeError("'" + objType.Name + "' object has no attribute named '" + attrName + "'"));
                    }
                    if (member[0].MemberType == System.Reflection.MemberTypes.Property)
                    {
                        var property = objType.GetProperty(attrName);
                        property.SetValue(rawObject, PyNetConverter.Convert(value, property.PropertyType));
                    }
                    else if (member[0].MemberType == System.Reflection.MemberTypes.Field)
                    {
                        var field = objType.GetField(attrName);
                        field.SetValue(rawObject, PyNetConverter.Convert(value, field.FieldType));
                    }
                    else if (member[0].MemberType == System.Reflection.MemberTypes.Method)
                    {
                        throw new EscapedPyException(new AttributeError("'" + objType.Name + "' is a .NET object and its methods cannot be reassigned"));
                    }
                    else if (member[0].MemberType == System.Reflection.MemberTypes.Event)
                    {
                        throw new EscapedPyException(new AttributeError("'" + objType.Name + "' is a .NET object and its events cannot be reassigned"));
                    }
                    else
                    {
                        throw new EscapedPyException(new NotImplementedError("'" + objType.Name + "' object attribute named '" + attrName + "' is neither a field, method, event, nor property."));
                    }
                }
                catch (ArgumentException e)
                {
                    throw new EscapedPyException(new AttributeError(e.Message));
                }

            }
        }

        #region Extension Method Resolution
        // Original inspiration: https://stackoverflow.com/questions/299515/reflection-to-identify-extension-methods
        private static MethodInfo[] GetExtensionMethods(Type t)
        {
            List<Type> AssTypes = new List<Type>();

            foreach (Assembly item in AppDomain.CurrentDomain.GetAssemblies())
            {
                AssTypes.AddRange(item.GetTypes());
            }

            var query = from type in AssTypes
                        where type.IsSealed && !type.IsGenericType && !type.IsNested
                        from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                        where method.IsDefined(typeof(ExtensionAttribute), false)
                        where method.GetParameters()[0].ParameterType == t
                        select method;
            return query.ToArray<MethodInfo>();
        }

        /// <summary>
        /// Search for an extension method.
        /// </summary>
        /// <param name="MethodName">Name of the Methode</param>
        /// <returns>the found Method or null</returns>
        private static MethodInfo GetExtensionMethod(Type t, string MethodName)
        {
            var mi = from methode in GetExtensionMethods(t)
                     where methode.Name == MethodName
                     select methode;
            if (mi.Count<MethodInfo>() <= 0)
                return null;
            else
                return mi.First<MethodInfo>();
        }
        #endregion Extension Method Resolution

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
                    // If it's not a type, then get its type. If it's a type, we have the reflection information already.
                    // This became an issue when we had to resolve stuff being imported from assemblies.
                    var objType = rawObject as Type;
                    if (objType == null)
                    {
                        objType = rawObject.GetType();
                    }

                    // Try it as a field and then as a property.
                    var member = objType.GetMember(attrName);
                    if(member.Length == 0)
                    {
                        // We couldn't find this as a member of the class. However, it could be an extension method. Hooray!
                        var extensionMethod = GetExtensionMethod(objType, attrName);
                        if (extensionMethod == null)
                        {
                            // We have a catch for ArgumentException but it also looks like GetMember will just return an empty list if the attribute is not found.
                            throw new EscapedPyException(new AttributeError("'" + objType.Name + "' object has no attribute named '" + attrName + "'"));
                        }
                        else
                        {
                            return new WrappedCodeObject(attrName, extensionMethod, rawObject);
                        }
                    }
                    if (member[0].MemberType == System.Reflection.MemberTypes.Property)
                    {
                        return objType.GetProperty(attrName).GetValue(rawObject);
                    }
                    else if (member[0].MemberType == System.Reflection.MemberTypes.Field)
                    {
                        return objType.GetField(attrName).GetValue(rawObject);
                    }
                    else if(member[0].MemberType == System.Reflection.MemberTypes.Method)
                    {
                        // If there's only one method for the given name (most common), then
                        // let's skip all of this filtering.
                        if(member.Length == 1)
                        {
                            return new WrappedCodeObject(member[0].Name, objType.GetMethod(attrName), rawObject);
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
                        return new EventInstance(rawObject, attrName);
                    }
                    else
                    {
                        throw new EscapedPyException(new NotImplementedError("'" + objType.Name + "' object attribute named '" + attrName + "' is neither a field, method, event, nor property."));
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
