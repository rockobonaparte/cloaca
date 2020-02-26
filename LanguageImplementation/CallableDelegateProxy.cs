using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LanguageImplementation
{
    /// <summary>
    /// We have to attach to our IPyCallables to .NET delegates--notably events--which requires taking the object[] call signature of
    /// the IPyCallable and map them to a normal parameter format in a .NET method. There isn't a dynamic way to do this that doesn't
    /// involve, say, creating class using manual MSIL generation and a DynamicMethod to do the packing and invocation. That kind of
    /// hand-assembled MSIL code is impossible to maintain. There is an alternative hack using generic methods:
    /// 1. Create lots of variations of the same generic call that accepts <Arg1, Arg2, Arg3..., ArgN, ...ReturnVal>
    /// 2. Find which one of these matches the delegate we're trying to bind
    /// 3. Fill the generic method matching the signature with the parameter types from the delegate we're trying to bind
    /// 4. Return a delegate as the desired type calling into the realized generic method.
    /// 
    /// When the delegate is finally called, all the arguments from the call are packed into the object[] and dumped into Call as normal.
    /// </summary>
    public class CallableDelegateProxy
    {
        private IPyCallable callable;
        private IInterpreter interpreter;
        private FrameContext contextToUseForCall;

        private CallableDelegateProxy(IPyCallable callable, IInterpreter interpreter, FrameContext contextToUseForCall)
        {
            this.callable = callable;
            this.interpreter = interpreter;
            this.contextToUseForCall = contextToUseForCall;
        }

        public bool MatchesTarget(IPyCallable callable)
        {
            return this.callable.Equals(callable);
        }

        public static Delegate Create(MethodInfo dotNetMethod, Type delegateType, IPyCallable callable, IInterpreter interpreter, FrameContext contextToUseForCall)
        {
            var proxy = new CallableDelegateProxy(callable, interpreter, contextToUseForCall);
            var dotNetMethodParamInfos = dotNetMethod.GetParameters();

            if (dotNetMethodParamInfos.Length > 2)
            {
                throw new NotImplementedException("We have only created templates for generic wrappers up to 4 arguments");
            }

            Delegate asDelegate;
            Type[] delegateArgs;
            MethodInfo genericWrapper;

            // An ugly amount of copypasta. If we have a return type, then we need an array one element longer to put in RetVal at the end.
            // We also need to find the method matching the name of the right return type and accommodate the existing of a return value into
            // the number of generic parameters required for the right binding.
            if (dotNetMethod.ReturnType == typeof(void))
            {
                delegateArgs = new Type[dotNetMethodParamInfos.Length];
                for (int i = 0; i < dotNetMethodParamInfos.Length; ++i)
                {
                    delegateArgs[i] = dotNetMethodParamInfos[i].ParameterType;
                }
                genericWrapper = typeof(CallableDelegateProxy).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                                        .Where(x => x.Name == "GenericWrapperVoid" && x.GetParameters().Length == delegateArgs.Length)
                                        .First();
            }
            else
            {
                delegateArgs = new Type[dotNetMethodParamInfos.Length + 1];
                for (int i = 0; i < dotNetMethodParamInfos.Length; ++i)
                {
                    delegateArgs[i] = dotNetMethodParamInfos[i].ParameterType;
                }

                // Last template parameter is the return type.
                delegateArgs[delegateArgs.Length - 1] = dotNetMethod.ReturnType;
                genericWrapper = typeof(CallableDelegateProxy).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                                        .Where(x => x.Name == "GenericWrapperReturns" && x.GetParameters().Length + 1 == delegateArgs.Length)
                                        .First();
            }

            var realizedWrapper = genericWrapper.MakeGenericMethod(delegateArgs);
            asDelegate = Delegate.CreateDelegate(delegateType, proxy, realizedWrapper);
            return asDelegate;
        }

        // Note that the wrappers that have a return value haven't been tested yet. We don't know if Task<RetVal> will bind
        // to an event expecting RetVal.
        private async Task<RetVal> GenericWrapperReturns<RetVal>()
        {
            var retVal = await callable.Call(interpreter, contextToUseForCall, new object[0]);
            return (RetVal) retVal;
        }

        private async Task<RetVal>GenericWrapperReturns<Arg1, RetVal>(Arg1 arg1)
        {
            var retVal = await callable.Call(interpreter, contextToUseForCall, new object[] { arg1 });
            return (RetVal)retVal;
        }

        private async Task<RetVal>GenericWrapperReturns<Arg1, Arg2, RetVal>(Arg1 arg1, Arg2 arg2)
        {
            var retVal = await callable.Call(interpreter, contextToUseForCall, new object[] { arg1, arg2 });
            return (RetVal)retVal;
        }

        private void GenericWrapperVoid()
        {
            callable.Call(interpreter, contextToUseForCall, new object[0]);
        }

        private void GenericWrapperVoid<Arg1>(Arg1 arg1)
        {
            callable.Call(interpreter, contextToUseForCall, new object[] { arg1 });
        }

        private void GenericWrapperVoid<Arg1, Arg2>(Arg1 arg1, Arg2 arg2)
        {
            callable.Call(interpreter, contextToUseForCall, new object[] { arg1, arg2 });
        }
    }
}
