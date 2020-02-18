using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace MsilEmissionExperiments
{
    public delegate void WrapperDelegate(int arg1, string arg2);
    public delegate void NoArgs();

    public class Program
    {
        public static void SimplestCall()
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

        public static void CallSimplestCall()
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
            var wrapper = new DynamicMethod("ThisIsAWrapper_generated", typeof(void), new Type[0]);
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
            gen.Emit(OpCodes.Call, SimplestCallMInfo);          // call        SimplestCall
            gen.Emit(OpCodes.Ret);                              // ret

            //var asDelegate = (WrapperDelegate) wrapper.CreateDelegate(typeof(WrapperDelegate));
            //asDelegate(1, "Wee!");
            var asDelegate = (NoArgs)wrapper.CreateDelegate(typeof(NoArgs));
            asDelegate();
        }

       static void Main(string[] args)
       {
            var p = new Program();
            p.GenerateDynamicMethod();
            Console.Read();
        }
    }
}
