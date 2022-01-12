using System;
using System.Collections.Generic;
using System.Text;

using LanguageImplementation.DataTypes;

namespace LanguageImplementation
{
    public enum ArgParamState
    {
        Initial,
        Positional,
        KeywordOverride,
        KeywordOrDefault,
        Variable,
        KeywordOnly,
        Finished,
    }

    public class ArgParamMatcher
    {
        // TODO [KEYWORD-POSITIONAL-ONLY] Implement positional-only (/) and keyword-only (*) arguments
        // TODO [ARGPARAMMATCHER ERRORS] Generate errors when input arguments don't match requirements of code object.
        // TODO [**kwargs] Support kwargs
        public static object[] Resolve(CodeObject co, object[] inArgs, Dictionary<string, object> keywords = null)
        {
            // This state machine model is definitely more verbose but it's much easier to maintain because:
            // 1. We will loiter in only one section at a time so we can mentally compartmentalize what's happening.
            // 2. We can be very explicit about what happens next.
            var state = ArgParamState.Initial;
            int inArg_i = 0;
            int outArg_i = 0;
            int default_i = 0;

            object[] outArgs = new object[co.ArgCount + co.KWOnlyArgCount + (co.HasVargs ? 1 : 0)];
            bool hasDefaults = co.Defaults.Count > 0;
            bool hasKwOnlyArgs = co.KWOnlyArgCount > 0;
            bool hasKeywords = keywords is null ? false : true;

            // Determine our true first state. Initial is just symbolic.
            if (co.ArgCount > 0)
            {
                state = ArgParamState.Positional;
                if (inArgs.Length == 0)
                {
                    if (hasDefaults || hasKeywords)
                    {
                        state = ArgParamState.KeywordOverride;
                    }
                    else if (co.HasVargs)
                    {
                        state = ArgParamState.Variable;
                    }
                    else if (hasKwOnlyArgs)
                    {
                        state = ArgParamState.KeywordOnly;
                    }
                    else
                    {
                        state = ArgParamState.Finished;
                    }
                }
            }
            else
            {
                if(co.HasVargs)
                {
                    state = ArgParamState.Variable;
                }
                else if(hasKwOnlyArgs)
                {
                    state = ArgParamState.KeywordOnly;
                }
                else
                {
                    state = ArgParamState.Finished;
                }
            }

            if(state == ArgParamState.Initial)
            {
                throw new InvalidOperationException("Arg param state was still in initial state after checking entire environment.");
            }

