using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public Injector(IInterpreter interpreter, FrameContext context)
        {
            Prepare(interpreter, context);
        }

        public void Prepare(IInterpreter interpreter, FrameContext context)
        {
            Interpreter = interpreter;
            Context = context;
        }

        public object[] Inject(MethodInfo methodInfo, object[] args)
        {
            var methodParams = methodInfo.GetParameters();

            // Let's assume usually we don't need to actually do anything. In such cases, return args since it's already okay.
            bool mustInject = false;
            
            // If there's a params field then we have to cram an array into there.
            bool hasParamsField = methodParams.Length >= 1 && methodParams[methodParams.Length - 1].IsDefined(typeof(ParamArrayAttribute), false);
            mustInject |= hasParamsField;
            if (!mustInject)
            {
                // Okay, let's see if we need to inject an interpreter or frame context.
                foreach(var paramInfo in methodParams)
                {
                    if(paramInfo.ParameterType == typeof(IInterpreter) ||
                       paramInfo.ParameterType == typeof(FrameContext))
                    {
                        mustInject = true;
                        break;
                    }
                }
            }

            if(!mustInject)
            {
                return args;
            }

            // If we're here, then it's injection time!

            // Transform all arguments except for any in the params field.
            object[] outParams = new object[methodParams.Length];
            int in_param_i = 0;     // Keep an eye on this for later to determine if we have stuff for a params field!
            for(int out_param_i = 0; out_param_i < (hasParamsField ? methodParams.Length - 1 : methodParams.Length); ++out_param_i)
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
                else
                {
                    outParams[out_param_i] = args[in_param_i];
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
                        paramsArray.SetValue(args[in_param_i + i], i);
                    }

                    outParams[outParams.Length - 1] = paramsArray;
                }
            }

            return outParams;
        }
    }
}
