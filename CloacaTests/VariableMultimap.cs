using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using CloacaInterpreter;

namespace CloacaTests
{
    // Helper for initializing lists of tuples
    public class TupleList<T1, T2> : List<Tuple<T1, T2>>
    {
        public void Add(T1 item, T2 item2)
        {
            Add(new Tuple<T1, T2>(item, item2));
        }
    }

    /// <summary>
    /// The Python interpreter might have multiple things with the same name. In particular, class Foo and method Foo are
    /// different names with different indices. If we want to check for these kinds of variables, then we have can't just 
    /// use a dictionary, or we'll get into a collision. This is a wrapper around a basic dictionary that does a first-level
    /// lookup based on name before disambiguating by data type. If there is only one variable of the given type, then the
    /// second-level lookup is unnecessary. However, if a 1st-level lookup is done where there is a collision, then this will
    /// raise an Exception.
    /// </summary>
    public class VariableMultimap : IEnumerable
    {
        private Dictionary<string, Dictionary<Type, object>> map;

        public VariableMultimap(Interpreter interpreter)
        {
            map = new Dictionary<string, Dictionary<Type, object>>();

            for(int i = 0; i < interpreter.LocalNames.Count; ++i)
            {
                var name = interpreter.LocalNames[i];
                var variable = interpreter.Locals[i];
                Add(name, variable);
            }
        }

        /// <summary>
        /// Use default initializers to create a VariableMultimap. This is generally used by unit tests to initialize
        /// the asserted variables.
        /// </summary>
        /// <param name="pairs">A list of tuples mapping strings to objects. The VariableMultimap will derive the type
        /// from the objects</param>
        public VariableMultimap(TupleList<string, object> pairs)
        {
            map = new Dictionary<string, Dictionary<Type, object>>();

            foreach(var pair in pairs)
            {
                Add(pair.Item1, pair.Item2);
            }
        }

        public void Add(string name, object value)
        {
            if (!map.ContainsKey(name))
            {
                map.Add(name, new Dictionary<Type, object>());
            }
            map[name].Add(value.GetType(), value);
        }

        public object Get(string name)
        {
            var firstLevel = map[name];
            if(firstLevel.Count == 1)
            {
                return firstLevel.First().Value;
            }
            else
            {
                throw new ArgumentException("There is more then one variable defined by '" + name + "'. You must further qualify by type");
            }
        }

        public bool ContainsKey(string name)
        {
            return map.ContainsKey(name);
        }

        public object Get(string name, Type type)
        {
            var firstLevel = map[name];
            if (firstLevel.Count == 1)
            {
                if(!firstLevel.ContainsKey(type))
                {
                    throw new KeyNotFoundException("Key '" + name + "' does exist but doesn't match type " + type);
                }
                else
                {
                    return firstLevel[type];
                }
            }
            else
            {
                return firstLevel[type];
            }
        }

        public object ContainsKey(string name, Type type)
        {
            var firstLevel = map[name];
            if (firstLevel.Count == 1)
            {
                if (!firstLevel.ContainsKey(type))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Returns all the values in the VariableMultimap as an IEnumerable list of tuples mapping keys to values.
        /// </summary>
        /// <returns>All entries in the VariableMultimap as an IEnumerable list of tuples mapping keys to values.</returns>
        public IEnumerator GetEnumerator()
        {
            var asList = new List<KeyValuePair<string, object>>();
            foreach(KeyValuePair<string, Dictionary<Type, object>> sublookup in map)
            {
                foreach(object actualValue in sublookup.Value)
                {
                    asList.Add(new KeyValuePair<string, object>(sublookup.Key, actualValue));
                }
            }
            return asList.GetEnumerator();
        }

        public void AssertSubsetEquals(VariableMultimap reference)
        {
            var failures = "";
            foreach (KeyValuePair<string, Dictionary<Type, object>> parentLookup in reference.map)
            {
                var name = parentLookup.Key;
                if(!map.ContainsKey(name))
                {
                    failures += "Missing any records for '" + name + "'\n";
                }
                else
                {
                    foreach (var sublookup in parentLookup.Value)
                    {
                        if(!map[name].ContainsKey(sublookup.Key))
                        {
                            failures += "Missing record for '" + name + "' of type " + sublookup.Key;
                        }
                        else
                        {
                            if(!map[name][sublookup.Key].Equals(sublookup.Value))
                            {
                                failures += "Mismatch '" + name + "' type " + sublookup.Key + " " +
                                    map[name][sublookup.Key].ToString() + " vs " + sublookup.Value.ToString();
                            }
                        }
                    }
                }
            }
            if(failures.Length > 0)
            {
                throw new Exception(failures);
            }
        }
    }
}
