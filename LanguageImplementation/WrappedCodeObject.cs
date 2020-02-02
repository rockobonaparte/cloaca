using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LanguageImplementation.DataTypes;
using System.Numerics;

namespace LanguageImplementation
{
    /// <summary>
    /// Represents callable code outside of the scope of the interpreter.
    /// </summary>
    public class WrappedCodeObject : IPyCallable
    {
        public MethodInfo[] MethodInfos
        {
            get;
            private set;
        }

        private object instance;

        public string Name
        {
            get; protected set;
        }

        /// <summary>
        /// Assuming that the payload is Task<T>, this will convert it to a Task<object>. We need this tedious
        /// step because C# does not support a direct conversion. Instead, we have to write a helper to invoke
        /// the coroutine, harvest its result, and convert that to an object. This inner coroutine is wrapped
        /// as the Task<object> we wanted in the first place.
        /// </summary>
        /// <param name="final_args">The final arguments to give to the method when invoking it. This is prepared
        /// by the injector in Call().</param>
        /// <returns>A Task<object> where the returned object is casted from some other type that was the original
        /// method's return type.</returns>
        private async Task<object> InvokeAsTaskObject(object[] final_args)
        {
            var task = (Task)MethodInfos[0].Invoke(instance, final_args);
            await task.ConfigureAwait(true);
            var result = ((dynamic)task).Result;
            return (object)result;
        }

        // This will lead to some problems if some asshat decides to subclass the base types. We won't be able to look it up this way.
        // At this point, I'm willing to dismiss that. Famous last words.
        private static Dictionary<Tuple<Type, Type>, Func<object, object>> PyNetConverters = new Dictionary<Tuple<Type, Type>, Func<object, object>>
        {
            { new Tuple<Type, Type>(typeof(int), typeof(PyInteger)), (as_int) => { return new PyInteger((int)as_int); } },
            { new Tuple<Type, Type>(typeof(short), typeof(PyInteger)), (as_short) => { return new PyInteger((short)as_short); } },
            { new Tuple<Type, Type>(typeof(long), typeof(PyInteger)), (as_long) => { return new PyInteger((long)as_long); } },
            { new Tuple<Type, Type>(typeof(BigInteger), typeof(PyInteger)), (as_bi) => { return new PyInteger((BigInteger)as_bi); } },
            { new Tuple<Type, Type>(typeof(PyInteger), typeof(int)), (as_pi) => { return (int) ((PyInteger)as_pi).number; } },
            { new Tuple<Type, Type>(typeof(PyInteger), typeof(short)), (as_pi) => { return (short) ((PyInteger)as_pi).number; } },
            { new Tuple<Type, Type>(typeof(PyInteger), typeof(long)), (as_pi) => { return (long) ((PyInteger)as_pi).number; } },
            { new Tuple<Type, Type>(typeof(PyInteger), typeof(BigInteger)), (as_pi) => { return (BigInteger) ((PyInteger)as_pi).number; } },
        };

