using LanguageImplementation.DataTypes;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace LanguageImplementation
{
    // TODO: Consider making this all static since it seems to get set up right at the point of injection.
    /// <summary>
    /// Helps WrappedCodeObjects inject the interpreter and frame context into wrapped calls that need them. Give it the current interpreter and frame
    /// context. Then when calls are being made, it'll take the actual arguments it has 
    /// </summary>    
    public class Injector
    {
        public IInterpreter Interpreter;
        public FrameContext Context;
        public IScheduler Scheduler;

        public Injector(IInterpreter interpreter, FrameContext context, IScheduler scheduler)
        {
            Prepare(interpreter, context, scheduler);
        }

        public void Prepare(IInterpreter interpreter, FrameContext context, IScheduler scheduler)
        {
            Interpreter = interpreter;
            Context = context;
            Scheduler = scheduler;
        }

        // Use for parameters; the abstraction we ask for in .NET bindings.
        public static bool IsInjectedParameterType(Type parameterType)
        {
            return parameterType == typeof(IInterpreter) ||
               parameterType == typeof(FrameContext) ||
               parameterType == typeof(IScheduler);
        }

        // Use for arguments; the actual instances we pass in.
        public static bool IsInjectedArgType(Type argType)
        {
            return typeof(IInterpreter).IsAssignableFrom(argType) ||
               typeof(FrameContext).IsAssignableFrom(argType) ||
               typeof(IScheduler).IsAssignableFrom(argType);
        }

        /// <summary>
        /// DEPRECATED DOCUMENTATION
        /// Makes no judgment about the parameter types in the methodBase, but just sets all arguments
        /// in the matching args array to the injectable type wherever they would apply. It assumed
        /// that the args array is the proper length.
        /// 
        /// It *will* however skip all args up-front that would match up with generic parameters for the
        /// method call. It's assumed these are being passed in to monomorphize the method.
        /// 
        /// </summary>
        /// <param name="methodBase"></param>
        /// <param name="args"></param>
        /// <param name="thisReference"></param>
        public object[] Inject2(MethodBase methodBase, object[] args, object thisReference = null, Dictionary<string, object> overrides = null)
        {
            var methodParams = methodBase.GetParameters();

            // If there's a params field then we have to cram an array into there.
            bool hasParamsField = methodParams.Length >= 1 && methodParams[methodParams.Length - 1].IsDefined(typeof(ParamArrayAttribute), false);
            bool isExtensionMethod = methodBase.IsExtensionMethod();

            // Transform all arguments except for any in the params field.
            object[] outParams;
            int in_param_i = 0;     // Keep an eye on this for later to determine if we have stuff for a params field!
            int out_param_i = 0;
            int methodInfo_i = 0;

            // Test for generic parameters in case we're making internal calls to stuff like DefaultNew and the
            // generic arguments are already filled in.
            int genericsCount = 0;

            // We used to first test ContainsGenericArguments but this would return false for monomorphized generics.
            // This was a problem because we'd still come in with the type variables, and these extra arguments would
            // foil the rest of parameterization.
            if (methodBase.IsConstructor)
            {
                // If this is a constructor, then the class we're instantiating itself might be generic. The constructor
                // can't legally define additional arguments, so the generic arguments boil down to what the type itself
                // defines.
                var asConstructor = methodBase as ConstructorInfo;
                genericsCount = asConstructor.DeclaringType.GetGenericArguments().Length;
            }
            else
            {
                genericsCount = methodBase.GetGenericArguments().Length;
            }

            outParams = new object[methodParams.Length + genericsCount];

            // Extension method; there's the "this object" parameter in the first position that we need to insert.
            if (isExtensionMethod)
            {
                outParams[0] = thisReference;
                out_param_i = 1;
                methodInfo_i = 1;
            }

            // Copy over generics
            while (out_param_i < genericsCount)
            {
                // If we got a .NET proxy then let's extract the type and copy that instead.
                // Yes, this is really meticulous and delicate.
                if(args[in_param_i] is PyDotNetClassProxy)
                {
                    var proxy = args[in_param_i] as PyDotNetClassProxy;
                    outParams[out_param_i] = proxy.DotNetType;
                }
                else
                {
                    outParams[out_param_i] = args[in_param_i];
                }
                out_param_i += 1;
                in_param_i += 1;
            }

            for (; out_param_i < (hasParamsField ? outParams.Length - 1 : outParams.Length); ++out_param_i, ++methodInfo_i)
            {
                var paramInfo = methodParams[methodInfo_i];
                if (paramInfo.ParameterType == typeof(IInterpreter))
                {
                    outParams[out_param_i] = Interpreter;
                }
                else if (paramInfo.ParameterType == typeof(FrameContext))
                {
                    outParams[out_param_i] = Context;
                }
                else if (paramInfo.ParameterType == typeof(IScheduler))
                {
                    outParams[out_param_i] = Scheduler;
                }
                else if(methodParams[methodInfo_i].HasDefaultValue)
                {
                    var overrideName = methodParams[methodInfo_i].Name;
                    if (overrides != null && overrides.ContainsKey(overrideName))
                    {
                        outParams[out_param_i] = PyNetConverter.Convert(overrides[overrideName], paramInfo.ParameterType);
                    }
                    else
                    {
                        outParams[out_param_i] = paramInfo.DefaultValue == null ? null : PyNetConverter.Convert(paramInfo.DefaultValue, paramInfo.ParameterType);
                    }
                }
                else 
                {
                    if (in_param_i < args.Length)
                    {
                        outParams[out_param_i] = PyNetConverter.Convert(args[in_param_i], paramInfo.ParameterType);
                        ++in_param_i;
                    }
                    else
                    {
                        throw new ArgumentException("Not enough arguments for " + methodBase.Name + " to satisfy the call");
                    }
                }
            }

            // If there's a params field and we don't have enough stuff to fill it, then we need to
            // give it a null or else we'll run into a TargetParameterCountException
            // If we *can* fill it in, we need to convert to the params array type.
            //
            if (hasParamsField)
            {
                if (in_param_i >= args.Length)
                {
                    outParams[outParams.Length - 1] = null;
                }
                else
                {
                    var elementType = methodParams[methodParams.Length - 1].ParameterType.GetElementType();
                    var paramsArray = Array.CreateInstance(elementType, args.Length - in_param_i);
                    for (int i = 0; i < paramsArray.Length; ++i)
                    {
                        paramsArray.SetValue(PyNetConverter.Convert(args[in_param_i + i], elementType), i);
                    }

                    outParams[outParams.Length - 1] = paramsArray;
                }
            }

            return outParams;
        }

        public object[] Inject(MethodBase methodBase, object[] args, object thisReference=null)
        {
            var methodParams = methodBase.GetParameters();

            // If there's a params field then we have to cram an array into there.
            bool hasParamsField = methodParams.Length >= 1 && methodParams[methodParams.Length - 1].IsDefined(typeof(ParamArrayAttribute), false);
            bool isExtensionMethod = methodBase.IsExtensionMethod();

            // Transform all arguments except for any in the params field.
            object[] outParams;
            int in_param_i = 0;     // Keep an eye on this for later to determine if we have stuff for a params field!
            int out_param_i = 0;
            int methodInfo_i = 0;

            outParams = new object[methodParams.Length];

            // Extension method; there's the "this object" parameter in the first position that we need to insert.
            if (isExtensionMethod)
            {
                outParams[0] = thisReference;
                out_param_i = 1;
                methodInfo_i = 1;
            }

            for (; out_param_i < (hasParamsField ? outParams.Length - 1 : outParams.Length); ++out_param_i, ++methodInfo_i)
            {
                var paramInfo = methodParams[methodInfo_i];
                if(paramInfo.ParameterType == typeof(IInterpreter))
                {
                    outParams[out_param_i] = Interpreter;
                }
                else if(paramInfo.ParameterType == typeof(FrameContext))
                {
                    outParams[out_param_i] = Context;
                }
                else if(paramInfo.ParameterType == typeof(IScheduler))
                {
                    outParams[out_param_i] = Scheduler;
                }
                else
                {
                    if (in_param_i < args.Length)
                    {
                        outParams[out_param_i] = PyNetConverter.Convert(args[in_param_i], paramInfo.ParameterType);
                        ++in_param_i;
                    }
                    else
                    {
                        // Might be an optional parameter. If so, we use the default:
                        if(paramInfo.HasDefaultValue)
                        {
                            outParams[out_param_i] = paramInfo.DefaultValue == null ? null : PyNetConverter.Convert(paramInfo.DefaultValue, paramInfo.ParameterType);
                        }
                        else
                        {
                            throw new ArgumentException("Not enough arguments for " + methodBase.Name + " to satisfy the call");
                        }
                    }
                }
            }

            // If there's a params field and we don't have enough stuff to fill it, then we need to
            // give it a null or else we'll run into a TargetParameterCountException
            // If we *can* fill it in, we need to convert to the params array type.
            //
            if (hasParamsField)
            {
                if (in_param_i >= args.Length)
                {
                    outParams[outParams.Length - 1] = null;
                }
                else
                {
                    var elementType = methodParams[methodParams.Length - 1].ParameterType.GetElementType();
                    var paramsArray = Array.CreateInstance(elementType, args.Length - in_param_i);
                    for (int i = 0; i < paramsArray.Length; ++i)
                    {
                        paramsArray.SetValue(PyNetConverter.Convert(args[in_param_i + i], elementType), i);
                    }

                    outParams[outParams.Length - 1] = paramsArray;
                }
            }

            return outParams;
        }
    }
}