            while (state != ArgParamState.Finished)
            {
                switch (state)
                {
                    case ArgParamState.Positional:
                        while(inArg_i < inArgs.Length && inArg_i < co.ArgCount)
                        {
                            outArgs[outArg_i] = inArgs[inArg_i];
                            outArg_i += 1;
                            inArg_i += 1;
                        }

                        if(inArg_i < co.ArgCount)
                        {
                            if (hasKeywords || hasDefaults)
                            {
                                state = ArgParamState.KeywordOverride;
                            }
                            else if (co.HasVargs)
                            {
                                state = ArgParamState.Variable;
                            }
                            else
                            {
                                // TODO: [PARAM MATCHER ERRORS] Cast to actual TypeError
                                throw new Exception("TypeError: " + co.Name + " takes " + co.ArgCount + " position arguments but " + inArgs.Length + " were given");
                            }
                        }
                        else if(co.HasVargs)
                        {
                            state = ArgParamState.Variable;
                        }
                        else if(hasKwOnlyArgs)
                        {
                            state = ArgParamState.KeywordOnly;
                        }
                        else
                        {
                            state = ArgParamState.Finished;
                        }
                        break;
                    case ArgParamState.KeywordOverride:
                        {
                            List<string> missingOverrides = null;
                            while (inArg_i < co.ArgCount - co.Defaults.Count)
                            {
                                if (hasKeywords && keywords.ContainsKey(co.VarNames[outArg_i]))
                                {
                                    outArgs[outArg_i] = keywords[co.VarNames[outArg_i]];
                                }
                                else
                                {
                                    if (missingOverrides == null)
                                    {
                                        missingOverrides = new List<string>();
                                    }
                                    missingOverrides.Add(co.VarNames[outArg_i]);
                                }
                                ++inArg_i;
                                ++outArg_i;
                            }

                            if(missingOverrides != null && missingOverrides.Count > 0)
                            {
                                // TODO: [PARAM MATCHER ERRORS] Cast to actual TypeError
                                var errorBuilder = new StringBuilder("TypeError: " + co.Name + " missing " + co.ArgCount + " positional argument");
                                if(missingOverrides.Count == 1)
                                {
                                    errorBuilder.Append(": ");
                                    errorBuilder.Append(missingOverrides[0]);
                                }
                                else
                                {
                                    errorBuilder.Append("s: ");
                                    for(int i = 0; i < missingOverrides.Count - 2; ++i)
                                    {
                                        errorBuilder.Append(missingOverrides[i]);
                                        errorBuilder.Append(", ");                                        
                                    }
                                    errorBuilder.Append(missingOverrides[missingOverrides.Count - 2]);
                                    errorBuilder.Append(" and ");
                                    errorBuilder.Append(missingOverrides[missingOverrides.Count - 1]);
                                }

                                throw new Exception(errorBuilder.ToString());
                            }
                            else if(co.Defaults.Count > 0)
                            {
                                state = ArgParamState.KeywordOrDefault;
                            }
                            else if(co.HasVargs)
                            {
                                state = ArgParamState.Variable;
                            }
                            else if(co.HasKwargs)
                            {
                                state = ArgParamState.KeywordOnly;
                            }
                            else
                            {
                                state = ArgParamState.Finished;
                            }
                            
                        }
                        break;
                    case ArgParamState.KeywordOrDefault:
                        // We might not start at the first default; some positionals might have had a keyword override.
                        default_i = outArg_i - (co.ArgCount - co.Defaults.Count);
                        while (default_i < co.Defaults.Count || outArg_i < co.ArgCount)
                        {
                            if (hasKeywords && keywords.ContainsKey(co.VarNames[outArg_i]))
                            {
                                outArgs[outArg_i] = keywords[co.VarNames[outArg_i]];
                            }
                            else if(default_i < co.Defaults.Count)
                            {
                                outArgs[outArg_i] = co.Defaults[default_i];
                            }
                            else
                            {
                                // TODO: [PARAM MATCHER ERRORS] Cast to actual TypeError
                                throw new Exception("TypeError: " + co.Name + " takes " + co.ArgCount + " position arguments but " + inArgs.Length + " were given");
                            }
                            default_i += 1;
                            outArg_i += 1;
                        }

                        if (default_i >= co.Defaults.Count)
                        {
                            if(co.HasVargs)
                            {
                                state = ArgParamState.Variable;
                            }
                            else if(hasKwOnlyArgs)
                            {
                                state = ArgParamState.KeywordOnly;
                            }
                            else
                            {
                                state = ArgParamState.Finished;
                            }
                        }
                        break;
                    case ArgParamState.Variable:
                        {
                            var vargLength = inArgs.Length - inArg_i;
                            vargLength = vargLength < 0 ? 0 : vargLength;
                            object[] vargs = new object[vargLength];
                            int varg_i = 0;
                            // Variable arguments (*args)
                            while (inArg_i < inArgs.Length)
                            {
                                vargs[varg_i] = inArgs[inArg_i];
                                ++inArg_i;
                                ++varg_i;
                            }
                            outArgs[outArg_i] = PyTuple.Create(vargs);
                            outArg_i += 1;

                            if(hasKwOnlyArgs)
                            {
                                state = ArgParamState.KeywordOnly;
                            }
                            else
                            {
                                state = ArgParamState.Finished;
                            }
                        }
                        break;
                    case ArgParamState.KeywordOnly:
                        {
                            int kwonly_i = 0;
                            while(kwonly_i < co.KWDefaults.Count)
                            {
                                if(hasKeywords && keywords.ContainsKey(co.VarNames[outArg_i]))
                                {
                                    outArgs[outArg_i] = keywords[co.VarNames[outArg_i]];
                                }
                                else
                                {
                                    outArgs[outArg_i] = co.KWDefaults[kwonly_i];
                                }

                                ++kwonly_i;
                                ++outArg_i;
                            }
                        }
                        state = ArgParamState.Finished;
                        break;
                }
            }

            return outArgs;
        }

        // BOOKMARK: Do all the default argument resolution here for .NET wrapped code objects.
        public static object[] Resolve(WrappedCodeObject co, object[] inArgs, Injector injector, Dictionary<string, object> defaultOverrides = null)
        {
            var methodInformation = co.FindBestMethodMatch(inArgs);
            var methodBase = methodInformation.FoundMethodInformation;
            object[] outArgs = injector.Inject2(methodBase, inArgs, overrides: defaultOverrides);
            return outArgs;
        }

    }
}