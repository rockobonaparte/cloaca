using System.Collections.Generic;
using System.Linq;

using LanguageImplementation.DataTypes;
using LanguageImplementation;

namespace CloacaInterpreter
{
    public class ArgParamMatcher
    {
        // TODO [KEYWORD-POSITIONAL-ONLY] Implement positional-only (/) and keyword-only (*) arguments
        // TODO [ARGPARAMMATCHER ERRORS] Generate errors when input arguments don't match requirements of code object.
        // TODO [**kwargs] Support kwargs
        // If this gets too unweildy in its current form, then consider something like a state machine. Once you 
        // are done processing one type of argument, you don't process any more of them, so there is some state
        // transition in this code. As written, it's obscured and goofy. However, the state machine might get pretty
        // gross. Since we can move forward to any of the subsequent states based on circumstance, the earlier
        // transitions would have a lot more logic in them to determine where to go next, and I think a lot of that
        // logic would repeat.
        public static object[] Resolve(CodeObject co, object[] inArgs, Dictionary<string, object> keywords = null)
        {
            bool hasVargs = (co.Flags & CodeObject.CO_FLAGS_VARGS) > 0;
            var defaultsStart = co.ArgCount - co.Defaults.Count;

            // The number of actual output arguments is not given straightforwardly. ArgCount gets use
            // the first positional and keyword arguments, but then we might have *args and **kwargs, which
            // are designated by flags.
            var outArgsLength = co.ArgCount;

            var num_vargs = inArgs.Length - co.ArgCount;
            object[] vargs = null;
            if (hasVargs)
            {
                if (num_vargs > 0)
                {
                    vargs = new object[num_vargs];
                }
                else
                {
                    vargs = new object[0];
                }
                outArgsLength += 1;
            }

            var outArgs = new object[outArgsLength];

            int inArg = 0;
            for (int outArgIdx = 0; outArgIdx < outArgs.Length; ++outArgIdx)
            {
                if (inArg >= inArgs.Length)
                {
                    // Out of inputs, now lean on defaults arguments
                    var varName = co.VarNames[inArg];
                    if (keywords != null && keywords.ContainsKey(varName))
                    {
                        // Keyword argument
                        outArgs[outArgIdx] = keywords[varName];
                    }
                    else
                    {
                        // Use the default
                        if (hasVargs && inArg == co.ArgCount)
                        {
                            // If it's a variable argument (*args) then the default is an empty tuple.
                            outArgs[outArgIdx] = PyTuple.Create(vargs);
                        }
                        else
                        {
                            outArgs[outArgIdx] = co.Defaults[inArg - defaultsStart];
                        }
                    }
                    inArg += 1;
                }
                else if (hasVargs && inArg >= co.ArgCount)
                {
                    // Variable arguments (*args)
                    while (inArg < inArgs.Length)
                    {
                        vargs[inArg - co.ArgCount] = inArgs[inArg];
                        ++inArg;
                    }
                    outArgs[outArgIdx] = PyTuple.Create(vargs);
                }
                else
                {
                    // Conventional, positional argument
                    outArgs[outArgIdx] = inArgs[inArg];
                    inArg += 1;
                }
            }

            return outArgs;
        }
    }
}