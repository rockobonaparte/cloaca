namespace CloacaInterpreter
{
    public class Sorting
    {
        private static void Swap(int[] arr, int a, int b)
        {
            int temp = arr[a];
            arr[a] = arr[b];
            arr[b] = temp;
        }
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

                    r_start = r_start >= arr.Length ? arr.Length - 1 : r_start;

                    int r = r_start;
                    for(int i = l_start; i < r_start && r < arr.Length; ++i)
                    {
                        if(arr[i] > arr[r])
                        {
                            Swap(arr, i, r);
                            if(r < r_start + r_width - 1)
                            {
                                // Keep r in-place as the last index for swapping
                                // if we have more left indices to work on, but move up
                                // otherwise.
                                r += 1;
                            }
                        }
                    }
                }
            }
        }
    }
}
