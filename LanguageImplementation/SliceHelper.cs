using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

using LanguageImplementation.DataTypes;
using LanguageImplementation.DataTypes.Exceptions;

namespace LanguageImplementation
{
    public class DotNetSlice
    {
        public int Start;
        public int Step;
        public int Stop;

        // Given a length, this will fixup the start and stop indices to not be negative any more.
        public void AdjustToLength(int len)
        {
            // Adjust negative starts and stops based on array length.
            Start = Start >= 0 ? Start : len + Start;
            Stop = Stop >= 0 ? Stop : len + Stop;
        }
    }

    public class SliceHelper
    {
        public static async Task<int> CastSliceIndex(IInterpreter interpreter, FrameContext context, object sliceIdx)
        {
            var asPyObject = sliceIdx as PyObject;
            if (asPyObject != null)
            {
                // Slight optimization: PyInteger has __index__ but we already know to get its index.
                var asPyInt = asPyObject as PyInteger;
                if (asPyInt != null)
                {
                    return (int)asPyInt.InternalValue;
                }
                else if (asPyObject.__dict__.ContainsKey("__index__"))
                {
                    var index_dunder = (IPyCallable)asPyObject.__dict__["__index__"];
                    var index = await index_dunder.Call(interpreter, context, new object[0]);
                    var asPyIndex = index as PyInteger;
                    if (asPyIndex == null)
                    {
                        context.CurrentException = new TypeError("TypeError: slice indices must be integers or None or have an __index__ method");
                        return 0;
                    }
                    else
                    {
                        return (int)asPyIndex.InternalValue;
                    }
                }
                else
                {
                    context.CurrentException = new TypeError("TypeError: slice indices must be integers or None or have an __index__ method");
                    return 0;
                }
            }
            else if (sliceIdx is BigInteger || sliceIdx is int)
            {
                return (int)sliceIdx;
            }
            else
            {
                context.CurrentException = new TypeError("TypeError: slice indices must be integers or None or have an __index__ method");
                return 0;
            }
        }

        public static async Task<DotNetSlice> extractSliceIndices(IInterpreter interpreter, FrameContext context, PySlice slice,
            int listLen)
        {
            var dotNetSlice = new DotNetSlice();
            dotNetSlice.Start = 0;
            dotNetSlice.Stop = listLen;
            dotNetSlice.Step = 1;
            if (slice.Start != null && slice.Start != NoneType.Instance)
            {
                dotNetSlice.Start = await CastSliceIndex(interpreter, context, slice.Start);
            }
            if (slice.Stop != null && slice.Stop != NoneType.Instance)
            {
                dotNetSlice.Stop = await CastSliceIndex(interpreter, context, slice.Stop);
            }
            if (slice.Step != null && slice.Step != NoneType.Instance)
            {
                dotNetSlice.Step = await CastSliceIndex(interpreter, context, slice.Step);
            }
            if (context.CurrentException != null)
            {
                return null;                // castSliceIndex failed somewhere along the line.
            }
            return dotNetSlice;
        }

        public static async Task<PyList> MakeSlice<T>(IInterpreter interpreter, FrameContext context, PySlice slice, List<T> list)
        {
            int listLen = list.Count;
            int start = 0;
            int stop = listLen;
            int step = 1;
            if (slice.Start != null && slice.Start != NoneType.Instance)
            {
                start = await CastSliceIndex(interpreter, context, slice.Start);
            }
            if (slice.Stop != null && slice.Stop != NoneType.Instance)
            {
                stop = await CastSliceIndex(interpreter, context, slice.Stop);
            }
            if (slice.Step != null && slice.Step != NoneType.Instance)
            {
                step = await CastSliceIndex(interpreter, context, slice.Step);
            }
            if (context.CurrentException != null)
            {
                return null;                // castSliceIndex failed somewhere along the line.
            }

            // Adjust negative starts and stops based on array length.
            start = start >= 0 ? start : listLen + start;
            stop = stop >= 0 ? stop : listLen + stop;

            // Adjust really negative starts to just be at the beginning of the list. We don't do modulus or rollover or whatever.
            if (start < 0)
            {
                start = 0;
            }

            var newList = new List<object>();
            for (int i = start; i < stop && i < list.Count && i >= 0; i += step)
            {
                newList.Add(list[i]);
            }
            return PyList.Create(newList);
        }
    }
}
