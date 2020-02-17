namespace LanguageImplementation.DataTypes
{
    public class PyClass : PyTypeObject
    {
        public PyClass(string name, CodeObject __init__, PyClass[] bases) :
            base(name, __init__)
        {
            __bases__ = bases;
            // __dict__ used to be set here but was moved upstream
        }

        public const string __REPR__ = "__repr__";

        [ClassMember]
        public static PyString __repr__(PyObject self)
        {
            // Default __repr__
            // TODO: Switch to __class__.__name__
            // TODO: Switch to an internal Python object ID (probably when doing something like object subpooling)
            // Using the hashcode is definitely not the same as what Python is using.
            return PyString.Create("<" + self.GetType().Name + " object at " + self.GetHashCode() + ">");
        }

        public const string __STR__ = "__str__";

        [ClassMember]
        public static PyString __str__(PyObject self)
        {
            // Default for __str__ is same as __repr__
            return __repr__(self);
        }
    }
}
