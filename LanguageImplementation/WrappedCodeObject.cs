using LanguageImplementation.DataTypes;
using System;
using System.Collections.Generic;
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

        public static void AssertMethodBaseNotNull(MethodBase methodBase)
        {
            if(methodBase == null)
            {
                throw new Exception("MethodBase given to WrappedCodeObject was null. This implies the reflection call to get the MethodInfo through reflection did not find the method in the first place.");
            }
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
            AssertMethodBaseNotNull(methodBase);
        }

        public WrappedCodeObject(string nameInsideInterpreter, MethodBase[] methodBases) : this(null, nameInsideInterpreter, methodBases)
        {
        }

        public WrappedCodeObject(string nameInsideInterpreter, MethodBase methodBase) : this(null, nameInsideInterpreter, new MethodBase[] { methodBase })
        {
            AssertMethodBaseNotNull(methodBase);
        }

        public WrappedCodeObject(FrameContext context, MethodBase[] methodBases)
        {
            this.MethodBases = methodBases;
            Name = methodBases[0].Name;
            instance = null;
        }

        public WrappedCodeObject(FrameContext context, MethodBase methodBase) : this(context, new MethodBase[] { methodBase })
        {
            AssertMethodBaseNotNull(methodBase);
        }

        public WrappedCodeObject(MethodBase[] methodBases)
        {
            this.MethodBases = methodBases;
            Name = methodBases[0].Name;
            instance = null;
        }


        public WrappedCodeObject(MethodBase methodBase) : this(new MethodBase[] { methodBase })
        {
            AssertMethodBaseNotNull(methodBase);
        }

        public WrappedCodeObject(FrameContext context, string nameInsideInterpreter, MethodBase[] methodBases, object instance)
        {
            this.MethodBases = methodBases;
            Name = nameInsideInterpreter;
            this.instance = instance;
        }

        public WrappedCodeObject(FrameContext context, string nameInsideInterpreter, MethodBase methodBase, object instance) : this(context, nameInsideInterpreter, new MethodBase[] { methodBase }, instance)
        {
            AssertMethodBaseNotNull(methodBase);
        }

        public WrappedCodeObject(string nameInsideInterpreter, MethodBase[] methodBases, object instance) : this(null, nameInsideInterpreter, methodBases, instance)
        {
        }

        public WrappedCodeObject(string nameInsideInterpreter, MethodBase methodBase, object instance) : this(null, nameInsideInterpreter, methodBase, instance)
        {
            AssertMethodBaseNotNull(methodBase);
        }

        public WrappedCodeObject(FrameContext context, MethodBase[] methodBases, object instance)
        {
            this.MethodBases = methodBases;
            Name = methodBases[0].Name;
            this.instance = instance;
        }

        public WrappedCodeObject(FrameContext context, MethodBase methodBase, object instance) : this(context, new MethodBase[] { methodBase }, instance)
        {
            AssertMethodBaseNotNull(methodBase);
        }

        public WrappedCodeObject(MethodBase[] methodBases, object instance)
        {
            this.MethodBases = methodBases;
            Name = methodBases[0].Name;
            this.instance = instance;
        }

        public WrappedCodeObject(MethodBase methodBase, object instance) : this(new MethodBase[] { methodBase }, instance)
        {
            AssertMethodBaseNotNull(methodBase);
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

        // Known issue here: We call this twice. First, we call it when trying to resolve arguments. At that point, we will inject arguments.
        // We will then call it with the injector arguments. However, this will follow resolution since we thought we were going to skip injected
        // arguments when making a match. However, they're all specified! So we shouldn't.
        //
        // This creates a lot of logic bombs around detecting injected types. However, the real solution some day is to just call all this once.
        // This would mean moving Resolve() next to Call() in some fashion. This requires smoothing out the API between WrappedCodeObject and
        // regular Pythonic CodeObjects.

        /// <summary>
        /// Search all the available MethodInfos and return one that most appropriately matches the given arguments. Note that
        /// injectable arguments will simply be skipped during consideration; it won't expect to find them in the given arguments.
        /// </summary>
        /// <param name="in_args">Arguments to call this method with</param>
        /// <returns>The right MethodInformation to use to invoke the method.</returns>
        public MethodBase FindBestMethodMatch(object[] in_args)
        {
            foreach(var methodBase_itr in MethodBases)
            {
                var methodBase = methodBase_itr;        // Might plow over this with the monomorphized generic method.
                var args = in_args;                     // We might tweak this if the arguments are for a generic.

                // Test for generic parameters in case we're making internal calls to stuff like DefaultNew and the
                // generic arguments are already filled in.
                int genericsCount = 0;
                if(methodBase.ContainsGenericParameters)
                {
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
                }

                var parameters = methodBase.GetParameters();
                bool found = true;
                bool hasParamsField = false;
                if(parameters.Length > 0 && parameters[parameters.Length - 1].IsDefined(typeof(ParamArrayAttribute), false))
                {
                    hasParamsField = true;
                }

                // We change all the rules if this is a generic. We'll monomorphize the generic and use that information for
                // comparisons, so don't get to attached to the args and parameters defined above.
                if (methodBase.ContainsGenericParameters)
                {                    
                    var genericTypes = new Type[genericsCount];
                    args = new object[in_args.Length - genericsCount];

                    // If it's an extension method, then ignore the this Class parameter in our calculation.
                    int first_param = 0;
                    int actualParameterCount = parameters.Length;
                    if(methodBase.IsExtensionMethod())
                    {
                        actualParameterCount -= 1;
                        first_param += 1;
                    }

                    // Some of these parameters might be injectable, and we can discount them for the sake of matching.
                    for(int i = 0; i < (hasParamsField ? parameters.Length - 1 : parameters.Length); ++i)
                    {
                        if(Injector.IsInjectedParameterType(parameters[i].ParameterType))
                        {
                            // Logic bomb. Skip injected parameters UNLESS we have defined them in our input arguments.
                            if(i >= in_args.Length || !Injector.IsInjectedParameterType(in_args[i].GetType()))
                            {
                                actualParameterCount -= 1;
                            }
                        }
                    }

                    if (in_args.Length - genericTypes.Length < actualParameterCount)
                    {                    
                        // Between generic args and arguments we got, we're coming up short on arguments.
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
                            genericTypes[i] = (Type) in_args[i];
                        }
                    }
                    Array.Copy(in_args, genericsCount, args, 0, in_args.Length - genericsCount);

                    parameters = methodBase.GetParameters();
                    var asMethodInfo = methodBase as MethodInfo;
                    if (asMethodInfo != null)
                    {
                        // We don't have to follow this path for constructors because the generic parameters are part of the type,
                        // not the call itself. On the other hand, we'll suffer that later when invoking it...
                        var monomorphedMethod = asMethodInfo.MakeGenericMethod(genericTypes);
                        methodBase = monomorphedMethod;
                        parameters = methodBase.GetParameters();
                    }
                }

                // Set up parameter checking and casting loop. If this is an extension method then skip the first parameter
                // because that's just the object to invoke.
                int params_i = 0;
                int args_i = 0;
                if(methodBase.IsExtensionMethod())
                {
                    params_i = 1;
                }

                // Use the parameters as the base for testing. We'll skip any of the parameters that are injectable.
                while(args_i < args.Length || params_i < parameters.Length)
                {
                    while(params_i < parameters.Length && Injector.IsInjectedParameterType(parameters[params_i].ParameterType))
                    {
                        params_i += 1;

                        // Injector logic bomb: if the type was already injected, then skip it in our input arguments too.
                        // ....BUT, don't do this if we either don't have an argument for this position or it isn't something injected.
                        if(args_i < in_args.Length && Injector.IsInjectedArgType(in_args[args_i].GetType()))
                        {
                            args_i += 1;
                        }
                    }

                    // Type check
                    if(args_i < args.Length && params_i < parameters.Length &&
                        !AreCompatible(parameters[params_i].ParameterType, args[args_i].GetType()))
                    {
                        // If the parameter is optional ("params") then it'll be optional and be an array. If the type we're trying to feed it is
                        // a single element then we're okay.
                        if(hasParamsField && AreCompatible(parameters[parameters.Length-1].ParameterType.GetElementType(), args[args_i].GetType()))
                        {
                            // Still alive! The args being given exceed the number of parameters defined, but they're fitting just fine into the params
                            // field!
                            args_i += 1;
                            continue;
                        }

                        found = false;
                        break;
                    }
                    // Ran out of arguments for our parameters... if they aren't optional at this point ("params" or string foo="bar")
                    else if (args_i >= args.Length && params_i < parameters.Length && !parameters[params_i].IsDefined(typeof(ParamArrayAttribute), false) && !parameters[params_i].IsOptional)
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
            var errorMessage = new StringBuilder("No .NET method found for " + Name + " to match the given arguments: ");
            if(in_args.Length == 0)
            {
                errorMessage.Append("(no arguments)");
            }
            else
            {
                errorMessage.Append(String.Join(", ", from x in in_args select x?.GetType().Name));
            }

            throw new Exception(errorMessage.ToString());
        }

        public Task<object> Call(IInterpreter interpreter, FrameContext context, object[] inArgs,
            Dictionary<string, object> defaultOverrides = null,
            KwargsDict kwargsDict = null)
        {

            var methodBase = FindBestMethodMatch(inArgs);
            var injector = new Injector(interpreter, context, interpreter.Scheduler);
            object[] resolvedArgs = injector.Inject2(methodBase, inArgs, overrides: defaultOverrides);

            // Strip generic arguments (if any).
            // Note this is kind of hacky! We get a monomorphized generic method back whether or not
            // we started out that way already. Current hack is to see if we have more arguments than
            // the method needs. If we do, then we strip the excess in front since they were used to
            // monomorphize the generic.
            int numGenerics = 0;
            var noGenericArgs = resolvedArgs;
            int actualParametersLength = methodBase.IsExtensionMethod() ? methodBase.GetParameters().Length - 1 : methodBase.GetParameters().Length;
            if ((methodBase.ContainsGenericParameters || methodBase.IsGenericMethod) && resolvedArgs.Length > actualParametersLength)
            {
                if (methodBase.IsConstructor)
                {
                    // If this is a constructor, then the class we're instantiating itself might be generic. The constructor
                    // can't legally define additional arguments, so the generic arguments boil down to what the type itself
                    // defines.
                    var asConstructor = methodBase as ConstructorInfo;
                    numGenerics = asConstructor.DeclaringType.GetGenericArguments().Length;
                }
                else
                {
                    numGenerics = methodBase.GetGenericArguments().Length;
                    noGenericArgs = new object[resolvedArgs.Length - numGenerics];
                }
            }
            Array.Copy(resolvedArgs, numGenerics, noGenericArgs, 0, resolvedArgs.Length - numGenerics);

            // Inject internal types, convert .NET/Cloaca types.
            // Unit tests like to come in with a null interpreter so we have to test for it.
            //var injector = new Injector(interpreter, context, interpreter != null ? interpreter.Scheduler : null);
            //var injected_args = injector.Inject(methodBase, noGenericArgs, instance);
            //object[] final_args = injected_args;
            object[] final_args = resolvedArgs;

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
                    // Special handling for generic constructors. The generic arguments for generic constructors are part of the type,
                    // not the constructor.
                    if(asConstructor.ContainsGenericParameters)
                    {
                        Type[] generics = new Type[numGenerics];
                        var constructorParamIns = methodBase.GetParameters();
                        Type[] constructorInTypes = new Type[constructorParamIns.Length];
                        for(int param_i = 0; param_i < constructorInTypes.Length; ++param_i)
                        {
                            constructorInTypes[param_i] = constructorParamIns[param_i].ParameterType;
                        }
                        Array.Copy(resolvedArgs, 0, generics, 0, numGenerics);
                        Type monomorphedConstructor = asConstructor.DeclaringType.MakeGenericType(generics);
                        asConstructor = monomorphedConstructor.GetConstructor(constructorInTypes);
                    }
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
