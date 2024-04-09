namespace SlnfUpdater.Helper
{
    public static class ArrayHelper
    {
        public static int IndexOf<T>(this T[] array, T value)
            where T : IComparable
        {
            for (var c = 0; c < array.Length; c++)
            {
                if (array[c].CompareTo(value) == 0)
                {
                    return c;
                }
            }

            return -1;
        }

        public static uint ThrowableIndexOf<T>(this T[] array, T value)
            where T : IComparable
        {
            for (uint c = 0; c < array.Length; c++)
            {
                if (array[c].CompareTo(value) == 0)
                {
                    return c;
                }
            }

            throw new Exception("Value not found: " + value);
        }

        public static bool NotIn<T>(this T v, IEnumerable<T> collection)
        {
            return
                !collection.Contains(v);
        }

        public static bool In<T>(this T v, IEnumerable<T> collection)
        {
            return
                collection.Contains(v);
        }

        public static bool NotIn<T>(this T v, params T[] array)
        {
            return
                !array.Contains(v);
        }

        public static bool In<T>(this T v, params T[] array)
        {
            return
                array.Contains(v);
        }

        public static void Foreach<T>(this T[] list, Action<T> method)
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            for (var cc = 0; cc < list.Length; cc++)
            {
                method(list[cc]);
            }
        }

        public static T[] CloneAndAppend<T>(this T[] a, T b)
        {
            var r = new T[a.Length + 1];
            a.CopyTo(r, 0);
            r[a.Length] = b;

            return r;
        }

        public static void Clear<T>(this T[] a, int startIndex, int length)
        {
            for (var cc = startIndex; cc < startIndex + length; cc++)
            {
                a[cc] = default;
            }
        }

        public static void Clear<T>(this T[] a)
        {
            for (var cc = 0; cc < a.Length; cc++)
            {
                a[cc] = default;
            }
        }

        public static void Fill<T>(this T[] a, T value)
        {
            for (var cc = 0; cc < a.Length; cc++)
            {
                a[cc] = value;
            }
        }

        public static void Fill<T>(this T[] a, Func<T> value)
        {
            for (var cc = 0; cc < a.Length; cc++)
            {
                a[cc] = value();
            }
        }

        public static void Fill<T>(this T[] a, Func<int, T> value)
        {
            for (var cc = 0; cc < a.Length; cc++)
            {
                a[cc] = value(cc);
            }
        }

        public static void TransformInPlace<T>(this T[] a, Func<int, T, T> value)
        {
            for (var cc = 0; cc < a.Length; cc++)
            {
                a[cc] = value(cc, a[cc]);
            }
        }

        public static void TransformInPlace<T>(this T[] a, Func<T, T> value)
        {
            for (var cc = 0; cc < a.Length; cc++)
            {
                a[cc] = value(a[cc]);
            }
        }


        public static T[] Concatenate<T>(this T[] a, T[] b)
        {
            if (a == null)
            {
                throw new ArgumentNullException(nameof(a));
            }
            if (b == null)
            {
                throw new ArgumentNullException(nameof(b));
            }

            var r = new T[a.Length + b.Length];
            Array.Copy(a, 0, r, 0, a.Length);
            Array.Copy(b, 0, r, a.Length, b.Length);

            return
                r;
        }

        public static T[] Concatenate<T>(this T[] a, T[] b, int bSize)
        {
            if (a == null)
            {
                throw new ArgumentNullException(nameof(a));
            }
            if (b == null)
            {
                throw new ArgumentNullException(nameof(b));
            }

            var r = new T[a.Length + bSize];
            Array.Copy(a, 0, r, 0, a.Length);
            Array.Copy(b, 0, r, a.Length, bSize);

            return
                r;
        }

        public static T[] GetSubArray<T>(this T[] a, int startIndex)
        {
            if (a == null)
            {
                throw new ArgumentNullException(nameof(a));
            }
            if (startIndex < 0 || startIndex >= a.Length)
            {
                throw new ArgumentException("startIndex < 0 || startIndex >= a.Length");
            }

            return
                a.GetSubArray(startIndex, a.Length - startIndex);
        }

        public static T[] GetSubArray<T>(this T[] a, int startIndex, int length)
        {
            if (a == null)
            {
                throw new ArgumentNullException(nameof(a));
            }
            if (startIndex < 0 || startIndex >= a.Length)
            {
                throw new ArgumentException("startIndex < 0 || startIndex >= a.Length");
            }
            if (length < 0 || startIndex + length > a.Length)
            {
                throw new ArgumentException("length < 0 || (startIndex + length) >= a.Length");
            }

            var r = new T[length];
            Array.Copy(a, startIndex, r, 0, length);

            return r;
        }

        public static TT[] ConvertAll<TF, TT>(this TF[] array, Func<TF, TT> converter)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            if (converter == null)
            {
                throw new ArgumentNullException(nameof(converter));
            }

            var result = new TT[array.Length];

            for (var cc = 0; cc < array.Length; cc++)
            {
                result[cc] = converter(array[cc]);
            }

            return result;
        }
    }
}
