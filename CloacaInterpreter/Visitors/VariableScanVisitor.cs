using System.Collections.Generic;

using Antlr4.Runtime.Misc;

using Language;

public class VariableScanVisitor : CloacaBaseVisitor<object>
{
    private List<string> names;
    public VariableScanVisitor(List<string> names)
    {
        this.names = names;
    }

    public override object VisitExpr_stmt([NotNull] CloacaParser.Expr_stmtContext context)
    {
        VisitLValueTestlist_star_expr(context.testlist_star_expr()[0].test()[0]);
        return null;
    }

    public object VisitLValueTestlist_star_expr([NotNull] CloacaParser.TestContext context)
    {
        var variableName = context.or_test()[0].and_test()[0].not_test()[0].comparison().expr()[0].GetText();
        names.Add(variableName);
        return null;
    }
}