        private object[] transformCompatibleArgs(ParameterInfo[] parameters, object[] args)
        {
            var returnedArgs = new object[parameters.Length];
            for(int arg_i = 0; arg_i < args.Length; ++arg_i)
            {
                if(args[arg_i] == null)
                {
                    returnedArgs[arg_i] = args[arg_i];
                    continue;
                }
                
                var lookup = new Tuple<Type, Type>(args[arg_i].GetType(), parameters[arg_i].ParameterType);
                if (PyNetConverters.ContainsKey(lookup))
                {
                    returnedArgs[arg_i] = PyNetConverters[lookup].Invoke(args[arg_i]);
                }
                else if (parameters[arg_i].IsDefined(typeof(ParamArrayAttribute), false))
                {
                    // That params field strikes again! We're breaking up a lot of these lookups to more easily see what's going on.
                    // Params field is last param so we'll be doing seemingly reckless things running arg_i out here.
                    var params_arg_array = (object[])args[arg_i];
                    var paramsArray = Array.CreateInstance(parameters[arg_i].ParameterType.GetElementType(), params_arg_array.Length);

                    // We'll cache our converter on the assumption that most arguments to the params fields are the same.
                    Func<object, object> converter = null;
                    var baseArrayType = parameters[arg_i].ParameterType.GetElementType();
                    var paramLookup = new Tuple<Type, Type>(args[arg_i].GetType(), baseArrayType);

                    if (PyNetConverters.ContainsKey(paramLookup))
                    {
                        converter = PyNetConverters[paramLookup];
                    }

                    for (int params_arg_i = 0; params_arg_i < paramsArray.Length; ++params_arg_i, ++arg_i)
                    {
                        // Invalidate cache
                        if (params_arg_array[params_arg_i].GetType() != paramLookup.Item1)
                        {
                            paramLookup = new Tuple<Type, Type>(params_arg_array[params_arg_i].GetType(), baseArrayType);
                            if (PyNetConverters.ContainsKey(paramLookup))
                            {
                                converter = PyNetConverters[paramLookup];
                            }
                            else
                            {
                                converter = null;
                            }
                        }

                        if (converter != null)
                        {
                            paramsArray.SetValue(converter.Invoke(params_arg_array[params_arg_i]), params_arg_i);
                        }
                        else
                        {
                            paramsArray.SetValue(params_arg_array[params_arg_i], params_arg_i);
                        }
                    }

                    // Params field is always last field.
                    returnedArgs[returnedArgs.Length - 1] = paramsArray;
                }
                else
                {
                    returnedArgs[arg_i] = args[arg_i];
                }
            }
            return returnedArgs;
           
        }

        private bool AreCompatible(Type paramType, Type argType)
        {
            if (paramType.GetTypeInfo().IsAssignableFrom(argType.GetTypeInfo()))
            {
                return true; 
            }
            else
            {
                return PyNetConverters.ContainsKey(new Tuple<Type, Type>(argType, paramType));
            }
        }

        /// <summary>
        /// Search all the available MethodInfos and return one that most appropriately matches the given arguments. Note that
        /// injectable arguments will simply be skipped during consideration; it won't expect to find them in the given arguments.
        /// </summary>
        /// <param name="args">Arguments to call this method with</param>
        /// <returns>The right MethodInformation to use to invoke the method.</returns>
        private MethodInfo findBestMethodMatch(object[] args)
        {
            foreach(var methodInfo in MethodInfos)
            {
                var parameters = methodInfo.GetParameters();
                bool found = true;

                // Use the parameters as the base for testing. We'll skip any of the parameters that are injectable.
                for(int params_i = 0, args_i = 0; args_i < args.Length || params_i < args.Length;)
                {
                    while(params_i < parameters.Length && Injector.IsInjectedType(parameters[params_i].ParameterType))
                    {
                        params_i += 1;
                    }

                    // Type check
                    if(args_i < args.Length && params_i < parameters.Length &&
                        !AreCompatible(parameters[params_i].ParameterType, args[args_i].GetType()))
                    {
                        // If the parameter is optional ("params") then it'll be optional and be an array. If the type we're trying to feed it is
                        // a single element then we're okay.
                        if(parameters[params_i].IsDefined(typeof(ParamArrayAttribute), false) && AreCompatible(parameters[params_i].ParameterType.GetElementType(), args[args_i].GetType()))
                        {
                            // Still alive! The args being given exceed the number of parameters defined, but they're fitting just fine into the params
                            // field!
                            args_i += 1;
                            continue;
                        }

                        found = false;
                        break;
                    }
                    // Ran out of arguments for our parameters... if they aren't optional at this point ("params")
                    else if (args_i >= args.Length && params_i < parameters.Length && !parameters[params_i].IsDefined(typeof(ParamArrayAttribute), false))
                    {
                        found = false;
                        break;
                    }
                    // Flip case: ran out of parameters for our arguments!
                    else if (args_i < args.Length && params_i >= parameters.Length)
                    {
                        found = false;
                        break;
                    }
                    else
                    {
                        args_i += 1;
                        params_i += 1;
                    }
                }
                if(found)
                {
                    return methodInfo;
                }
            }

            // Broke through to here: we couldn't find a match at all for the given arguments!
            var errorMessage = new StringBuilder("No .NET method found to match the given arguments: ");
            if(args.Length == 0)
            {
                errorMessage.Append("(no arguments)");
            }
            else
            {
                errorMessage.Append(String.Join(", ", from x in args select x.GetType().Name));
            }

            throw new Exception(errorMessage.ToString());
        }

