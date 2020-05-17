using CloacaInterpreter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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

        public static bool IsInjectedType(Type parameterType)
        {
            return parameterType == typeof(IInterpreter) ||
               parameterType == typeof(FrameContext) ||
               parameterType == typeof(IScheduler);
        }

        public object[] Inject(MethodBase methodBase, object[] args)
        {
            var methodParams = methodBase.GetParameters();

            // If there's a params field then we have to cram an array into there.
            bool hasParamsField = methodParams.Length >= 1 && methodParams[methodParams.Length - 1].IsDefined(typeof(ParamArrayAttribute), false);
            bool isExtensionMethod = methodBase.IsExtensionMethod();

            // Transform all arguments except for any in the params field.
            object[] outParams;
            int in_param_i = 0;     // Keep an eye on this for later to determine if we have stuff for a params field!
            int out_param_i = 0;

            // Extension method; there's the "this object" parameter in the first position that we need to gloss over.
            if (isExtensionMethod)
            {
                in_param_i = 1;
                outParams = new object[methodParams.Length-1];
            }
            else
            {
                outParams = new object[methodParams.Length];
            }

            for (; out_param_i < (hasParamsField ? outParams.Length - 1 : outParams.Length); ++out_param_i)
            {
                var paramInfo = methodParams[out_param_i];
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
                    outParams[out_param_i] = PyNetConverter.Convert(args[in_param_i], paramInfo.ParameterType);
                    ++in_param_i;
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
