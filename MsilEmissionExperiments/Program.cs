using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MsilEmissionExperiments
{
    public delegate void WrapperDelegate(int arg1, string arg2);
    public delegate void OneArgNoReturn(int arg1);
    public delegate object OneArgOneReturn(int arg1);
    public delegate void NoArgs();
    public delegate void CallsOnProgramInstance(Program prog);

    //////////////////////////////////
    /// This block is for mimicking the generated code so I can compare the diassembly the C# compiler makes
    /// to what I'm generating using MSIL manually    
    public class ReferenceWrapper
    {
        private WrappedCodeObject wco;
        public ReferenceWrapper(WrappedCodeObject wco)
        {
            this.wco = wco;
        }

        public void Call(int arg1)
        {
            wco.Call(new object[] { arg1 });
        }
    }

    //////////////////////////////////

    public class WrappedCodeObject
    {
        public object Call(object[] args)
        {
            Console.WriteLine("Yay! Made it into Call!");
            return null;
        }

        public RetVal GenericWrapperReturns<Arg1, RetVal>()
        {
            return (RetVal)Call(new object[0]);
        }

        public RetVal GenericWrapperReturns<Arg1, RetVal>(Arg1 arg1)
        {
            return (RetVal) Call(new object[] { arg1 });
        }

        public RetVal GenericWrapperReturns<Arg1, Arg2, RetVal>(Arg1 arg1, Arg2 arg2)
        {
            return (RetVal)Call(new object[] { arg1, arg2 });
        }

        public RetVal GenericWrapperReturns<Arg1, Arg2, Arg3, RetVal>(Arg1 arg1, Arg2 arg2, Arg3 arg3)
        {
            return (RetVal)Call(new object[] { arg1, arg2, arg3 });
        }

        public RetVal GenericWrapperReturns<Arg1, Arg2, Arg3, Arg4, RetVal>(Arg1 arg1, Arg2 arg2, Arg3 arg3, Arg4 arg4)
        {
            return (RetVal)Call(new object[] { arg1, arg2, arg3, arg4 });
        }

        public void GenericWrapperVoid<Arg1>()
        {
            Call(new object[0]);
        }

        public void GenericWrapperVoid<Arg1>(Arg1 arg1)
        {
            Call(new object[] { arg1 });
        }

        public void GenericWrapperVoid<Arg1, Arg2>(Arg1 arg1, Arg2 arg2)
        {
            Call(new object[] { arg1, arg2 });
        }

        public void GenericWrapperVoid<Arg1, Arg2, Arg3>(Arg1 arg1, Arg2 arg2, Arg3 arg3)
        {
            Call(new object[] { arg1, arg2, arg3 });
        }
        public void GenericWrapperVoid<Arg1, Arg2, Arg3, Arg4>(Arg1 arg1, Arg2 arg2, Arg3 arg3, Arg4 arg4)
        {
            Call(new object[] { arg1, arg2, arg3, arg4 });
        }

        public Delegate WrapWithGeneric(MethodInfo dotNetMethod, Type delegateType)
        {
            var dotNetMethodParamInfos = dotNetMethod.GetParameters();

            if(dotNetMethodParamInfos.Length > 4)
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
                for(int i = 0; i < dotNetMethodParamInfos.Length; ++i)
                {
                    delegateArgs[i] = dotNetMethodParamInfos[i].ParameterType;
                }
                genericWrapper = typeof(WrappedCodeObject).GetMethods()
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
                genericWrapper = typeof(WrappedCodeObject).GetMethods()
                                        .Where(x => x.Name == "GenericWrapperReturns" && x.GetParameters().Length + 1 == delegateArgs.Length)
                                        .First();
            }

            var realizedWrapper = genericWrapper.MakeGenericMethod(delegateArgs);
            asDelegate = Delegate.CreateDelegate(delegateType, this, realizedWrapper);
            return asDelegate;
        }

        public void GenerateDotNetWrapper(MethodInfo dotNetMethod)
        {
            AppDomain myDomain = Thread.GetDomain();
            AssemblyName myAsmName = new AssemblyName("MsilExperimentGeneratedAssembly");
            //myAsmName.Name = "CloacaDynamicAssembly";

            AssemblyBuilder myAsmBuilder = myDomain.DefineDynamicAssembly(
                                myAsmName,
                                AssemblyBuilderAccess.RunAndSave);              // TODO: Make this AssemblyBuilderAccess.Run when we're straight on this.
            
            //ModuleBuilder myModBuilder = myAsmBuilder.DefineDynamicModule("CloacaDotNetWrappers");
            ModuleBuilder myModBuilder = myAsmBuilder.DefineDynamicModule("MsilExperimentGeneratedAssembly", "MsilExperimentGeneratedAssembly.dll");

            TypeBuilder myTypeBuilder = myModBuilder.DefineType("WrappedCodeObject_" + dotNetMethod.Name,
                                    TypeAttributes.Public);

            // Define a field in the class that'll contain the WrappedCodeObject
            var wrappedCodefieldBuilder = myTypeBuilder.DefineField("wrappedCodeObject", typeof(WrappedCodeObject), FieldAttributes.Private);

            // Define the constructor that'll take the WrappedCodeObject and keep it internally
            ConstructorBuilder constructorBuilder =
                myTypeBuilder.DefineConstructor(MethodAttributes.Public,
                      CallingConventions.Standard, new Type[] { typeof(WrappedCodeObject) });
            var ctorGen = constructorBuilder.GetILGenerator();
            ctorGen.Emit(OpCodes.Ldarg_0);
            ctorGen.Emit(OpCodes.Ldarg_1);
            ctorGen.Emit(OpCodes.Stfld, wrappedCodefieldBuilder);
            ctorGen.Emit(OpCodes.Ret);

            var dotNetMethodParamInfos = dotNetMethod.GetParameters();
            var dotNetMethodParamTypes = new Type[dotNetMethodParamInfos.Length];
            for (int i = 0; i < dotNetMethodParamInfos.Length; ++i)
            {
                dotNetMethodParamTypes[i] = dotNetMethodParamInfos[i].ParameterType;
            }

            string methodName = dotNetMethod.Name + "_generated_wrapper";
            MethodBuilder myMthdBuilder = myTypeBuilder.DefineMethod(methodName, MethodAttributes.Public, dotNetMethod.ReturnType, dotNetMethodParamTypes);

            ILGenerator wrapGen = myMthdBuilder.GetILGenerator();

            var wrappedCodeInnerCall = typeof(WrappedCodeObject).GetMethod("Call");

            wrapGen.Emit(OpCodes.Nop);
            wrapGen.Emit(OpCodes.Ldarg_0);
            wrapGen.Emit(OpCodes.Ldfld, wrappedCodefieldBuilder);       // First arg is the this pointer
            wrapGen.Emit(OpCodes.Ldc_I4_1);
            wrapGen.Emit(OpCodes.Newarr, typeof(System.Object));        // Create object[1] (specified by ldc_i4_1)
            wrapGen.Emit(OpCodes.Dup);
            wrapGen.Emit(OpCodes.Ldc_I4_0);                             // Set object array index 0...
            wrapGen.Emit(OpCodes.Ldarg_1);                              // ...to Integer argument...
            wrapGen.Emit(OpCodes.Box, typeof(int));                     // ...that has been made into an object...
            wrapGen.Emit(OpCodes.Stelem_Ref);                           // ...final bake to array element
            wrapGen.Emit(OpCodes.Callvirt, wrappedCodeInnerCall);       // call        (call target)
            wrapGen.Emit(OpCodes.Pop);                                  // Don't return anything for now so blow the return value off the stack.
            wrapGen.Emit(OpCodes.Ret);


            //wrapGen.Emit(OpCodes.Ldfld, wrappedCodefieldBuilder);
            //wrapGen.Emit(OpCodes.Callvirt, wrappedCodeInnerCall);   // call        (call target)
            //wrapGen.Emit(OpCodes.Ret);                              // ret

            Type finalizedType = myTypeBuilder.CreateType();
            var attachableMethod = finalizedType.GetMethod(methodName);

            myAsmBuilder.Save("MsilExperimentGeneratedAssembly.dll");

            object instance = Activator.CreateInstance(finalizedType, new object[] { new WrappedCodeObject() });
            attachableMethod.Invoke(instance, new object[] { 1 });
        }

        public void DoNothingCall(int takes_an_int)
        {

        }

        public object DoNothingCallReturns(int takes_an_int)
        {
            return null;
        }
    }

    public static class EmitsEvents
    {
        public static OneArgNoReturn IntEvent;
    }

    public class Program
    {
        public void SimplestCall()
        {
            Console.WriteLine("Made it through simplest call!");
        }

        public object Call(object[] args)
        {
            Console.WriteLine("Yay! Made it into Call!");
            return null;
        }

        public void ThisIsAWrapper(int arg1, string arg2)
        {
//            var args = new object[] { arg1, arg2 };
//            Call(args);
        }

        public void CallSimplestCall()
        {
            SimplestCall();
        }

        public void GenerateDynamicMethod()
        {
            // Program.ThisIsAWrapper:
            // IL_0000:  nop         
            // IL_0001:  ldc.i4.2    
            // IL_0002:  newarr      System.Object
            // IL_0007:  dup         
            // IL_0008:  ldc.i4.0    
            // IL_0009:  ldarg.1     
            // IL_000A:  box         System.Int32
            // IL_000F:  stelem.ref  
            // IL_0010:  dup         
            // IL_0011:  ldc.i4.1    
            // IL_0012:  ldarg.2     
            // IL_0013:  stelem.ref  
            // IL_0014:  stloc.0     // args
            // IL_0015:  ldarg.0     
            // IL_0016:  ldloc.0     // args
            // IL_0017:  call        UserQuery+Program.Call
            // IL_001C:  pop         
            // IL_001D:  ret
            //var wrapper = new DynamicMethod("ThisIsAWrapper_generated", typeof(void), new Type[] { typeof(int), typeof(string) });
            var wrapper = new DynamicMethod("ThisIsAWrapper_generated", typeof(void), new Type[] { typeof(Program) });
            var gen = wrapper.GetILGenerator();
            //gen.Emit(OpCodes.Nop);                              // IL_0000:  nop       
            //gen.Emit(OpCodes.Ldc_I4_2);                         // IL_0001:  ldc.i4.2    
            //gen.Emit(OpCodes.Newarr, typeof(System.Object));    // IL_0002:  newarr      System.Object
            //gen.Emit(OpCodes.Dup);                              // IL_0007:  dup  
            //gen.Emit(OpCodes.Ldc_I4_0);                         // IL_0008:  ldc.i4.0    
            //gen.Emit(OpCodes.Ldarg_1);                          // IL_0009:  ldarg.1 
            //gen.Emit(OpCodes.Box, typeof(System.Int32));        // IL_000A:  box         System.Int32
            //gen.Emit(OpCodes.Stelem_Ref);                       // IL_000F:  stelem.ref  
            //gen.Emit(OpCodes.Dup);                              // IL_0010:  dup         
            //gen.Emit(OpCodes.Ldc_I4_1);                         // IL_0011:  ldc.i4.1  
            //gen.Emit(OpCodes.Ldarg_2);                          // IL_0012:  ldarg.2    
            //gen.Emit(OpCodes.Stelem_Ref);                       // IL_0013:  stelem.ref  
            //gen.Emit(OpCodes.Stloc_0);                          // IL_0014:  stloc.0     // args
            //gen.Emit(OpCodes.Ldarg_0);                          // IL_0015:  ldarg.0     
            //gen.Emit(OpCodes.Ldloc_0);                          // IL_0016:  ldloc.0     // args
            //gen.Emit(OpCodes.Call, typeof(Program).GetMethod("Call"));      // IL_0017:  call        UserQuery+Program.Call
            //gen.Emit(OpCodes.Pop);                              // IL_001C:  pop       
            //gen.Emit(OpCodes.Ret);                              // IL_001D:  ret

            // Trying to just call Call with null
            // IL_0000:  nop         
            // IL_0001:  ldarg.0     
            // IL_0002:  ldnull      
            // IL_0003:  call        UserQuery+Program.Call
            // IL_0008:  pop         
            // IL_0009:  ret  
            var SimplestCallMInfo = typeof(Program).GetMethod("SimplestCall");
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Callvirt, SimplestCallMInfo);      // call        SimplestCall
            gen.Emit(OpCodes.Ret);                              // ret

            //var asDelegate = (WrapperDelegate) wrapper.CreateDelegate(typeof(WrapperDelegate));
            //asDelegate(1, "Wee!");
            var asDelegate = (CallsOnProgramInstance)wrapper.CreateDelegate(typeof(CallsOnProgramInstance));
            asDelegate(this);
        }

        static void Main(string[] args)
        {
            //var p = new Program();
            //p.GenerateDynamicMethod();
            var wco = new WrappedCodeObject();
            //wco.GenerateDotNetWrapper(typeof(WrappedCodeObject).GetMethod("DoNothingCall"));

            // DoNothingCall matches signature so we'll just rip it here.
            var d = wco.WrapWithGeneric(typeof(WrappedCodeObject).GetMethod("DoNothingCall"), typeof(OneArgNoReturn));

            // BOOKMARK: Try to do this with reflection and AddEventHandler. See the generic attach.
            EmitsEvents.IntEvent += (OneArgNoReturn) d;
            Console.Read();
        }
    }
}
