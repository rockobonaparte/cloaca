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
        public static PyObject __mul__(PyObject self, PyObject other)
        {
            PyString a;
            PyInteger b;
            a = self as PyString;
            b = other as PyInteger;
            if(a == null)
            {
                throw new Exception("Tried to use a non-PyString for lvalue of: multiplication");
            }

            if(b == null)
            {
                // TODO: Try to realize this as a real TypeError object in some way.
                throw new Exception("TypeError: can't multiply sequence by non-int of type 'str'");
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

        [ClassMember]
        public static PyObject __div__(PyObject self, PyObject other)
        {
            // TODO: TypeError: unsupported operand type(s) for /: '[self type]' and '[other type]'
            throw new Exception("Strings do not support division");
        }

        [ClassMember]
        public static PyBool __lt__(PyObject self, PyObject other)
        {
            PyString a, b;
            castOperands(self, other, out a, out b, "less-than");
            return a.InternalValue.CompareTo(b.InternalValue) < 0;
        }

        [ClassMember]
        public static PyBool __gt__(PyObject self, PyObject other)
        {
            PyString a, b;
            castOperands(self, other, out a, out b, "greater-than");
            return a.InternalValue.CompareTo(b.InternalValue) > 0;
        }

        [ClassMember]
        public static PyBool __le__(PyObject self, PyObject other)
        {
            PyString a, b;
            castOperands(self, other, out a, out b, "less-than-equal");
            return a.InternalValue.CompareTo(b.InternalValue) <= 0;
        }

        [ClassMember]
        public static PyBool __ge__(PyObject self, PyObject other)
        {
            PyString a, b;
            castOperands(self, other, out a, out b, "greater-than-equal");
            return a.InternalValue.CompareTo(b.InternalValue) >= 0;
        }

        [ClassMember]
        public static PyBool __eq__(PyObject self, PyObject other)
        {
            PyString a, b;
            castOperands(self, other, out a, out b, "equality");
            return a.InternalValue.CompareTo(b.InternalValue) == 0;
        }

        [ClassMember]
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
        public static new PyString __str__(PyObject self)
        {
            return __repr__(self);
        }

        [ClassMember]
        public static new PyString __repr__(PyObject self)
        {
            return PyString.Create(((PyString)self).ToString());
        }

        [ClassMember]
        public static PyInteger __len__(IInterpreter interpreter, FrameContext context, PyObject self)
        {
            var asStr = (PyString)self;
            return PyInteger.Create(asStr.InternalValue.Length);
        }


        [ClassMember]
        public static PyFloat __neg__(PyObject self)
        {
            return PyFloat.Create(-((PyFloat)self).InternalValue);
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
