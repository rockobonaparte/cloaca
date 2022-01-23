using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using LanguageImplementation;
using LanguageImplementation.DataTypes;

namespace CloacaInterpreter
{
    public class Sorting
    {
        public static void Sort(int[] arr)
        {
            // Iterative merge sort
            for (int width = 1; width < arr.Length; width <<= 1)
            {
                for(int l_start = 0; l_start < arr.Length; l_start += 2 * width)
                {
                    int r_start = l_start + width;

                    // The right-side width (r_width) should not exceed the array length.
                    // We will use it later to keep r from exiting the right-side merge window.
                    int r_width = width;
                    if(r_start + r_width > arr.Length)
                    {
                        r_width -= r_start + r_width - arr.Length;
                    }

                    // Temporary arrays from which to source the original sublists
                    int[] temp = new int[width + r_width];
                    Array.Copy(arr, l_start, temp, 0, width + r_width);

                    int r = width;
                    int l = 0;
                    for(int temp_i = 0; temp_i < temp.Length; ++temp_i)
                    {
                        if(l >= width)
                        {
                            arr[l_start + temp_i] = temp[r];
                            r += 1;
                        }
                        else if(r >= width + r_width)
                        {
                            arr[l_start + temp_i] = temp[l];
                            l += 1;
                        }
                        else if (temp[r] < temp[l])
                        {
                            arr[l_start + temp_i] = temp[r];
                            r += 1;
                        }
                        else
                        {
                            arr[l_start + temp_i] = temp[l];
                            l += 1;
                        }
                    }
                }
            }
        }

        private static async Task<bool> comparePyObj(IInterpreter interpreter, FrameContext context, object left, object right,
            IPyCallable keyfunc=null, bool reverse=false)
        {
            var comp_func_name = !reverse ? "__lt__" : "__gt__";
            var pyleft = left as PyObject;
            var pyright = right as PyObject;
            if(pyleft == null)
            {
                throw new Exception(left.ToString() + " is not a PyObject and cannot be compared for sorting.");
            }
            else if(pyright == null)
            {
                throw new Exception(right.ToString() + " is not a PyObject and cannot be compared for sorting.");
            }

            if(keyfunc != null)
            {
                pyleft = (PyObject) await keyfunc.Call(interpreter, context, new object[] { pyleft });
                pyright = (PyObject)await keyfunc.Call(interpreter, context, new object[] { pyright });
            }

            var comp_func = pyleft.__dict__.GetDefault(comp_func_name, null) as IPyCallable;
            if (comp_func == null)
            {
                throw new Exception(pyleft.__class__.Name + " does not have a working, callable " + comp_func_name + " implementation");
            }

            var resultObj = await comp_func.Call(interpreter, context, new object[] { pyleft, pyright });
            var resultPyBool = resultObj as PyBool;
            if(resultPyBool == null)
            {
                throw new Exception(pyleft.__class__.Name + " " + comp_func_name + " did not return PyBool " + resultPyBool.ToString());
            }
            return resultPyBool.InternalValue;
        }

        public static object DefaultSortKeyfunc(object o)
        {
            return o;
        }

        private static WrappedCodeObject _defaultKeyFunc = null;
        public static WrappedCodeObject DefaultKeyFunc
        {
            get
            {
                if(_defaultKeyFunc == null)
                {
                    _defaultKeyFunc = new WrappedCodeObject("DefaultSortKeyfunc", typeof(Sorting).GetMethod("DefaultSortKeyfunc"));
                }
                return _defaultKeyFunc;
            }
        }

        public static async Task Sort(IInterpreter interpreter, FrameContext context, List<object> list, IPyCallable keyfunc=null, bool reverse=false)
        {
            for (int width = 1; width < list.Count; width <<= 1)
            {
                for (int l_start = 0; l_start < list.Count; l_start += 2 * width)
                {
                    int r_start = l_start + width;

                    // The right-side width (r_width) should not exceed the array length.
                    // We will use it later to keep r from exiting the right-side merge window.
                    int r_width = width;
                    if (r_start + r_width > list.Count)
                    {
                        r_width -= r_start + r_width - list.Count;
                    }

                    // Temporary arrays from which to source the original sublists
                    object[] temp = new object[width + r_width];
                    list.CopyTo(l_start, temp, 0, width + r_width);

                    int r = width;
                    int l = 0;

                    for (int temp_i = 0; temp_i < temp.Length; ++temp_i)
                    {
                        if (l >= width)
                        {
                            list[l_start + temp_i] = temp[r];
                            r += 1;
                        }
                        else if (r >= width + r_width)
                        {
                            list[l_start + temp_i] = temp[l];
                            l += 1;
                        }
                        else if (await comparePyObj(interpreter, context,
                            temp[r],
                            temp[l],
                            keyfunc,
                            reverse))
                        {
                            list[l_start + temp_i] = temp[r];
                            r += 1;
                        }
                        else
                        {
                            list[l_start + temp_i] = temp[l];
                            l += 1;
                        }
                    }
                }
            }
        }
    }
}
