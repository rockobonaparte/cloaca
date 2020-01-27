using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

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

        public Task<object> Call(IInterpreter interpreter, FrameContext context, object[] args)
        {
            var injector = new Injector(interpreter, context, interpreter.Scheduler);
            var final_args = injector.Inject(MethodInfos[0], args);

            // Little convenience here. We'll convert a non-task Task<object> type to a task.
            if (MethodInfos[0].ReturnType.IsGenericType && MethodInfos[0].ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                // Task<object> is straightforward and we can just return it. Other return types need to go through
                // our helper.
                if (MethodInfos[0].ReturnType == typeof(Task<object>))
                {
                    return (Task<object>)MethodInfos[0].Invoke(instance, final_args);
                }
                else
                {
                    return InvokeAsTaskObject(final_args);
                }
            }
            else
            {
                return Task.FromResult(MethodInfos[0].Invoke(instance, final_args));
            }
        }

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
    }
}
