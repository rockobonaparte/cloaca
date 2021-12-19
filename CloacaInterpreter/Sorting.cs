using System;

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
    }
}
