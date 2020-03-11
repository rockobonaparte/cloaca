using LanguageImplementation.DataTypes;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LanguageImplementation
{
    /// <summary>
    /// Represents callable code outside of the scope of the interpreter.
    /// </summary>
    public class WrappedCodeObject : IPyCallable
    {
        public MethodBase[] MethodBases
        {
            get;
            private set;
        }

        private object instance;

        public string Name
        {
            get; protected set;
        }

        public override bool Equals(object other)
        {
            var asWco = other as WrappedCodeObject;
            if(asWco == null)
            {
                return false;
            }
            else
            {
                if(!(instance.Equals(asWco.instance) && Name.Equals(asWco.Name)))
                {
                    return false;
                }
                if(MethodBases.Length != asWco.MethodBases.Length)
                {
                    return false;
                }
                for(int i = 0; i < MethodBases.Length; ++i)
                {
                    if(!MethodBases[i].Equals(asWco.MethodBases[i]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public override int GetHashCode()
        {
            int hash = Name.GetHashCode();
            hash ^= instance.GetHashCode();
            foreach(var methodBase in MethodBases)
            {
                hash ^= methodBase.GetHashCode();
            }
            return hash;
        }

        #region Constructors
        public WrappedCodeObject(FrameContext context, string nameInsideInterpreter, MethodBase[] methodBases)
        {
            this.MethodBases = methodBases;
            Name = nameInsideInterpreter;
            instance = null;
        }

        public WrappedCodeObject(FrameContext context, string nameInsideInterpreter, MethodBase methodBase) : this(context, nameInsideInterpreter, new MethodBase[] { methodBase })
        {
        }

        public WrappedCodeObject(string nameInsideInterpreter, MethodBase[] methodBases) : this(null, nameInsideInterpreter, methodBases)
        {
        }

        public WrappedCodeObject(string nameInsideInterpreter, MethodBase methodBase) : this(null, nameInsideInterpreter, new MethodBase[] { methodBase })
        {
        }

        public WrappedCodeObject(FrameContext context, MethodBase[] methodBases)
        {
            this.MethodBases = methodBases;
            Name = methodBases[0].Name;
            instance = null;
        }

        public WrappedCodeObject(FrameContext context, MethodBase methodBase) : this(context, new MethodBase[] { methodBase })
        {
        }

        public WrappedCodeObject(MethodBase[] methodBases)
        {
            this.MethodBases = methodBases;
            Name = methodBases[0].Name;
            instance = null;
        }


        public WrappedCodeObject(MethodBase methodBase) : this(new MethodBase[] { methodBase })
        {
        }

        public WrappedCodeObject(FrameContext context, string nameInsideInterpreter, MethodBase[] methodBases, object instance)
        {
            this.MethodBases = methodBases;
            Name = nameInsideInterpreter;
            this.instance = instance;
        }

        public WrappedCodeObject(FrameContext context, string nameInsideInterpreter, MethodBase methodBase, object instance) : this(context, nameInsideInterpreter, new MethodBase[] { methodBase }, instance)
        {
        }

        public WrappedCodeObject(string nameInsideInterpreter, MethodBase[] methodBases, object instance) : this(null, nameInsideInterpreter, methodBases, instance)
        {
        }

        public WrappedCodeObject(string nameInsideInterpreter, MethodBase methodBase, object instance) : this(null, nameInsideInterpreter, methodBase, instance)
        {
        }

        public WrappedCodeObject(FrameContext context, MethodBase[] methodBases, object instance)
        {
            this.MethodBases = methodBases;
            Name = methodBases[0].Name;
            this.instance = instance;
        }

        public WrappedCodeObject(FrameContext context, MethodBase methodBase, object instance) : this(context, new MethodBase[] { methodBase }, instance)
        {
        }

        public WrappedCodeObject(MethodBase[] methodBases, object instance)
        {
            this.MethodBases = methodBases;
            Name = methodBases[0].Name;
            this.instance = instance;
        }

        public WrappedCodeObject(MethodBase methodBase, object instance) : this(new MethodBase[] { methodBase }, instance)
        {
        }

        #endregion Constructors
        public object GetObjectInstance()
        {
            return instance;
        }


        public WrappedCodeObject CloneForInstance(object instance)
        {
            return new WrappedCodeObject(Name, MethodBases, instance);
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
            var task = (Task)MethodBases[0].Invoke(instance, final_args);
            await task.ConfigureAwait(true);
            var result = ((dynamic)task).Result;
            return (object)result;
        }

        private bool AreCompatible(Type paramType, Type argType)
        {
            if (paramType.GetTypeInfo().IsAssignableFrom(argType.GetTypeInfo()))
            {
                return true; 
            }
            else
            {
                return PyNetConverter.CanConvert(argType, paramType);
            }
        }

        /// <summary>
        /// Search all the available MethodInfos and return one that most appropriately matches the given arguments. Note that
        /// injectable arguments will simply be skipped during consideration; it won't expect to find them in the given arguments.
        /// </summary>
        /// <param name="in_args">Arguments to call this method with</param>
        /// <returns>The right MethodInformation to use to invoke the method.</returns>
        private MethodBase findBestMethodMatch(object[] in_args)
        {
            foreach(var methodBase_itr in MethodBases)
            {
                var methodBase = methodBase_itr;        // Might plow over this with the monomorphized generic method.
                var args = in_args;             // We might tweak this if the arguments are for a generic.

                // Test for IsGenericMethodDefinition in case we're making internal calls to stuff like DefaultNew and the
                // generic arguments are already filled in.
                var genericsCount = methodBase_itr.IsGenericMethodDefinition ? methodBase_itr.GetGenericArguments().Length : 0;
                var parameters = methodBase_itr.GetParameters();
                bool found = true;

                // We change all the rules if this is a generic. We'll monomorphize the generic and use that information for
                // comparisons, so don't get to attached to the args and parameters defined above.
                if (methodBase_itr.IsGenericMethodDefinition)
                {
                    var asMethodInfo = methodBase_itr as MethodInfo;
                    if (asMethodInfo == null)
                    {
                        throw new Exception("Cannot find matching methods for " + methodBase_itr.Name + " since it is not a MethodInfo. For example, we don't support generic constructors (does .NET even support it?)");
                    }
                    
                    var genericTypes = new Type[genericsCount];
                    args = new object[in_args.Length - genericsCount];
                    if (in_args.Length - genericTypes.Length < parameters.Length)
                    {
                        // Between generic args and arguments we got, we can't fill in for this one so don't even try.
                        continue;
                    }

                    // Can't Array.Copy the actual objects to an array of their types.
                    for(int i = 0; i < genericsCount; ++i)
                    {                        
                        if(in_args[i] is PyDotNetClassProxy)
                        {
                            var asProxy = (PyDotNetClassProxy)in_args[i];
                            genericTypes[i] = (Type) asProxy.__getattribute__(PyDotNetClassProxy.__dotnettype__);
                        }
                        else
                        {
                            genericTypes[i] = in_args[i].GetType();
                        }                        
                    }
                    Array.Copy(in_args, genericsCount, args, 0, in_args.Length - genericsCount);

                    var monomorphedMethod = asMethodInfo.MakeGenericMethod(genericTypes);
                    methodBase = monomorphedMethod;
                    parameters = methodBase.GetParameters();
                }


                // Use the parameters as the base for testing. We'll skip any of the parameters that are injectable.
                for (int params_i = 0, args_i = 0; args_i < args.Length || params_i < args.Length;)
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
                    return methodBase;
                }
            }

            // Broke through to here: we couldn't find a match at all for the given arguments!
            var errorMessage = new StringBuilder("No .NET method found to match the given arguments: ");
            if(in_args.Length == 0)
            {
                errorMessage.Append("(no arguments)");
            }
            else
            {
                errorMessage.Append(String.Join(", ", from x in in_args select x.GetType().Name));
            }

            throw new Exception(errorMessage.ToString());
        }

        public Task<object> Call(IInterpreter interpreter, FrameContext context, object[] args)
        {
            var methodBase = findBestMethodMatch(args);

            // Strip generic arguments (if any).
            // Note this is kind of hacky! We get a monomorphized generic method back whether or not
            // we started out that way already. Current hack is to see if we have more arguments than
            // the method needs. If we do, then we strip the excess in front since they were used to
            // monomorphize the generic.
            var noGenericArgs = args;
            if(methodBase.IsGenericMethod && args.Length > methodBase.GetParameters().Length)
            {
                var numGenerics = methodBase.GetGenericArguments().Length;
                noGenericArgs = new object[args.Length - numGenerics];
                Array.Copy(args, numGenerics, noGenericArgs, 0, args.Length - numGenerics);
            }

            // Inject internal types, convert .NET/Cloaca types.
            // Unit tests like to come in with a null interpreter so we have to test for it.
            var injector = new Injector(interpreter, context, interpreter != null ? interpreter.Scheduler : null);
            var final_args = injector.Inject(methodBase, noGenericArgs);

            // Little convenience here. We'll convert a non-task Task<object> type to a task.
            var asMethodInfo = methodBase as MethodInfo;
            if (asMethodInfo != null && asMethodInfo.ReturnType.IsGenericType && asMethodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                // Task<object> is straightforward and we can just return it. Other return types need to go through
                // our helper.
                if (asMethodInfo.ReturnType == typeof(Task<object>))
                {
                    return (Task<object>)methodBase.Invoke(instance, final_args);
                }
                else
                {
                    return InvokeAsTaskObject(final_args);
                }
            }
            else
            {
                var asConstructor = methodBase as ConstructorInfo;
                if (asConstructor != null)
                {
                    return Task.FromResult(asConstructor.Invoke(final_args));
                }
                else
                {
                    return Task.FromResult(methodBase.Invoke(instance, final_args));
                }
            }
        }

    }
}
