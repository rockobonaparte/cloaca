using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloacaInterpreter;
using CloacaNative.IO.DataTypes;
using LanguageImplementation;
using LanguageImplementation.DataTypes;

namespace CloacaNative
{
    public class NativeResourceManager
    {
        private readonly Dictionary<Type, INativeResourceProvider> _nativeResourceProviders =
            new Dictionary<Type, INativeResourceProvider>();

        private readonly List<Handle> _activeHandles = new List<Handle>();

        private int _nextDescriptor = 1;


        public void RegisterProvider<T>(INativeResourceProvider provider)
        {
            var providerType = typeof(T);
            if (_nativeResourceProviders.ContainsKey(providerType))
            {
                throw new Exception($"Provider of type '{providerType}' already exists.");
            }

            _nativeResourceProviders.Add(providerType, provider);
        }

        public bool TryGetProvider<T>(out T provider) where T : INativeResourceProvider
        {
            var providerType = typeof(T);
            if (_nativeResourceProviders.ContainsKey(providerType))
            {
                provider = (T) _nativeResourceProviders[providerType];
                return true;
            }
            else
            {
                provider = default(T);
                return false;
            }
        }

        public void RegisterBuiltins(Interpreter interpreter)
        {
            interpreter.AddBuiltin(new WrappedCodeObject("open", typeof(NativeResourceManager).GetMethod("open_func"),
                this));
        }

        public Task<PyIOBase> open_func(IInterpreter interpreter, FrameContext context, string fileName, string fileMode)
        {
            if (TryGetProvider<INativeFileProvider>(out INativeFileProvider provider))
            {
                var handle = CreateResourceHandle();
                _activeHandles.Add(handle);
                return provider.Open(interpreter, context, handle, fileName, fileMode);
            }
            throw new Exception("Missing provider.");
        }

        private Handle CreateResourceHandle()
        {
            return new Handle(this, _nextDescriptor++);
        }
    }
}