        public Task<object> Call(IInterpreter interpreter, FrameContext context, object[] args)
        {
            var methodInfo = findBestMethodMatch(args);
            var injector = new Injector(interpreter, context, interpreter.Scheduler);
            var injected_args = injector.Inject(methodInfo, args);
            var final_args = transformCompatibleArgs(methodInfo.GetParameters(), injected_args);
            //var boxed_args = transformCompatibleArgs(methodInfo.GetParameters(), args);
            //var final_args = injector.Inject(methodInfo, boxed_args);

            // Little convenience here. We'll convert a non-task Task<object> type to a task.
            if (MethodInfos[0].ReturnType.IsGenericType && MethodInfos[0].ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                // Task<object> is straightforward and we can just return it. Other return types need to go through
                // our helper.
                if (MethodInfos[0].ReturnType == typeof(Task<object>))
                {
                    return (Task<object>)methodInfo.Invoke(instance, final_args);
                }
                else
                {
                    return InvokeAsTaskObject(final_args);
                }
            }
            else
            {
                return Task.FromResult(methodInfo.Invoke(instance, final_args));
            }
        }

        #region Constructors
        public WrappedCodeObject(FrameContext context, string nameInsideInterpreter, MethodInfo[] methodInfos)
        {
            this.MethodInfos = methodInfos;
            Name = nameInsideInterpreter;
            instance = null;
        }

        public WrappedCodeObject(FrameContext context, string nameInsideInterpreter, MethodInfo methodInfo) : this(context, nameInsideInterpreter, new MethodInfo[] { methodInfo })
        {
        }

        public WrappedCodeObject(string nameInsideInterpreter, MethodInfo[] methodInfos) : this(null, nameInsideInterpreter, methodInfos)
        {
        }

        public WrappedCodeObject(string nameInsideInterpreter, MethodInfo methodInfo) : this(null, nameInsideInterpreter, new MethodInfo[] { methodInfo })
        {
        }

        public WrappedCodeObject(FrameContext context, MethodInfo[] methodInfos)
        {
            this.MethodInfos = methodInfos;
            Name = methodInfos[0].Name;
            instance = null;
        }

        public WrappedCodeObject(FrameContext context, MethodInfo methodInfo) : this(context, new MethodInfo[] { methodInfo })
        {
        }

        public WrappedCodeObject(MethodInfo[] methodInfos)
        {
            this.MethodInfos = methodInfos;
            Name = methodInfos[0].Name;
            instance = null;
        }


        public WrappedCodeObject(MethodInfo methodInfo) : this(new MethodInfo[] { methodInfo })
        {
        }

        public WrappedCodeObject(FrameContext context, string nameInsideInterpreter, MethodInfo[] methodInfos, object instance)
        {
            this.MethodInfos = methodInfos;
            Name = nameInsideInterpreter;
            this.instance = instance;
        }

        public WrappedCodeObject(FrameContext context, string nameInsideInterpreter, MethodInfo methodInfo, object instance) : this(context, nameInsideInterpreter, new MethodInfo[] { methodInfo }, instance)
        {
        }

        public WrappedCodeObject(string nameInsideInterpreter, MethodInfo[] methodInfos, object instance) : this(null, nameInsideInterpreter, methodInfos, instance)
        {
        }

        public WrappedCodeObject(string nameInsideInterpreter, MethodInfo methodInfo, object instance) : this(null, nameInsideInterpreter, methodInfo, instance)
        {
        }

        public WrappedCodeObject(FrameContext context, MethodInfo[] methodInfos, object instance)
        {
            this.MethodInfos = methodInfos;
            Name = methodInfos[0].Name;
            this.instance = instance;
        }

        public WrappedCodeObject(FrameContext context, MethodInfo methodInfo, object instance) : this(context, new MethodInfo[] { methodInfo }, instance)
        {
        }

        public WrappedCodeObject(MethodInfo[] methodInfos, object instance)
        {
            this.MethodInfos = methodInfos;
            Name = methodInfos[0].Name;
            this.instance = instance;
        }

        public WrappedCodeObject(MethodInfo methodInfo, object instance) : this(new MethodInfo[] { methodInfo }, instance)
        {
        }

        #endregion Constructors
    }
}
