using LanguageImplementation.DataTypes.Exceptions;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace LanguageImplementation.DataTypes
{
    public class PyStringClass : PyClass
    {
        public PyStringClass(PyFunction __init__) :
            base("str", __init__, new PyClass[0])
        {
            __instance = this;

            // We have to replace PyTypeObject.DefaultNew with one that creates a PyString.
            // TODO: Can this be better consolidated?
            Expression<Action<PyTypeObject>> expr = instance => DefaultNew<PyString>(null);
            var methodInfo = ((MethodCallExpression)expr.Body).Method;
            __new__ = new WrappedCodeObject("__new__", methodInfo, this);
        }

        private static PyStringClass __instance;
        public static PyStringClass Instance
        {
            get
            {
                if(__instance == null)
                {
                    __instance = new PyStringClass(null);
                }
                return __instance;
            }
        }

        private static void castOperands(PyObject self, PyObject other, out PyString selfOut, out PyString otherOut, string operation)
        {
            selfOut = self as PyString;
            otherOut = other as PyString;
            if (selfOut == null)
            {
                throw new Exception("Tried to use a non-PyString for lvalue of: " + operation);
            }
            if (otherOut == null)
            {
                throw new Exception("TypeError: can only concatenate str (not \"" + other.__class__.Name + "\") to str");
            }
        }

        [ClassMember]
        //  __add__(self, value, /)
        //      Return self+value.
        //
        public static PyObject __add__(FrameContext context, PyObject self, PyObject other)
        {
            PyString a, b;
            try
            {
                castOperands(self, other, out a, out b, "concatenation");
            }
            catch(Exception e)
            {
                context.CurrentException = new TypeError(e.Message);
                return null;
            }
            var newPyString = PyString.Create(a.InternalValue + b.InternalValue);
            return newPyString;
        }

        [ClassMember]
        //  __contains__(self, key, /)
        //      Return key in self.
        //
        public static PyBool __contains__(FrameContext context, PyString self, PyString key)
        {
            return PyBool.Create(self.InternalValue.Contains(key.InternalValue));
        }

        [ClassMember]
        //  __mul__(self, value, /)
        //      Return self*value.
        //
        public static PyObject __mul__(PyString self, PyObject other, FrameContext context)
        {
            PyString a;
            PyInteger b;
            a = self as PyString;
            b = other as PyInteger;
            if(a == null)
            {
                context.CurrentException = new PyException("Tried to use a non-PyString for lvalue of: multiplication");
                return null;
            }

            if(b == null)
            {
                context.CurrentException = new TypeError("can't multiply sequence by non-int of type '" + other.__class__.Name + "'");
                return null;
            }
            var newPyString = PyString.Create(string.Concat(Enumerable.Repeat(a.InternalValue, (int) b.InternalValue)));
            return newPyString;
        }

        [ClassMember]
        public static PyObject __sub__(PyObject self, PyObject other)
        {
            // TODO: TypeError: unsupported operand type(s) for -: '[self type]' and '[other type]'
            throw new Exception("Strings do not support subtraction");
        }

        //  __rmul__(self, value, /)
        //      Return value*self.
        //
        [ClassMember]
        public static PyObject __rmul__(PyString self, PyObject lother, FrameContext context)
        {
            return __mul__(self, lother, context);
        }

        [ClassMember]
        public static PyObject __div__(PyObject self, PyObject other)
        {
            // TODO: TypeError: unsupported operand type(s) for /: '[self type]' and '[other type]'
            throw new Exception("Strings do not support division");
        }

        [ClassMember]
        //  __lt__(self, value, /)
        //      Return self<value.
        //
        public static PyBool __lt__(PyObject self, PyObject other)
        {
            PyString a, b;
            castOperands(self, other, out a, out b, "less-than");
            return a.InternalValue.CompareTo(b.InternalValue) < 0;
        }

        [ClassMember]
        //  __gt__(self, value, /)
        //      Return self>value.
        //
        public static PyBool __gt__(PyObject self, PyObject other)
        {
            PyString a, b;
            castOperands(self, other, out a, out b, "greater-than");
            return a.InternalValue.CompareTo(b.InternalValue) > 0;
        }

        [ClassMember]
        //  __le__(self, value, /)
        //      Return self<=value.
        //
        public static PyBool __le__(PyObject self, PyObject other)
        {
            PyString a, b;
            castOperands(self, other, out a, out b, "less-than-equal");
            return a.InternalValue.CompareTo(b.InternalValue) <= 0;
        }

        [ClassMember]
        //  __ge__(self, value, /)
        //      Return self>=value.
        //
        public static PyBool __ge__(PyObject self, PyObject other)
        {
            PyString a, b;
            castOperands(self, other, out a, out b, "greater-than-equal");
            return a.InternalValue.CompareTo(b.InternalValue) >= 0;
        }

        [ClassMember]
        //  __eq__(self, value, /)
        //      Return self==value.
        //
        public static PyBool __eq__(PyObject self, PyObject other)
        {
            PyString a, b;
            castOperands(self, other, out a, out b, "equality");
            return a.InternalValue.CompareTo(b.InternalValue) == 0;
        }

        [ClassMember]
        //  __ne__(self, value, /)
        //      Return self!=value.
        //
        public static PyBool __ne__(PyObject self, PyObject other)
        {
            PyString a, b;
            castOperands(self, other, out a, out b, "non-equality");
            return a.InternalValue.CompareTo(b.InternalValue) != 0;
        }

        [ClassMember]
        public static PyBool __ltgt__(PyObject self, PyObject other)
        {
            PyString a, b;
            castOperands(self, other, out a, out b, "less-than-greater-than");
            var compared = a.InternalValue.CompareTo(b.InternalValue);
            return compared < 0 && compared > 0;
        }

        [ClassMember]
        //  __str__(self, /)
        //      Return str(self).
        //
        public static new PyString __str__(PyObject self)
        {
            return __repr__(self);
        }

        [ClassMember]
        //  __repr__(self, /)
        //      Return repr(self).
        //
        public static new PyString __repr__(PyObject self)
        {
            return PyString.Create(((PyString)self).ToString());
        }

        [ClassMember]
        //  __len__(self, /)
        //      Return len(self).
        //
        public static PyInteger __len__(IInterpreter interpreter, FrameContext context, PyObject self)
        {
            var asStr = (PyString)self;
            return PyInteger.Create(asStr.InternalValue.Length);
        }

        [ClassMember]
        //  capitalize(self, /)
        //      Return a capitalized version of the string.
        //
        //      More specifically, make the first character have upper case and the rest lower
        //      case.
        //
        public static PyString capitalize(PyString self)
        {
            if(self.InternalValue.Length > 0)
            {
                return PyString.Create(self.InternalValue[0].ToString().ToUpper() + self.InternalValue.Substring(1));
            }
            else
            {
                return PyString.Create();
            }
        }

        // TODO [str.casefold] implement.
        [ClassMember]
        //  casefold(self, /)
        //      Return a version of the string suitable for caseless comparisons.
        //
        public static PyString casefold(PyString self, FrameContext context)
        {
            context.CurrentException = new NotImplementedError(
                "casefold is not implemented yet. We tried ToLower() but discovered it doesn't handle characters " +
                "in other languages. You have to set a culture, but we don't necessarily know that. So we'll " +
                "probably have to reference Python's implementation.");
            return null;
        }

        [ClassMember]
        //  find(...)
        //      S.find(sub[, start[, end]]) -> int
        //
        //      Return the lowest index in S where substring sub is found,
        //      such that sub is contained within S[start:end].  Optional
        //      arguments start and end are interpreted as in slice notation.
        //
        //      Return -1 on failure.
        //
        public static async Task<PyInteger> find(IInterpreter interpreter, FrameContext context, PyString self, PyString substring, params PyInteger[] startstop)
        {
            DotNetSlice dotNetSlice = new DotNetSlice();
            dotNetSlice.Start = 0;
            dotNetSlice.Stop = self.InternalValue.Length - 1;
            dotNetSlice.Step = 1;

            if (startstop != null)
            {
                if(startstop.Length > 2)
                {
                    context.CurrentException = new TypeError("TypeError: find() takes at most 3 arguments (" +
                        (startstop.Length + 1) + "given)");
                    return null;
                }

                if (startstop.Length >= 1)
                {
                    dotNetSlice.Start = (int) startstop[0].InternalValue;
                }
                if (startstop.Length >= 2)
                {
                    dotNetSlice.Stop = (int)startstop[1].InternalValue;
                }

            }

            dotNetSlice.AdjustToLength(self.InternalValue.Length);
            if (dotNetSlice.Stop - dotNetSlice.Start <= 0)
            {
                return PyInteger.Create(-1);
            }
            else
            {
                return PyInteger.Create(self.InternalValue.IndexOf(substring.InternalValue, dotNetSlice.Start, dotNetSlice.Stop - dotNetSlice.Start));
            }
        }

        [ClassMember]
        public static async Task<object> __getitem__(IInterpreter interpreter, FrameContext context, PyString self, PyObject index_or_slice)
        {
            var asPyInt = index_or_slice as PyInteger;
            if (asPyInt != null)
            {
                var index = (int)asPyInt.InternalValue;
                if (index < 0)
                {
                    index = self.InternalValue.Length + index;
                }

                if (index < 0 || index >= self.InternalValue.Length)
                {
                    // TODO: Represent as a more natural Python exception;
                    throw new Exception("IndexError: string index out of range");
                }

                try
                {
                    return PyString.Create(self.InternalValue[index].ToString());
                }
                catch (ArgumentOutOfRangeException)
                {
                    // TODO: Represent as a more natural Python exception;
                    throw new Exception("IndexError: string index out of range");
                }
            }

            var asPySlice = index_or_slice as PySlice;
            if (asPySlice != null)
            {
                var dotNetSlice = await SliceHelper.extractSliceIndices(interpreter, context, asPySlice, self.InternalValue.Length);
                if(dotNetSlice.Step == 1)
                {
                    return PyString.Create(self.InternalValue.Substring(dotNetSlice.Start, dotNetSlice.Stop - dotNetSlice.Start));
                }
                else
                {
                    var sb = new StringBuilder();
                    for (int i = dotNetSlice.Start;
                        i < dotNetSlice.Stop && i < self.InternalValue.Length && i >= 0;
                        i += dotNetSlice.Step)
                    {
                        sb.Append(self.InternalValue[i]);
                    }
                    return PyString.Create(sb.ToString());
                }
            }
            else
            {
                context.CurrentException = new PyException("__getitem__ requires an integer index or slice, received " + index_or_slice.GetType().Name);
                return null;
            }
        }

        // Methods to implement
        //
        //  __format__(self, format_spec, /)
        //      Return a formatted version of the string as described by format_spec.
        //
        //  __getattribute__(self, name, /)
        //      Return getattr(self, name).
        //
        //  __getnewargs__(...)
        //
        //  __hash__(self, /)
        //      Return hash(self).
        //
        //  __iter__(self, /)
        //      Implement iter(self).
        //
        //  __mod__(self, value, /)
        //      Return self%value.
        //
        //  __rmod__(self, value, /)
        //      Return value%self.
        //
        //  __sizeof__(self, /)
        //      Return the size of the string in memory, in bytes.
        //
        //  center(self, width, fillchar=' ', /)
        //      Return a centered string of length width.
        //
        //      Padding is done using the specified fill character (default is a space).
        //
        //  count(...)
        //      S.count(sub[, start[, end]]) -> int
        //
        //      Return the number of non-overlapping occurrences of substring sub in
        //      string S[start:end].  Optional arguments start and end are
        //      interpreted as in slice notation.
        //
        //  encode(self, /, encoding='utf-8', errors='strict')
        //      Encode the string using the codec registered for encoding.
        //
        //      encoding
        //        The encoding in which to encode the string.
        //      errors
        //        The error handling scheme to use for encoding errors.
        //        The default is 'strict' meaning that encoding errors raise a
        //        UnicodeEncodeError.  Other possible values are 'ignore', 'replace' and
        //        'xmlcharrefreplace' as well as any other name registered with
        //        codecs.register_error that can handle UnicodeEncodeErrors.
        //
        //  endswith(...)
        //      S.endswith(suffix[, start[, end]]) -> bool
        //
        //      Return True if S ends with the specified suffix, False otherwise.
        //      With optional start, test S beginning at that position.
        //      With optional end, stop comparing S at that position.
        //      suffix can also be a tuple of strings to try.
        //
        //  expandtabs(self, /, tabsize=8)
        //      Return a copy where all tab characters are expanded using spaces.
        //
        //      If tabsize is not given, a tab size of 8 characters is assumed.
        //
        //  format(...)
        //      S.format(*args, **kwargs) -> str
        //
        //      Return a formatted version of S, using substitutions from args and kwargs.
        //      The substitutions are identified by braces ('{' and '}').
        //
        //  format_map(...)
        //      S.format_map(mapping) -> str
        //
        //      Return a formatted version of S, using substitutions from mapping.
        //      The substitutions are identified by braces ('{' and '}').
        //
        //  index(...)
        //      S.index(sub[, start[, end]]) -> int
        //
        //      Return the lowest index in S where substring sub is found,
        //      such that sub is contained within S[start:end].  Optional
        //      arguments start and end are interpreted as in slice notation.
        //
        //      Raises ValueError when the substring is not found.
        //
        //  isalnum(self, /)
        //      Return True if the string is an alpha-numeric string, False otherwise.
        //
        //      A string is alpha-numeric if all characters in the string are alpha-numeric and
        //      there is at least one character in the string.
        //
        //  isalpha(self, /)
        //      Return True if the string is an alphabetic string, False otherwise.
        //
        //      A string is alphabetic if all characters in the string are alphabetic and there
        //      is at least one character in the string.
        //
        //  isascii(self, /)
        //      Return True if all characters in the string are ASCII, False otherwise.
        //
        //      ASCII characters have code points in the range U+0000-U+007F.
        //      Empty string is ASCII too.
        //
        //  isdecimal(self, /)
        //      Return True if the string is a decimal string, False otherwise.
        //
        //      A string is a decimal string if all characters in the string are decimal and
        //      there is at least one character in the string.
        //
        //  isdigit(self, /)
        //      Return True if the string is a digit string, False otherwise.
        //
        //      A string is a digit string if all characters in the string are digits and there
        //      is at least one character in the string.
        //
        //  isidentifier(self, /)
        //      Return True if the string is a valid Python identifier, False otherwise.
        //
        //      Call keyword.iskeyword(s) to test whether string s is a reserved identifier,
        //      such as "def" or "class".
        //
        //  islower(self, /)
        //      Return True if the string is a lowercase string, False otherwise.
        //
        //      A string is lowercase if all cased characters in the string are lowercase and
        //      there is at least one cased character in the string.
        //
        //  isnumeric(self, /)
        //      Return True if the string is a numeric string, False otherwise.
        //
        //      A string is numeric if all characters in the string are numeric and there is at
        //      least one character in the string.
        //
        //  isprintable(self, /)
        //      Return True if the string is printable, False otherwise.
        //
        //      A string is printable if all of its characters are considered printable in
        //      repr() or if it is empty.
        //
        //  isspace(self, /)
        //      Return True if the string is a whitespace string, False otherwise.
        //
        //      A string is whitespace if all characters in the string are whitespace and there
        //      is at least one character in the string.
        //
        //  istitle(self, /)
        //      Return True if the string is a title-cased string, False otherwise.
        //
        //      In a title-cased string, upper- and title-case characters may only
        //      follow uncased characters and lowercase characters only cased ones.
        //
        //  isupper(self, /)
        //      Return True if the string is an uppercase string, False otherwise.
        //
        //      A string is uppercase if all cased characters in the string are uppercase and
        //      there is at least one cased character in the string.
        //
        //  join(self, iterable, /)
        //      Concatenate any number of strings.
        //
        //      The string whose method is called is inserted in between each given string.
        //      The result is returned as a new string.
        //
        //      Example: '.'.join(['ab', 'pq', 'rs']) -> 'ab.pq.rs'
        //
        //  ljust(self, width, fillchar=' ', /)
        //      Return a left-justified string of length width.
        //
        //      Padding is done using the specified fill character (default is a space).
        //
        //  lower(self, /)
        //      Return a copy of the string converted to lowercase.
        //
        //  lstrip(self, chars=None, /)
        //      Return a copy of the string with leading whitespace removed.
        //
        //      If chars is given and not None, remove characters in chars instead.
        //
        //  partition(self, sep, /)
        //      Partition the string into three parts using the given separator.
        //
        //      This will search for the separator in the string.  If the separator is found,
        //      returns a 3-tuple containing the part before the separator, the separator
        //      itself, and the part after it.
        //
        //      If the separator is not found, returns a 3-tuple containing the original string
        //      and two empty strings.
        //
        //  removeprefix(self, prefix, /)
        //      Return a str with the given prefix string removed if present.
        //
        //      If the string starts with the prefix string, return string[len(prefix):].
        //      Otherwise, return a copy of the original string.
        //
        //  removesuffix(self, suffix, /)
        //      Return a str with the given suffix string removed if present.
        //
        //      If the string ends with the suffix string and that suffix is not empty,
        //      return string[:-len(suffix)]. Otherwise, return a copy of the original
        //      string.
        //
        //  replace(self, old, new, count=-1, /)
        //      Return a copy with all occurrences of substring old replaced by new.
        //
        //        count
        //          Maximum number of occurrences to replace.
        //          -1 (the default value) means replace all occurrences.
        //
        //      If the optional argument count is given, only the first count occurrences are
        //      replaced.
        //
        //  rfind(...)
        //      S.rfind(sub[, start[, end]]) -> int
        //
        //      Return the highest index in S where substring sub is found,
        //      such that sub is contained within S[start:end].  Optional
        //      arguments start and end are interpreted as in slice notation.
        //
        //      Return -1 on failure.
        //
        //  rindex(...)
        //      S.rindex(sub[, start[, end]]) -> int
        //
        //      Return the highest index in S where substring sub is found,
        //      such that sub is contained within S[start:end].  Optional
        //      arguments start and end are interpreted as in slice notation.
        //
        //      Raises ValueError when the substring is not found.
        //
        //  rjust(self, width, fillchar=' ', /)
        //      Return a right-justified string of length width.
        //
        //      Padding is done using the specified fill character (default is a space).
        //
        //  rpartition(self, sep, /)
        //      Partition the string into three parts using the given separator.
        //
        //      This will search for the separator in the string, starting at the end. If
        //      the separator is found, returns a 3-tuple containing the part before the
        //      separator, the separator itself, and the part after it.
        //
        //      If the separator is not found, returns a 3-tuple containing two empty strings
        //      and the original string.
        //
        //  rsplit(self, /, sep=None, maxsplit=-1)
        //      Return a list of the words in the string, using sep as the delimiter string.
        //
        //        sep
        //          The delimiter according which to split the string.
        //          None (the default value) means split according to any whitespace,
        //          and discard empty strings from the result.
        //        maxsplit
        //          Maximum number of splits to do.
        //          -1 (the default value) means no limit.
        //
        //      Splits are done starting at the end of the string and working to the front.
        //
        //  rstrip(self, chars=None, /)
        //      Return a copy of the string with trailing whitespace removed.
        //
        //      If chars is given and not None, remove characters in chars instead.
        //
        //  split(self, /, sep=None, maxsplit=-1)
        //      Return a list of the words in the string, using sep as the delimiter string.
        //
        //      sep
        //        The delimiter according which to split the string.
        //        None (the default value) means split according to any whitespace,
        //        and discard empty strings from the result.
        //      maxsplit
        //        Maximum number of splits to do.
        //        -1 (the default value) means no limit.
        //
        //  splitlines(self, /, keepends=False)
        //      Return a list of the lines in the string, breaking at line boundaries.
        //
        //      Line breaks are not included in the resulting list unless keepends is given and
        //      true.
        //
        //  startswith(...)
        //      S.startswith(prefix[, start[, end]]) -> bool
        //
        //      Return True if S starts with the specified prefix, False otherwise.
        //      With optional start, test S beginning at that position.
        //      With optional end, stop comparing S at that position.
        //      prefix can also be a tuple of strings to try.
        //
        //  strip(self, chars=None, /)
        //      Return a copy of the string with leading and trailing whitespace removed.
        //
        //      If chars is given and not None, remove characters in chars instead.
        //
        //  swapcase(self, /)
        //      Convert uppercase characters to lowercase and lowercase characters to uppercase.
        //
        //  title(self, /)
        //      Return a version of the string where each word is titlecased.
        //
        //      More specifically, words start with uppercased characters and all remaining
        //      cased characters have lower case.
        //
        //  translate(self, table, /)
        //      Replace each character in the string using the given translation table.
        //
        //        table
        //          Translation table, which must be a mapping of Unicode ordinals to
        //          Unicode ordinals, strings, or None.
        //
        //      The table must implement lookup/indexing via __getitem__, for instance a
        //      dictionary or list.  If this operation raises LookupError, the character is
        //      left untouched.  Characters mapped to None are deleted.
        //
        //  upper(self, /)
        //      Return a copy of the string converted to uppercase.
        //
        //  zfill(self, width, /)
        //      Pad a numeric string with zeros on the left, to fill a field of the given width.
        //
        //      The string is never truncated.

    }

    public class PyString : PyObject
    {
        public string InternalValue;
        public PyString(string str) : base(PyStringClass.Instance)
        {
            this.InternalValue = str;
        }

        public PyString()
        {
            InternalValue = "";
        }

        public static PyString Create()
        {
            return PyTypeObject.DefaultNew<PyString>(PyStringClass.Instance);
        }

        public static PyString Create(string value)
        {
            var pyString = PyTypeObject.DefaultNew<PyString>(PyStringClass.Instance);
            pyString.InternalValue = value;
            return pyString;
        }

        public override bool Equals(object obj)
        {
            var asPyStr = obj as PyString;
            if(asPyStr == null)
            {
                return false;
            }
            else
            {
                return asPyStr.InternalValue == InternalValue;
            }
        }

        public override int GetHashCode()
        {
            return InternalValue.GetHashCode();
        }

        public override string ToString()
        {
            return InternalValue;
        }

        public static implicit operator string(PyString asPyString)
        {
            return asPyString.InternalValue;
        }
    }
}
