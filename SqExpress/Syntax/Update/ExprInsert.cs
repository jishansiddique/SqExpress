﻿using System.Collections.Generic;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Utils;

namespace SqExpress.Syntax.Update
{
    public class ExprInsert : IExprExec
    {
        public ExprInsert(ExprTableFullName target, IReadOnlyList<ExprColumnName>? targetColumns, IExprInsertSource source)
        {
            this.Target = target;
            this.TargetColumns = targetColumns;
            this.Source = source;
        }

        public ExprTableFullName Target { get; }

        public IReadOnlyList<ExprColumnName>? TargetColumns { get; }

        public IExprInsertSource Source { get; }

        public TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprInsert(this);
    }

    public class ExprInsertOutput : IExprQuery
    {
        public ExprInsertOutput(ExprInsert insert, IReadOnlyList<ExprAliasedColumnName> outputColumns)
        {
            this.Insert = insert;
            this.OutputColumns = outputColumns;
        }

        public ExprInsert Insert { get; }

        public IReadOnlyList<ExprAliasedColumnName> OutputColumns { get; }

        public TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprInsertOutput(this);

        public IReadOnlyList<string?> GetOutputColumnNames() 
            => this.OutputColumns.SelectToReadOnlyList(i => ((IExprNamedSelecting)i).OutputName);
    }


    public interface IExprInsertSource : IExpr
    {

    }

    public class ExprInsertValues : IExprInsertSource
    {
        public ExprInsertValues(ExprTableValueConstructor values)
        {
            this.Values = values;
        }

        public ExprTableValueConstructor Values { get; }

        public TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprInsertValues(this);

    }

    public class ExprInsertQuery : IExprInsertSource
    {
        public ExprInsertQuery(IExprQuery query)
        {
            this.Query = query;
        }

        public IExprQuery Query { get; }

        public TRes Accept<TRes>(IExprVisitor<TRes> visitor)
            => visitor.VisitExprInsertQuery(this);
    }
}