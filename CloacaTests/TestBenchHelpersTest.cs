using System;

using NUnit.Framework;

namespace CloacaTests
{
    [TestFixture]
    public class VariableMultimapTests
    {
        [Test]
        public void BasicVariableMatch()
        {
            VariableMultimap a = new VariableMultimap(new TupleList<string, object>
            {
                { "foo", "bar" }
            });

            VariableMultimap b = new VariableMultimap(new TupleList<string, object>
            {
                { "foo", "bar" }
            });

            try
            {
                a.AssertSubsetEquals(b);
                b.AssertSubsetEquals(a);
            }
            catch(Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [Test]
        public void ThreeVariablesMatch()
        {
            VariableMultimap a = new VariableMultimap(new TupleList<string, object>
            {
                { "foo", "bar" },
                { "a", "b" },
                { "x", "y" }
            });

            try
            {
                a.AssertSubsetEquals(a);
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }
        }

        [Test]
        public void BasicVariableMismatch()
        {
            VariableMultimap a = new VariableMultimap(new TupleList<string, object>
            {
                { "foo", "bar" }
            });

            VariableMultimap b = new VariableMultimap(new TupleList<string, object>
            {
                { "foo", "butt" }
            });

            // Starting with a vs b
            try
            {
                a.AssertSubsetEquals(b);
                throw new Exception("Mismatch was not detected");
            }
            catch (Exception e)
            {
                if(e.Message != "Mismatch 'foo' type System.String bar vs butt\n")
                {
                    Assert.Fail(e.Message);
                }
            }

            // Trying again with b vs a
            try
            {
                b.AssertSubsetEquals(a);
                throw new Exception("Mismatch was not detected");
            }
            catch (Exception e)
            {
                if (e.Message != "Mismatch 'foo' type System.String butt vs bar\n")
                {
                    Assert.Fail(e.Message);
                }
            }
        }

        [Test]
        public void BasicVariableTypeMismatch()
        {
            VariableMultimap a = new VariableMultimap(new TupleList<string, object>
            {
                { "foo", "bar" }
            });

            VariableMultimap b = new VariableMultimap(new TupleList<string, object>
            {
                { "foo", 1 }
            });

            // Starting with a vs b; this mismatch will be for an integer
            try
            {
                a.AssertSubsetEquals(b);
                throw new Exception("Mismatch was not detected");
            }
            catch (Exception e)
            {
                if (e.Message != "Missing record for 'foo' of type System.Int32\n")
                {
                    Assert.Fail(e.Message);
                }
            }

            // Trying again with b vs a; this mismatch will be for a string
            try
            {
                b.AssertSubsetEquals(a);
                throw new Exception("Mismatch was not detected");
            }
            catch (Exception e)
            {
                if (e.Message != "Missing record for 'foo' of type System.String\n")
                {
                    Assert.Fail(e.Message);
                }
            }
        }
    }
}
